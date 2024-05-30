using Unity.Burst;
using Unity.Burst.Intrinsics;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics.Systems;
using Unity.Transforms;
using System;
using Unity.Assertions;
using Unity.Core;
using UnityEngine;

namespace Unity.CharacterController
{
    /// <summary>
    /// Handles remembering character interpolation data during the fixed physics update
    /// </summary>
    [UpdateInGroup(typeof(KinematicCharacterPhysicsUpdateGroup), OrderFirst = true)]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct CharacterInterpolationRememberTransformSystem : ISystem
    {
        /// <summary>
        /// Singleton component holding global time data for interpolation calculations
        /// </summary>
        public struct Singleton : IComponentData
        {
            /// <summary>
            /// Represents the duration of an interpolation between two fixed updates
            /// </summary>
            public float InterpolationDeltaTime;
            /// <summary>
            /// Represents the elapsed time when we last remembered the transforms characters should be interpolating from
            /// </summary>
            public double LastTimeRememberedInterpolationTransforms;
        }

        private ComponentTypeHandle<LocalTransform> _transformType;
        private ComponentTypeHandle<CharacterInterpolation> _characterInterpolationType;
        private EntityQuery _interpolatedEntitiesQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _interpolatedEntitiesQuery = new EntityQueryBuilder(Allocator.Temp).WithAll<LocalTransform, CharacterInterpolation>().Build(ref state);

            _transformType = state.GetComponentTypeHandle<LocalTransform>(true);
            _characterInterpolationType = state.GetComponentTypeHandle<CharacterInterpolation>(false);

            Entity singletonEntity = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(singletonEntity, new Singleton());

            state.RequireForUpdate(_interpolatedEntitiesQuery);
            state.RequireForUpdate<Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            TimeData time = SystemAPI.Time;
            ref Singleton singleton = ref SystemAPI.GetSingletonRW<Singleton>().ValueRW;
            singleton.InterpolationDeltaTime = time.DeltaTime;
            singleton.LastTimeRememberedInterpolationTransforms = time.ElapsedTime;

            _transformType.Update(ref state);
            _characterInterpolationType.Update(ref state);

            CharacterInterpolationRememberTransformJob job = new CharacterInterpolationRememberTransformJob
            {
                TransformType = _transformType,
                CharacterInterpolationType = _characterInterpolationType,
            };
            state.Dependency = job.ScheduleParallel(_interpolatedEntitiesQuery, state.Dependency);
        }

        /// <summary>
        /// Job that remembers interpolated previous character transforms in simulation, for interpolation purposes
        /// </summary>
        [BurstCompile]
        public unsafe struct CharacterInterpolationRememberTransformJob : IJobChunk
        {
            /// <summary>
            /// LocalTransform type handle
            /// </summary>
            [ReadOnly]
            public ComponentTypeHandle<LocalTransform> TransformType;
            /// <summary>
            /// CharacterInterpolation type handle
            /// </summary>
            public ComponentTypeHandle<CharacterInterpolation> CharacterInterpolationType;

