using System;
using System.Collections;
using System.Collections.Generic;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEngine;
using BoxCollider = Unity.Physics.BoxCollider;
using CapsuleCollider = Unity.Physics.CapsuleCollider;
using Collider = Unity.Physics.Collider;
using Material = Unity.Physics.Material;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Unity.CharacterController.RuntimeTests
{
    public struct TestEntity : IComponentData
    { }

    public unsafe static class CharacterTestUtils
    {
        public static Entity CreateTestEntity(World world)
        {
            Entity entity = world.EntityManager.CreateEntity();
            world.EntityManager.AddComponentData(entity, new TestEntity());
            return entity;
        }

        public static void DestroyAllTestEntities()
        {
            for (int i = 0; i < World.All.Count; i++)
            {
                World world = World.All[i];
                EntityQuery physicsEntities = new EntityQueryBuilder(Allocator.Temp).WithAll<TestEntity>().Build(world.EntityManager);
                world.EntityManager.DestroyEntity(physicsEntities);
                world.Update();
            }
        }

        public static Entity CreateCharacter(World world, float3 position, quaternion rotation, float height, float radius, AuthoringKinematicCharacterProperties characterProperties)
        {
            Entity characterEntity = CreateCapsuleBody(world, position, rotation, height, radius, BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            KinematicCharacterUtilities.CreateCharacter(world.EntityManager, characterEntity, characterProperties);

            world.EntityManager.AddComponentData(characterEntity, TestCharacterComponent.GetDefault());
            world.EntityManager.AddComponentData(characterEntity, new TestCharacterControl());

            return characterEntity;
        }

        public static Entity CreateBoxBody(World world, float3 position, quaternion rotation, float3 size, BodyMotionType motionType, CollisionResponsePolicy collisionResponse, bool addTrackedTransform)
        {
            BoxGeometry geometry = new BoxGeometry
            {
                Center = float3.zero,
                Orientation = quaternion.identity,
                Size = size,
                BevelRadius = 0f,
            };
            BlobAssetReference<Collider> colliderBlob = BoxCollider.Create(geometry);
            return CreateBaseBody(world, position, rotation, colliderBlob, motionType, collisionResponse, addTrackedTransform);
        }

        public static Entity CreateSphereBody(World world, float3 position, quaternion rotation, float radius, BodyMotionType motionType, CollisionResponsePolicy collisionResponse, bool addTrackedTransform)
        {
            SphereGeometry geometry = new SphereGeometry
            {
                Center = float3.zero,
                Radius = radius,
            };
            BlobAssetReference<Collider> colliderBlob = SphereCollider.Create(geometry);
            return CreateBaseBody(world, position, rotation, colliderBlob, motionType, collisionResponse, addTrackedTransform);
        }

        public static Entity CreateCapsuleBody(World world, float3 position, quaternion rotation, float height, float radius, BodyMotionType motionType, CollisionResponsePolicy collisionResponse, bool addTrackedTransform)
        {
            CapsuleGeometry geometry = new CapsuleGeometry
            {
                Vertex0 = math.up() * radius,
                Vertex1 = math.up() * (height - radius),
                Radius = radius,
            };
            BlobAssetReference<Collider> colliderBlob = CapsuleCollider.Create(geometry);
            return CreateBaseBody(world, position, rotation, colliderBlob, motionType, collisionResponse, addTrackedTransform);
        }

        public static Entity CreateBaseBody(World world, float3 position, quaternion rotation, BlobAssetReference<Collider> collider, BodyMotionType motionType, CollisionResponsePolicy collisionResponse, bool addTrackedTransform)
        {
            Entity bodyEntity = Entity.Null;

            if (GetWorldIndex(world, out uint worldIndex))
            {
                collider.Value.SetCollisionResponse(collisionResponse);

                bodyEntity = CreateTestEntity(world);

                world.EntityManager.AddComponentData(bodyEntity, new Simulate { });
                world.EntityManager.AddComponentData(bodyEntity, new LocalTransform { Position = position, Rotation = rotation, Scale = 1f });
                world.EntityManager.AddComponentData(bodyEntity, new LocalToWorld { Value = float4x4.TRS(position, rotation, new float3(1f, 1f, 1f)) });
                world.EntityManager.AddComponentData(bodyEntity, new PhysicsCollider { Value = collider });
                world.EntityManager.AddSharedComponent(bodyEntity, new PhysicsWorldIndex { Value = worldIndex });
                world.EntityManager.AddBuffer<PhysicsColliderKeyEntityPair>(bodyEntity);

                if (addTrackedTransform)
                {
                    RigidTransform currentTransform = new RigidTransform(rotation, position);
                    TrackedTransform trackedTransform = new TrackedTransform
                    {
                        CurrentFixedRateTransform = currentTransform,
                        PreviousFixedRateTransform = currentTransform,
                    };
                    world.EntityManager.AddComponentData(bodyEntity, trackedTransform);
                }

                switch (motionType)
                {
                    case BodyMotionType.Dynamic:
                        world.EntityManager.AddComponentData(bodyEntity, new PhysicsVelocity());
                        world.EntityManager.AddComponentData(bodyEntity, PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 1f));
                        world.EntityManager.AddComponentData(bodyEntity, new PhysicsGravityFactor { Value = 1f });
                        world.EntityManager.AddComponentData(bodyEntity, new PhysicsCustomTags { });
                        break;
                    case BodyMotionType.Kinematic:
                        world.EntityManager.AddComponentData(bodyEntity, new PhysicsVelocity());
                        world.EntityManager.AddComponentData(bodyEntity, PhysicsMass.CreateKinematic(MassProperties.UnitSphere));
                        world.EntityManager.AddComponentData(bodyEntity, new PhysicsGravityFactor { Value = 0f });
                        world.EntityManager.AddComponentData(bodyEntity, new PhysicsCustomTags { });
                        break;
                    case BodyMotionType.Static:
                    default:
                        break;
                }
            }

            return bodyEntity;
        }

        public static bool GetWorldIndex(World world, out uint index)
        {
            index = 0;
            for (int i = 0; i < World.All.Count; i++)
            {
                if (World.All[i] == world)
                {
                    index = Convert.ToUInt32(i);
                    return true;
                }
            }
            return false;
        }

        public static void AddSystemToGroup<TGroup>(World world, ComponentSystemBase system) where TGroup : ComponentSystemGroup
        {
            world.AddSystemManaged(system);
            world.GetOrCreateSystemManaged<TGroup>().AddSystemToUpdateList(system);
            world.GetOrCreateSystemManaged<TGroup>().SortSystems();
        }

        public static bool TryGetSingleton<T>(World world, out T singleton, out Entity entity) where T : unmanaged, IComponentData
        {
            EntityQuery query = new EntityQueryBuilder(Allocator.Temp).WithAll<T>().Build(world.EntityManager);
            if (query.HasSingleton<T>())
            {
                singleton = query.GetSingleton<T>();
                entity = query.GetSingletonEntity();
                return true;
            }

            singleton = default;
            entity = default;
            return false;
        }

        public static void BuildPhysicsWorld(World world, out PhysicsWorldSingleton physicsWorldSingleton)
        {
            world.GetOrCreateSystem<PhysicsInitializeGroup>().Update(world.Unmanaged);
            world.EntityManager.CompleteAllTrackedJobs();
            TryGetSingleton(world, out physicsWorldSingleton, out Entity physicsWorldEntity);
        }

        public static void StepPhysicsWorld(World world, out PhysicsWorldSingleton physicsWorldSingleton)
        {
            world.GetOrCreateSystem<PhysicsSimulationGroup>().Update(world.Unmanaged);
            world.GetOrCreateSystem<ExportPhysicsWorld>().Update(world.Unmanaged);
            world.EntityManager.CompleteAllTrackedJobs();
            TryGetSingleton(world, out physicsWorldSingleton, out Entity physicsWorldEntity);
        }

        public static void UpdateTrackedTransforms(World world)
        {
            world.GetOrCreateSystem<TrackedTransformFixedSimulationSystem>().Update(world.Unmanaged);
            world.EntityManager.CompleteAllTrackedJobs();
        }

        public static void UpdateDeferredImpulses(World world)
        {
            world.GetOrCreateSystem<KinematicCharacterDeferredImpulsesSystem>().Update(world.Unmanaged);
            world.EntityManager.CompleteAllTrackedJobs();
        }

        public static void GetCharacterAspectWithContexts(World world, Entity characterEntity, PhysicsWorldSingleton physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context)
        {
            SimulationSystemGroup simulationSystem = world.GetOrCreateSystemManaged<SimulationSystemGroup>();

            context = new TestCharacterUpdateContext();
            context.OnSystemCreate(ref simulationSystem.CheckedStateRef);
            context.OnSystemUpdate(ref simulationSystem.CheckedStateRef);
            baseContext = new KinematicCharacterUpdateContext();
            baseContext.OnSystemCreate(ref simulationSystem.CheckedStateRef);
            baseContext.OnSystemUpdate(ref simulationSystem.CheckedStateRef, world.Time, physicsWorldSingleton);
            baseContext.EnsureCreationOfTmpCollections();

            characterAspect = world.EntityManager.GetAspect<TestCharacterAspect>(characterEntity);
        }

        public static void DisposeCharacterContext(ref KinematicCharacterUpdateContext baseContext)
        {
            baseContext.TmpDistanceHits.Dispose();
            baseContext.TmpRigidbodyIndexesProcessed.Dispose();
            baseContext.TmpRaycastHits.Dispose();
            baseContext.TmpColliderCastHits.Dispose();
        }

        public static void MoveCharacterToClosestCollideableInDirection(World world, Entity characterEntity, PhysicsWorldSingleton physicsWorldSingleton, float3 direction, float range = 1000f)
        {
            if (CastCharacterShapeClosest(world, characterEntity, physicsWorldSingleton, direction, range, out ColliderCastHit hit, out float hitDistance))
            {
                LocalTransform characterTransform = world.EntityManager.GetComponentData<LocalTransform>(characterEntity);
                characterTransform.Position += (direction * (hitDistance - KinematicCharacterAspect.Constants.CollisionOffset));
                world.EntityManager.SetComponentData(characterEntity, characterTransform);
            }
        }

        public static void SetCharacterVelocity(World world, Entity characterEntity, float3 velocity)
        {
            KinematicCharacterBody characterBody = world.EntityManager.GetComponentData<KinematicCharacterBody>(characterEntity);
            characterBody.RelativeVelocity = velocity;
            world.EntityManager.SetComponentData(characterEntity, characterBody);
        }

        public static void SetCharacterPosition(World world, Entity characterEntity, float3 position)
        {
            LocalTransform characterTransform = world.EntityManager.GetComponentData<LocalTransform>(characterEntity);
            characterTransform.Position = position;
            world.EntityManager.SetComponentData(characterEntity, characterTransform);
        }

        public static void SetCharacterRotation(World world, Entity characterEntity, quaternion rotation)
        {
            LocalTransform characterTransform = world.EntityManager.GetComponentData<LocalTransform>(characterEntity);
            characterTransform.Rotation = rotation;
            world.EntityManager.SetComponentData(characterEntity, characterTransform);
        }

        public static bool CastCharacterShapeClosest(World world, Entity characterEntity, PhysicsWorldSingleton physicsWorldSingleton, float3 castDirection, float castLength, out ColliderCastHit hit, out float hitDistance)
        {
            GetCharacterAspectWithContexts(world, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);

            bool foundHit = characterAspect.CharacterAspect.CastColliderClosestCollisions(
                in characterAspect,
                ref context,
                ref baseContext,
                world.EntityManager.GetComponentData<LocalTransform>(characterEntity).Position,
                world.EntityManager.GetComponentData<LocalTransform>(characterEntity).Rotation,
                world.EntityManager.GetComponentData<LocalTransform>(characterEntity).Scale,
                castDirection,
                castLength,
                true,
                false,
                out hit,
                out hitDistance);

            DisposeCharacterContext(ref baseContext);

            return foundHit;
        }

        public static RigidTransform ToRigidTransform(this LocalTransform localTransform)
        {
            return new RigidTransform
            {
                pos = localTransform.Position,
                rot = localTransform.Rotation,
            };
        }

        public static bool IsRoughlyEqual(this RigidTransform transform, RigidTransform toTransform)
        {
            return transform.pos.IsRoughlyEqual(toTransform.pos) && transform.rot.IsRoughlyEqual(toTransform.rot);
        }

        public static bool IsRoughlyEqual(this float val, float toVal, float error = 0.005f)
        {
            return math.distance(val, toVal) < error;
        }

        public static bool IsRoughlyEqual(this float3 vector, float3 toVector, float error = 0.005f)
        {
            return vector.x.IsRoughlyEqual(toVector.x, error) &&
                   vector.y.IsRoughlyEqual(toVector.y, error) &&
                   vector.z.IsRoughlyEqual(toVector.z, error);
        }

        public static bool IsRoughlyEqual(this quaternion quat, quaternion toQuat, float error = 0.005f)
        {
            return quat.value.x.IsRoughlyEqual(toQuat.value.x, error) &&
                   quat.value.y.IsRoughlyEqual(toQuat.value.y, error) &&
                   quat.value.z.IsRoughlyEqual(toQuat.value.z, error) &&
                   quat.value.w.IsRoughlyEqual(toQuat.value.w, error);
        }
    }
}
