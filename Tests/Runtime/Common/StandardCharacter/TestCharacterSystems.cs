using Unity.Burst;
using Unity.Entities;
using Unity.Collections;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Transforms;
using Unity.CharacterController;
using Unity.Burst.Intrinsics;

namespace Unity.CharacterController.RuntimeTests
{
    [UpdateInGroup(typeof(KinematicCharacterPhysicsUpdateGroup))]
    [BurstCompile]
    public partial struct TestCharacterPhysicsUpdateSystem : ISystem
    {
        private EntityQuery _characterQuery;
        private TestCharacterUpdateContext _context;
        private KinematicCharacterUpdateContext _baseContext;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _characterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder()
                .WithAll<
                    TestCharacterComponent,
                    TestCharacterControl>()
                .Build(ref state);

            _context = new TestCharacterUpdateContext();
            _context.OnSystemCreate(ref state);
            _baseContext = new KinematicCharacterUpdateContext();
            _baseContext.OnSystemCreate(ref state);

            state.RequireForUpdate(_characterQuery);
            state.RequireForUpdate<PhysicsWorldSingleton>();
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _context.OnSystemUpdate(ref state);
            _baseContext.OnSystemUpdate(ref state, SystemAPI.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());

            TestCharacterPhysicsUpdateJob job = new TestCharacterPhysicsUpdateJob
            {
                Context = _context,
                BaseContext = _baseContext,
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct TestCharacterPhysicsUpdateJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public TestCharacterUpdateContext Context;
            public KinematicCharacterUpdateContext BaseContext;

            void Execute(TestCharacterAspect characterAspect)
            {
                characterAspect.PhysicsUpdate(ref Context, ref BaseContext);
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BaseContext.EnsureCreationOfTmpCollections();
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
            }
        }
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateAfter(typeof(FixedStepSimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    [BurstCompile]
    public partial struct TestCharacterVariableUpdateSystem : ISystem
    {
        private EntityQuery _characterQuery;
        private TestCharacterUpdateContext _context;
        private KinematicCharacterUpdateContext _baseContext;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            _characterQuery = KinematicCharacterUtilities.GetBaseCharacterQueryBuilder()
                .WithAll<
                    TestCharacterComponent,
                    TestCharacterControl>()
                .Build(ref state);

            _context = new TestCharacterUpdateContext();
            _context.OnSystemCreate(ref state);
            _baseContext = new KinematicCharacterUpdateContext();
            _baseContext.OnSystemCreate(ref state);

            state.RequireForUpdate(_characterQuery);
        }

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            _context.OnSystemUpdate(ref state);
            _baseContext.OnSystemUpdate(ref state, SystemAPI.Time, SystemAPI.GetSingleton<PhysicsWorldSingleton>());

            TestCharacterVariableUpdateJob job = new TestCharacterVariableUpdateJob
            {
                Context = _context,
                BaseContext = _baseContext,
            };
            job.ScheduleParallel();
        }

        [BurstCompile]
        [WithAll(typeof(Simulate))]
        public partial struct TestCharacterVariableUpdateJob : IJobEntity, IJobEntityChunkBeginEnd
        {
            public TestCharacterUpdateContext Context;
            public KinematicCharacterUpdateContext BaseContext;

            void Execute(TestCharacterAspect characterAspect)
            {
                characterAspect.VariableUpdate(ref Context, ref BaseContext);
            }

            public bool OnChunkBegin(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask)
            {
                BaseContext.EnsureCreationOfTmpCollections();
                return true;
            }

            public void OnChunkEnd(in ArchetypeChunk chunk, int unfilteredChunkIndex, bool useEnabledMask, in v128 chunkEnabledMask, bool chunkWasExecuted)
            {
            }
        }
    }
}
