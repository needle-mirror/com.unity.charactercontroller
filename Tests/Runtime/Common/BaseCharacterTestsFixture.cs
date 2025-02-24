using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Core;
using Unity.Entities;
using Unity.Physics;
using UnityEngine;

namespace Unity.CharacterController.RuntimeTests
{
    [TestFixture]
    public abstract class BaseCharacterTestsFixture
    {
        public World World => World.DefaultGameObjectInjectionWorld;

        private const float SimulationDeltaTime = 0.02f;

        [SetUp]
        public void SetUp()
        {
            World.GetExistingSystemManaged<SimulationSystemGroup>().RateManager = new RateUtils.FixedRateSimpleManager(SimulationDeltaTime);
            World.GetExistingSystemManaged<FixedStepSimulationSystemGroup>().RateManager = null;
            World.Time = new TimeData(0f, SimulationDeltaTime);

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physWorldSingleton);
        }

        [TearDown]
        public void TearDown()
        {
            CharacterTestUtils.DestroyAllTestEntities();
        }
    }
}