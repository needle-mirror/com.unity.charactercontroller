using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;

namespace Unity.CharacterController
{
    /// <summary>
    /// A system that stores key character data in a component on the character entity, before the character update
    /// </summary>
    [UpdateInGroup(typeof(KinematicCharacterPhysicsUpdateGroup), OrderFirst = true)]
    [BurstCompile]
    public partial struct StoreKinematicCharacterBodyPropertiesSystem : ISystem
    {
        private EntityQuery _storedCharacterQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _storedCharacterQuery = new EntityQueryBuilder(Allocator.Temp)
                .WithAll<StoredKinematicCharacterData, KinematicCharacterProperties>()
                .Build(ref state);

            state.RequireForUpdate(_storedCharacterQuery);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        { }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            StoreKinematicCharacterBodyPropertiesJob job = new StoreKinematicCharacterBodyPropertiesJob();
            job.ScheduleParallel();
        }

        /// <summary>
        /// Job that copies character data to another component on the same entity, to capture a snapshot of them before modifications.
        /// This exists to allow deterministic parallel character updates
        /// </summary>
        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct StoreKinematicCharacterBodyPropertiesJob : IJobEntity
        {
            void Execute(ref StoredKinematicCharacterData storedData, in KinematicCharacterProperties characterProperties, in KinematicCharacterBody characterBody)
            {
                storedData.SetFrom(in characterProperties, in characterBody);
            }
        }
    }
}