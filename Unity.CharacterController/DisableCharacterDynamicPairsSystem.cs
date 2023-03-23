using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;

namespace Unity.CharacterController
{
    /// <summary>
    /// Singleton that enables the character dynamic body pairs job to run
    /// </summary>
    public struct DisableCharacterDynamicPairs : IComponentData
    {
    }

    /// <summary>
    /// System scheduling a job that disables body pairs between dynamic characters and dynamic bodies
    /// </summary>
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    [UpdateAfter(typeof(PhysicsCreateBodyPairsGroup))]
    [UpdateBefore(typeof(PhysicsCreateContactsGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct DisableCharacterDynamicPairsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Create singleton 
            Entity singleton = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(singleton, new DisableCharacterDynamicPairs());

            EntityQuery characterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder().Build(ref state);

            state.RequireForUpdate(characterQuery);
            state.RequireForUpdate<DisableCharacterDynamicPairs>();
            state.RequireForUpdate<PhysicsWorldSingleton>();
            state.RequireForUpdate<SimulationSingleton>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            PhysicsWorld physicsWorld = SystemAPI.GetSingletonRW<PhysicsWorldSingleton>().ValueRW.PhysicsWorld;
            SimulationSingleton simulationSingleton = SystemAPI.GetSingleton<SimulationSingleton>();

            if (physicsWorld.Bodies.Length > 0)
            {
                DisableCharacterDynamicPairsJob job = new DisableCharacterDynamicPairsJob
                {
                    NumDynamicBodies = physicsWorld.NumDynamicBodies,
                    MotionVelocities = physicsWorld.MotionVelocities,
                    StoredKinematicCharacterDataLookup = SystemAPI.GetComponentLookup<StoredKinematicCharacterData>(true),
                };
                state.Dependency = job.Schedule(simulationSingleton, ref physicsWorld, state.Dependency);
            }
        }

        /// <summary>
        /// Disables body pairs between dynamic characters and dynamic bodies
        /// </summary>
        [BurstCompile]
        public struct DisableCharacterDynamicPairsJob : IBodyPairsJob
        {
            public int NumDynamicBodies;
            [ReadOnly] public NativeArray<MotionVelocity> MotionVelocities;
            [ReadOnly] public ComponentLookup<StoredKinematicCharacterData> StoredKinematicCharacterDataLookup;

            public unsafe void Execute(ref ModifiableBodyPair pair)
            {
                // Both should be non-static
                if (pair.BodyIndexA < NumDynamicBodies && pair.BodyIndexB < NumDynamicBodies)
                {
                    bool aIsKinematic = MotionVelocities[pair.BodyIndexA].IsKinematic;
                    bool bIsKinematic = MotionVelocities[pair.BodyIndexB].IsKinematic;

                    // One should be kinematic and the other should not
                    if (aIsKinematic != bIsKinematic)
                    {
                        Entity kinematicEntity = aIsKinematic ? pair.EntityA : pair.EntityB;

                        // Disable pair if kinematic entity is character.
                        if (StoredKinematicCharacterDataLookup.TryGetComponent(kinematicEntity, out StoredKinematicCharacterData characterData))
                        {
                            if (characterData.SimulateDynamicBody)
                            {
                                pair.Disable();
                            }
                        }
                    }
                }
            }
        }
    }
}