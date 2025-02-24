using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Unity.CharacterController;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Transforms;
using Collider = Unity.Physics.Collider;
using Material = Unity.Physics.Material;
using SphereCollider = Unity.Physics.SphereCollider;

namespace Unity.CharacterController.RuntimeTests
{
    class PhysicsUtilitiesTests : BaseCharacterTestsFixture
    {
        [Test]
        public void DoesBodyHavePhysicsVelocityAndMass()
        {
            Entity bodyStatic = CharacterTestUtils.CreateSphereBody(World, default, quaternion.identity, 1f, BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity bodyDynamic = CharacterTestUtils.CreateSphereBody(World, default, quaternion.identity, 1f, BodyMotionType.Dynamic, CollisionResponsePolicy.Collide, false);
            Entity bodyKinematic = CharacterTestUtils.CreateSphereBody(World, default, quaternion.identity, 1f, BodyMotionType.Kinematic, CollisionResponsePolicy.Collide, false);

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            Assert.IsFalse(PhysicsUtilities.DoesBodyHavePhysicsVelocityAndMass(in physicsWorldSingleton.PhysicsWorld, physicsWorldSingleton.PhysicsWorld.GetRigidBodyIndex(bodyStatic)));
            Assert.IsTrue(PhysicsUtilities.DoesBodyHavePhysicsVelocityAndMass(in physicsWorldSingleton.PhysicsWorld, physicsWorldSingleton.PhysicsWorld.GetRigidBodyIndex(bodyDynamic)));
            Assert.IsTrue(PhysicsUtilities.DoesBodyHavePhysicsVelocityAndMass(in physicsWorldSingleton.PhysicsWorld, physicsWorldSingleton.PhysicsWorld.GetRigidBodyIndex(bodyKinematic)));
        }

        [Test]
        public void IsBodyDynamic()
        {
            Entity bodyStatic = CharacterTestUtils.CreateSphereBody(World, default, quaternion.identity, 1f, BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity bodyDynamic = CharacterTestUtils.CreateSphereBody(World, default, quaternion.identity, 1f, BodyMotionType.Dynamic, CollisionResponsePolicy.Collide, false);
            Entity bodyKinematic = CharacterTestUtils.CreateSphereBody(World, default, quaternion.identity, 1f, BodyMotionType.Kinematic, CollisionResponsePolicy.Collide, false);

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            Assert.IsFalse(PhysicsUtilities.IsBodyDynamic(in physicsWorldSingleton.PhysicsWorld, physicsWorldSingleton.PhysicsWorld.GetRigidBodyIndex(bodyStatic)));
            Assert.IsTrue(PhysicsUtilities.IsBodyDynamic(in physicsWorldSingleton.PhysicsWorld, physicsWorldSingleton.PhysicsWorld.GetRigidBodyIndex(bodyDynamic)));
            Assert.IsFalse(PhysicsUtilities.IsBodyDynamic(in physicsWorldSingleton.PhysicsWorld, physicsWorldSingleton.PhysicsWorld.GetRigidBodyIndex(bodyKinematic)));
        }

        [Test]
        public unsafe void HasPhysicsTag()
        {
            void SetTagsM(ref PhysicsCustomTags tags, bool t0, bool t1, bool t2, bool t3, bool t4, bool t5, bool t6, bool t7)
            {
                byte values = tags.Value;
                SetTags(ref values, t0, t1, t2, t3, t4, t5, t6, t7);
                tags.Value = values;
            }

            void SetTags(ref byte tags, bool t0, bool t1, bool t2, bool t3, bool t4, bool t5, bool t6, bool t7)
            {
                int tmpTags = 0;
                tmpTags |= (t0 ? 1 : 0) << 0;
                tmpTags |= (t1 ? 1 : 0) << 1;
                tmpTags |= (t2 ? 1 : 0) << 2;
                tmpTags |= (t3 ? 1 : 0) << 3;
                tmpTags |= (t4 ? 1 : 0) << 4;
                tmpTags |= (t5 ? 1 : 0) << 5;
                tmpTags |= (t6 ? 1 : 0) << 6;
                tmpTags |= (t7 ? 1 : 0) << 7;
                tags = (byte)tmpTags;
            }

            void SetTagsOnEntityAndUpdate(Entity entity, ref NativeList<DistanceHit> tmpHits, ref PhysicsWorldSingleton physicsWorldSingleton, bool t0, bool t1, bool t2, bool t3, bool t4, bool t5, bool t6, bool t7, out Material hitMaterial)
            {
                PhysicsCollider physicsCollider = World.EntityManager.GetComponentData<PhysicsCollider>(entity);
                SphereCollider* sphereCollider = (SphereCollider*)physicsCollider.ColliderPtr;
                Material collMaterial = sphereCollider->Material;
                SetTags(ref collMaterial.CustomTags, t0, t1, t2, t3, t4, t5, t6, t7);
                sphereCollider->Material = collMaterial;

                CharacterTestUtils.BuildPhysicsWorld(World, out physicsWorldSingleton);

                tmpHits.Clear();
                physicsWorldSingleton.OverlapSphere(default, 100f, ref tmpHits, CollisionFilter.Default, QueryInteraction.Default);
                Assert.AreEqual(1, tmpHits.Length);

                hitMaterial = tmpHits[0].Material;
            }

            NativeList<DistanceHit> tmpHits = new NativeList<DistanceHit>(100, Allocator.Temp);
            Entity bodyA = CharacterTestUtils.CreateSphereBody(World, default, quaternion.identity, 1f, BodyMotionType.Kinematic, CollisionResponsePolicy.Collide, false);
            PhysicsCustomTags hasTag = default;
            Material bodyMaterial = default;
            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            // --------------------------------------------------------------------------------------

            SetTagsM(ref hasTag, false, false, false, false, false, false, false, false);
            SetTagsOnEntityAndUpdate(bodyA, ref tmpHits, ref physicsWorldSingleton, false, false, false, false, false, false, false, false, out bodyMaterial);
            Assert.IsTrue(PhysicsUtilities.HasPhysicsTag(bodyMaterial, hasTag));

            // --------------------------------------------------------------------------------------

            SetTagsM(ref hasTag, true, true, true, true, true, true, true, true);
            SetTagsOnEntityAndUpdate(bodyA, ref tmpHits, ref physicsWorldSingleton, false, false, false, false, false, false, false, false, out bodyMaterial);
            Assert.IsFalse(PhysicsUtilities.HasPhysicsTag(bodyMaterial, hasTag));

            // --------------------------------------------------------------------------------------

            SetTagsM(ref hasTag, false, false, false, false, false, false, false, false);
            SetTagsOnEntityAndUpdate(bodyA, ref tmpHits, ref physicsWorldSingleton, true, true, true, true, true, true, true, true, out bodyMaterial);
            Assert.IsTrue(PhysicsUtilities.HasPhysicsTag(bodyMaterial, hasTag));

            // --------------------------------------------------------------------------------------

            SetTagsM(ref hasTag, true, true, true, true, true, true, true, true);
            SetTagsOnEntityAndUpdate(bodyA, ref tmpHits, ref physicsWorldSingleton, true, true, true, true, true, true, true, true, out bodyMaterial);
            Assert.IsTrue(PhysicsUtilities.HasPhysicsTag(bodyMaterial, hasTag));

            // --------------------------------------------------------------------------------------

            SetTagsM(ref hasTag, true, false, false, false, false, false, false, false);
            SetTagsOnEntityAndUpdate(bodyA, ref tmpHits, ref physicsWorldSingleton, false, false, false, false, false, false, false, false, out bodyMaterial);
            Assert.IsFalse(PhysicsUtilities.HasPhysicsTag(bodyMaterial, hasTag));

            // --------------------------------------------------------------------------------------

            SetTagsM(ref hasTag, false, false, false, false, false, false, false, false);
            SetTagsOnEntityAndUpdate(bodyA, ref tmpHits, ref physicsWorldSingleton, true, false, false, false, false, false, false, false, out bodyMaterial);
            Assert.IsTrue(PhysicsUtilities.HasPhysicsTag(bodyMaterial, hasTag));

            // --------------------------------------------------------------------------------------

            SetTagsM(ref hasTag, true, false, false, false, false, false, false, false);
            SetTagsOnEntityAndUpdate(bodyA, ref tmpHits, ref physicsWorldSingleton, true, false, false, false, false, false, false, false, out bodyMaterial);
            Assert.IsTrue(PhysicsUtilities.HasPhysicsTag(bodyMaterial, hasTag));

            // --------------------------------------------------------------------------------------

            SetTagsM(ref hasTag, false, false, false, true, false, false, false, false);
            SetTagsOnEntityAndUpdate(bodyA, ref tmpHits, ref physicsWorldSingleton, false, false, false, false, true, false, false, false, out bodyMaterial);
            Assert.IsFalse(PhysicsUtilities.HasPhysicsTag(bodyMaterial, hasTag));

            // --------------------------------------------------------------------------------------

            SetTagsM(ref hasTag, false, false, false, true, true, false, false, false);
            SetTagsOnEntityAndUpdate(bodyA, ref tmpHits, ref physicsWorldSingleton, false, false, false, false, true, false, false, false, out bodyMaterial);
            Assert.IsFalse(PhysicsUtilities.HasPhysicsTag(bodyMaterial, hasTag));

            // --------------------------------------------------------------------------------------

            SetTagsM(ref hasTag, false, false, false, false, true, false, false, false);
            SetTagsOnEntityAndUpdate(bodyA, ref tmpHits, ref physicsWorldSingleton, false, false, false, true, true, false, false, false, out bodyMaterial);
            Assert.IsTrue(PhysicsUtilities.HasPhysicsTag(bodyMaterial, hasTag));

            tmpHits.Dispose();
        }

        [Test]
        public unsafe void GetBodyComponents()
        {
            Entity bodyA = CharacterTestUtils.CreateTestEntity(World);
            Entity bodyB = CharacterTestUtils.CreateSphereBody(World, math.up(), quaternion.identity, 1f, BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity bodyC = CharacterTestUtils.CreateSphereBody(World, math.up(), quaternion.identity, 1f, BodyMotionType.Kinematic, CollisionResponsePolicy.Collide, false);
            Entity bodyD = CharacterTestUtils.CreateSphereBody(World, math.up(), quaternion.identity, 1f, BodyMotionType.Dynamic, CollisionResponsePolicy.Collide, false);

            PhysicsVelocity velC = World.EntityManager.GetComponentData<PhysicsVelocity>(bodyC);
            velC.Linear = math.right();
            World.EntityManager.SetComponentData(bodyC, velC);
            PhysicsVelocity velD = World.EntityManager.GetComponentData<PhysicsVelocity>(bodyD);
            velD.Linear = math.right();
            World.EntityManager.SetComponentData(bodyD, velD);

            PhysicsMass massC = World.EntityManager.GetComponentData<PhysicsMass>(bodyC);
            massC.InverseMass = 5f;
            World.EntityManager.SetComponentData(bodyC, massC);
            PhysicsMass massD = World.EntityManager.GetComponentData<PhysicsMass>(bodyD);
            massD.InverseMass = 5f;
            World.EntityManager.SetComponentData(bodyD, massD);

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);
            bool result = false;

            result = PhysicsUtilities.GetBodyComponents(in physicsWorldSingleton.PhysicsWorld, physicsWorldSingleton.GetRigidBodyIndex(bodyA), out LocalTransform transform, out PhysicsVelocity physicsVelocity, out PhysicsMass physicsMass);
            Assert.IsFalse(result);

            result = PhysicsUtilities.GetBodyComponents(in physicsWorldSingleton.PhysicsWorld, physicsWorldSingleton.GetRigidBodyIndex(bodyB), out transform, out physicsVelocity, out physicsMass);
            Assert.IsFalse(result);

            result = PhysicsUtilities.GetBodyComponents(in physicsWorldSingleton.PhysicsWorld, physicsWorldSingleton.GetRigidBodyIndex(bodyC), out transform, out physicsVelocity, out physicsMass);
            Assert.IsTrue(result);
            Assert.IsTrue(transform.Position.IsRoughlyEqual(math.up()));
            Assert.IsTrue(physicsVelocity.Linear.IsRoughlyEqual(math.right()));
            Assert.IsTrue(physicsMass.InverseMass.IsRoughlyEqual(5f));

            result = PhysicsUtilities.GetBodyComponents(in physicsWorldSingleton.PhysicsWorld, physicsWorldSingleton.GetRigidBodyIndex(bodyD), out transform, out physicsVelocity, out physicsMass);
            Assert.IsTrue(result);
            Assert.IsTrue(transform.Position.IsRoughlyEqual(math.up()));
            Assert.IsTrue(physicsVelocity.Linear.IsRoughlyEqual(math.right()));
            Assert.IsTrue(physicsMass.InverseMass.IsRoughlyEqual(5f));
        }

        [Test]
        public unsafe void IsCollidable()
        {
            Entity bodyA = CharacterTestUtils.CreateSphereBody(World, math.up(), quaternion.identity, 1f, BodyMotionType.Static, CollisionResponsePolicy.None, false);
            Entity bodyB = CharacterTestUtils.CreateSphereBody(World, math.up(), quaternion.identity, 1f, BodyMotionType.Static, CollisionResponsePolicy.RaiseTriggerEvents, false);
            Entity bodyC = CharacterTestUtils.CreateSphereBody(World, math.up(), quaternion.identity, 1f, BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity bodyD = CharacterTestUtils.CreateSphereBody(World, math.up(), quaternion.identity, 1f, BodyMotionType.Static, CollisionResponsePolicy.CollideRaiseCollisionEvents, false);

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);


            ConvexCollider* convCollider = (ConvexCollider*)World.EntityManager.GetComponentData<PhysicsCollider>(bodyA).ColliderPtr;
            Assert.IsFalse(PhysicsUtilities.IsCollidable(convCollider->Material));

            convCollider = (ConvexCollider*)World.EntityManager.GetComponentData<PhysicsCollider>(bodyB).ColliderPtr;
            Assert.IsFalse(PhysicsUtilities.IsCollidable(convCollider->Material));

            convCollider = (ConvexCollider*)World.EntityManager.GetComponentData<PhysicsCollider>(bodyC).ColliderPtr;
            Assert.IsTrue(PhysicsUtilities.IsCollidable(convCollider->Material));

            convCollider = (ConvexCollider*)World.EntityManager.GetComponentData<PhysicsCollider>(bodyD).ColliderPtr;
            Assert.IsTrue(PhysicsUtilities.IsCollidable(convCollider->Material));
        }

        [Test]
        public unsafe void SetCollisionResponse()
        {
            NativeList<DistanceHit> tmpHits = new NativeList<DistanceHit>(100, Allocator.Temp);

            Entity bodyA = CharacterTestUtils.CreateSphereBody(World, math.up(), quaternion.identity, 1f, BodyMotionType.Static, CollisionResponsePolicy.None, false);

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            tmpHits.Clear();
            physicsWorldSingleton.OverlapSphere(default, 100f, ref tmpHits, CollisionFilter.Default, QueryInteraction.Default);
            Assert.AreEqual(1, tmpHits.Length);

            Collider* collPtr = (Collider*)physicsWorldSingleton.Bodies[tmpHits[0].RigidBodyIndex].Collider.GetUnsafePtr();

            PhysicsUtilities.SetCollisionResponse(physicsWorldSingleton.Bodies[tmpHits[0].RigidBodyIndex], CollisionResponsePolicy.RaiseTriggerEvents);
            Assert.IsTrue(collPtr->GetCollisionResponse() == CollisionResponsePolicy.RaiseTriggerEvents);

            PhysicsUtilities.SetCollisionResponse(physicsWorldSingleton.Bodies[tmpHits[0].RigidBodyIndex], tmpHits[0].ColliderKey, CollisionResponsePolicy.Collide);
            Assert.IsTrue(collPtr->GetCollisionResponse(tmpHits[0].ColliderKey) == CollisionResponsePolicy.Collide);

            tmpHits.Dispose();
        }

        [Test]
        public void SolveCollisionImpulses()
        {
            // B is left, A is right
            PhysicsVelocity velA = new PhysicsVelocity { Linear = -math.right() * 10f };
            PhysicsVelocity velB = new PhysicsVelocity { Linear = math.right() * 10f };
            PhysicsMass massA = PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 1f);
            PhysicsMass massB = PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 1f);
            RigidTransform transformA = new RigidTransform { pos = math.right(), rot = quaternion.identity };
            RigidTransform transformB = new RigidTransform { pos = -math.right(), rot = quaternion.identity };
            float3 collisionPoint = new float3(0f);
            float3 normalBToA = new float3(1f, 0f, 0f);

            PhysicsUtilities.SolveCollisionImpulses(velA, velB, massA, massB, transformA, transformB, collisionPoint, normalBToA, out float3 impulseOnA, out float3 impulseOnB);

            // If A and B have same mass and equal opposite speeds, the impulse on A of mass 1f would cancel out its velocity
            Assert.IsTrue(impulseOnA.IsRoughlyEqual(-impulseOnB));
            Assert.IsTrue(impulseOnA.IsRoughlyEqual(math.right() * 10f));

            massA = PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 1f);
            massB = PhysicsMass.CreateDynamic(MassProperties.UnitSphere, 2f);

