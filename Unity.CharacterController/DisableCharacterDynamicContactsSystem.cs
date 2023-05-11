using System.Collections;
using System.Collections.Generic;
using Unity.Burst;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Extensions;
using Unity.Physics.Systems;

namespace Unity.CharacterController
{
    /// <summary>
    /// Singleton that enables the character dynamic contacts filtering job to run
    /// </summary>
    public struct DisableCharacterDynamicContacts : IComponentData
    { }

    /// <summary>
    /// System scheduling a job that disables contacts between dynamic characters and dynamic colliders
    /// </summary>
    [UpdateInGroup(typeof(PhysicsSimulationGroup))]
    [UpdateAfter(typeof(PhysicsCreateContactsGroup))]
    [UpdateBefore(typeof(PhysicsCreateJacobiansGroup))]
    [RequireMatchingQueriesForUpdate]
    public partial struct DisableCharacterDynamicContactsSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            // Create singleton
            Entity singleton = state.EntityManager.CreateEntity();
            state.EntityManager.AddComponentData(singleton, new DisableCharacterDynamicContacts());

            EntityQuery characterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder().Build(ref state);

            state.RequireForUpdate(characterQuery);
            state.RequireForUpdate<DisableCharacterDynamicContacts>();
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
                DisableCharacterDynamicContactsJob job = new DisableCharacterDynamicContactsJob
                {
                    PhysicsWorld = physicsWorld,
                    StoredKinematicCharacterDataLookup = SystemAPI.GetComponentLookup<StoredKinematicCharacterData>(true),
                };
                state.Dependency = job.Schedule(simulationSingleton, ref physicsWorld, state.Dependency);
            }
        }

        /// <summary>
        /// Disables body pairs between dynamic characters and dynamic bodies
        /// </summary>
        [BurstCompile]
        public struct DisableCharacterDynamicContactsJob : IContactsJob
        {
            /// <summary>
            /// The physics world that the characters belong to
            /// </summary>
            [ReadOnly]
            public PhysicsWorld PhysicsWorld;
            /// <summary>
            /// Lookup for <see cref="StoredKinematicCharacterData"/>
            /// </summary>
            [ReadOnly]
            public ComponentLookup<StoredKinematicCharacterData> StoredKinematicCharacterDataLookup;

            public unsafe void Execute(ref ModifiableContactHeader manifold, ref ModifiableContactPoint contact)
            {
                // Both should be non-static
                if (manifold.BodyIndexA < PhysicsWorld.NumDynamicBodies && manifold.BodyIndexB < PhysicsWorld.NumDynamicBodies)
                {
                    bool aIsKinematic = PhysicsWorld.MotionVelocities[manifold.BodyIndexA].IsKinematic;
                    bool bIsKinematic = PhysicsWorld.MotionVelocities[manifold.BodyIndexB].IsKinematic;

                    // One should be kinematic and the other should be dynamic
                    if (aIsKinematic != bIsKinematic)
                    {
                        Entity kinematicEntity;
                        int dynamicBodyIndex;
                        ColliderKey dynamicBodyColliderKey;
                        if (aIsKinematic)
                        {
                            kinematicEntity = manifold.EntityA;
                            dynamicBodyIndex = manifold.BodyIndexB;
                            dynamicBodyColliderKey = manifold.ColliderKeyB;
                        }
                        else
                        {
                            kinematicEntity = manifold.EntityB;
                            dynamicBodyIndex = manifold.BodyIndexA;
                            dynamicBodyColliderKey = manifold.ColliderKeyA;
                        }

                        // Disable only if dynamic entity is collidable
                        CollisionResponsePolicy dynamicBodyCollisionResponse = PhysicsWorld.Bodies[dynamicBodyIndex].Collider.Value.GetCollisionResponse(dynamicBodyColliderKey);
                        if (dynamicBodyCollisionResponse == CollisionResponsePolicy.Collide || dynamicBodyCollisionResponse == CollisionResponsePolicy.CollideRaiseCollisionEvents)
                        {
                            // Disable only if kinematic entity is character and is simulated dynamic
                            if (StoredKinematicCharacterDataLookup.TryGetComponent(kinematicEntity, out StoredKinematicCharacterData characterData) &&
                                characterData.SimulateDynamicBody)
                            {
                                manifold.JacobianFlags |= JacobianFlags.Disabled;
                            }
                        }
                    }
                }
            }
        }
    }
}
