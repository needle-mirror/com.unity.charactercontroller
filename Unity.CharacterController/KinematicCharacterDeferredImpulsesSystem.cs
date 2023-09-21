using Unity.Entities;
using Unity.Physics;
using Unity.Transforms;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

namespace Unity.CharacterController
{
    /// <summary>
    /// Handles applying impulses that were detected during the character update
    /// </summary>
    [UpdateInGroup(typeof(KinematicCharacterPhysicsUpdateGroup), OrderLast = true)]
    [BurstCompile]
    public partial struct KinematicCharacterDeferredImpulsesSystem : ISystem
    {
        private EntityQuery _characterQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _characterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder().Build(ref state);
            state.RequireForUpdate(_characterQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            KinematicCharacterDeferredImpulsesJob job = new KinematicCharacterDeferredImpulsesJob
            {
                TransformLookup = SystemAPI.GetComponentLookup<LocalTransform>(false),
                PhysicsVelocityLookup = SystemAPI.GetComponentLookup<PhysicsVelocity>(false),
                CharacterBodyLookup = SystemAPI.GetComponentLookup<KinematicCharacterBody>(false),
                CharacterPropertiesLookup = SystemAPI.GetComponentLookup<KinematicCharacterProperties>(true),
            };
            job.Schedule();
        }

        /// <summary>
        /// Job that processes deferred character impulses (applies them to rigidbodies and characters)
        /// </summary>
        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct KinematicCharacterDeferredImpulsesJob : IJobEntity
        {
            /// <summary>
            /// Lookup for the transforms component
            /// </summary>
            public ComponentLookup<LocalTransform> TransformLookup;
            /// <summary>
            /// Lookup for the physics velocity component
            /// </summary>
            public ComponentLookup<PhysicsVelocity> PhysicsVelocityLookup;
            /// <summary>
            /// Lookup for the character body component
            /// </summary>
            public ComponentLookup<KinematicCharacterBody> CharacterBodyLookup;
            /// <summary>
            /// Lookup for the character properties component
            /// </summary>
            [ReadOnly]
            public ComponentLookup<KinematicCharacterProperties> CharacterPropertiesLookup;

            void Execute(in DynamicBuffer<KinematicCharacterDeferredImpulse> characterDeferredImpulsesBuffer)
            {
                for (int deferredImpulseIndex = 0; deferredImpulseIndex < characterDeferredImpulsesBuffer.Length; deferredImpulseIndex++)
                {
                    KinematicCharacterDeferredImpulse deferredImpulse = characterDeferredImpulsesBuffer[deferredImpulseIndex];

                    // Impulse
                    bool isImpulseOnCharacter = CharacterPropertiesLookup.HasComponent(deferredImpulse.OnEntity);
                    if (isImpulseOnCharacter)
                    {
                        KinematicCharacterProperties hitCharacterProperties = CharacterPropertiesLookup[deferredImpulse.OnEntity];
                        if (hitCharacterProperties.SimulateDynamicBody)
                        {
                            KinematicCharacterBody hitCharacterBody = CharacterBodyLookup[deferredImpulse.OnEntity];
                            hitCharacterBody.RelativeVelocity += deferredImpulse.LinearVelocityChange;
                            CharacterBodyLookup[deferredImpulse.OnEntity] = hitCharacterBody;
                        }
                    }
                    else
                    {
                        if (PhysicsVelocityLookup.TryGetComponent(deferredImpulse.OnEntity, out PhysicsVelocity bodyPhysicsVelocity))
                        {
                            bodyPhysicsVelocity.Linear += deferredImpulse.LinearVelocityChange;
                            bodyPhysicsVelocity.Angular += deferredImpulse.AngularVelocityChange;

                            PhysicsVelocityLookup[deferredImpulse.OnEntity] = bodyPhysicsVelocity;
                        }
                    }

                    // Displacement
                    if (math.lengthsq(deferredImpulse.Displacement) > 0f)
                    {
                        if (TransformLookup.TryGetComponent(deferredImpulse.OnEntity, out LocalTransform bodyTransform))
                        {
                            bodyTransform.Position += deferredImpulse.Displacement;
                            TransformLookup[deferredImpulse.OnEntity] = bodyTransform;
                        }
                    }
                }
            }
        }
    }
}