            PhysicsUtilities.SolveCollisionImpulses(velA, velB, massA, massB, transformA, transformB, collisionPoint, normalBToA, out impulseOnA, out impulseOnB);

            // If A has mass 1f and B has mass 2f, and both have equal opposite speeds...
            // ...the total mass is 3f, and the total collision velocity is 20f...
            // ...B's velocity changes by 1/3 of this, and A's velocity changes by 2/3 of this...
            // ...so A's velocity must change by (2/3) * 20f...
            // ...and since A has a mass of 1f, its impulse is equal to its velocity change.
            Assert.IsTrue(impulseOnA.IsRoughlyEqual(-impulseOnB));
            Assert.IsTrue(impulseOnA.IsRoughlyEqual(math.right() * ((2f / 3f) * 20f)));
        }

        [Test]
        public void GetKinematicCharacterPhysicsMass()
        {
            KinematicCharacterProperties propsA = new KinematicCharacterProperties
            {
                Mass = 2f,
                SimulateDynamicBody = true,
            };
            KinematicCharacterProperties propsB = new KinematicCharacterProperties
            {
                Mass = 3f,
                SimulateDynamicBody = false,
            };
            StoredKinematicCharacterData storedPropsA = new StoredKinematicCharacterData
            {
                Mass = 4f,
                SimulateDynamicBody = true,
            };
            StoredKinematicCharacterData storedPropsB = new StoredKinematicCharacterData
            {
                Mass = 5f,
                SimulateDynamicBody = false,
            };

            PhysicsMass massPropsA = PhysicsUtilities.GetKinematicCharacterPhysicsMass(propsA);
            PhysicsMass massPropsB = PhysicsUtilities.GetKinematicCharacterPhysicsMass(propsB);
            PhysicsMass massStoredPropsA = PhysicsUtilities.GetKinematicCharacterPhysicsMass(storedPropsA);
            PhysicsMass massStoredPropsB = PhysicsUtilities.GetKinematicCharacterPhysicsMass(storedPropsB);

            Assert.IsTrue(massPropsA.InverseMass.IsRoughlyEqual(1f / 2f));
            Assert.IsTrue(massPropsB.InverseMass.IsRoughlyEqual(0f));
            Assert.IsTrue(massStoredPropsA.InverseMass.IsRoughlyEqual(1f / 4f));
            Assert.IsTrue(massStoredPropsB.InverseMass.IsRoughlyEqual(0f));
            Assert.IsTrue(massPropsA.InverseInertia.IsRoughlyEqual(float3.zero));
            Assert.IsTrue(massPropsB.InverseInertia.IsRoughlyEqual(float3.zero));
            Assert.IsTrue(massStoredPropsA.InverseInertia.IsRoughlyEqual(float3.zero));
            Assert.IsTrue(massStoredPropsB.InverseInertia.IsRoughlyEqual(float3.zero));
        }
    }
}