            public void Execute(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                // No enabled comps support for interpolation
                Assert.IsFalse(useEnabledMask);

                NativeArray<LocalTransform> chunkTransforms = chunk.GetNativeArray(ref TransformType);
                NativeArray<CharacterInterpolation> chunkCharacterInterpolations = chunk.GetNativeArray(ref CharacterInterpolationType);

                void* chunkInterpolationsPtr = chunkCharacterInterpolations.GetUnsafePtr();
                int chunkCount = chunk.Count;
                int sizeCharacterInterpolation = UnsafeUtility.SizeOf<CharacterInterpolation>();
                var sizeTransform = UnsafeUtility.SizeOf<LocalTransform>();
                int sizePosition = UnsafeUtility.SizeOf<float3>();
                int sizeScale = UnsafeUtility.SizeOf<float>();
                int sizeRotation = UnsafeUtility.SizeOf<quaternion>();
                int sizeByte = UnsafeUtility.SizeOf<byte>();

                // Efficiently copy all position & rotation to the "from" rigidtransform in the character interpolation component
                {
                    // Copy positions
                    UnsafeUtility.MemCpyStride(
                        (void*)((long)chunkInterpolationsPtr + sizeRotation),
                        sizeCharacterInterpolation,
                        chunkTransforms.GetUnsafeReadOnlyPtr(),
                        sizeTransform,
                        sizePosition,
                        chunkCount
                    );

                    // Copy rotations
                    UnsafeUtility.MemCpyStride(
                        chunkInterpolationsPtr,
                        sizeCharacterInterpolation,
                        (void*)((long)chunkTransforms.GetUnsafeReadOnlyPtr() + sizePosition + sizeScale),
                        sizeTransform,
                        sizeRotation,
                        chunkCount
                    );

                    // Reset interpolation skippings
                    UnsafeUtility.MemCpyStride(
                        (void*)((long)chunkInterpolationsPtr + sizeRotation + sizePosition), // the "InterpolationSkipping" field
                        sizeCharacterInterpolation,
                        (void*)((long)chunkInterpolationsPtr + sizePosition + sizeRotation + sizeByte), // the "DefaultByte" field
                        sizeCharacterInterpolation,
                        sizeByte,
                        chunkCount
                    );
                }
            }
        }
    }

    /// <summary>
    /// Handles interpolating the character during variable update
    /// </summary>
    [UpdateInGroup(typeof(TransformSystemGroup))]
    [UpdateBefore(typeof(LocalToWorldSystem))]
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    [BurstCompile]
    public partial struct CharacterInterpolationSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<CharacterInterpolation>();
            state.RequireForUpdate<CharacterInterpolationRememberTransformSystem.Singleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            CharacterInterpolationRememberTransformSystem.Singleton singleton = SystemAPI.GetSingletonRW<CharacterInterpolationRememberTransformSystem.Singleton>().ValueRO;

            if (singleton.LastTimeRememberedInterpolationTransforms <= 0f)
            {
                return;
            }

            float fixedTimeStep = singleton.InterpolationDeltaTime;
            if (fixedTimeStep == 0f)
            {
                return;
            }

            float timeAheadOfLastFixedUpdate = (float)(SystemAPI.Time.ElapsedTime - singleton.LastTimeRememberedInterpolationTransforms);
            float normalizedTimeAhead = math.clamp(timeAheadOfLastFixedUpdate / fixedTimeStep, 0f, 1f);

            CharacterInterpolationJob job = new CharacterInterpolationJob
            {
                NormalizedTimeAhead = normalizedTimeAhead,
            };
            state.Dependency = job.ScheduleParallel(state.Dependency);
        }

        /// <summary>
        /// Job that interpolates the character visual transforms
        /// </summary>
        [BurstCompile]
        [WithAll(typeof(KinematicCharacterBody))]
        public partial struct CharacterInterpolationJob : IJobEntity
        {
            /// <summary>
            /// Ratio representing how far in time we are in-between two fixed updates.
            /// </summary>
            public float NormalizedTimeAhead;

            void Execute(
                ref CharacterInterpolation characterInterpolation,
                ref LocalToWorld localToWorld,
                in LocalTransform transform)
            {
                RigidTransform targetTransform = new RigidTransform(transform.Rotation, transform.Position);

                quaternion interpolatedRot = targetTransform.rot;
                if (characterInterpolation.InterpolateRotation == 1)
                {
                    if (!characterInterpolation.ShouldSkipNextRotationInterpolation())
                    {
                        interpolatedRot = math.slerp(characterInterpolation.InterpolationFromTransform.rot, targetTransform.rot, NormalizedTimeAhead);
                    }
                }

                float3 interpolatedPos = targetTransform.pos;
                if (characterInterpolation.InterpolatePosition == 1)
                {
                    if (!characterInterpolation.ShouldSkipNextPositionInterpolation())
                    {
                        interpolatedPos = math.lerp(characterInterpolation.InterpolationFromTransform.pos, targetTransform.pos, NormalizedTimeAhead);
                    }
                }

                localToWorld.Value = float4x4.TRS(interpolatedPos, interpolatedRot, transform.Scale);
            }
        }
    }
}
