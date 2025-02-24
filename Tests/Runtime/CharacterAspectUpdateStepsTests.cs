using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Core;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Authoring;
using Unity.Physics.Systems;
using Unity.Transforms;
using UnityEditor;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using UnityEngine.TestTools;

namespace Unity.CharacterController.RuntimeTests
{
    public class CharacterAspectUpdateStepsTests : BaseCharacterTestsFixture
    {
        [Test]
        public void UpdateInitialize()
        {
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(0f, 0f, 0f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);

            ref KinematicCharacterBody characterBody = ref characterAspect.CharacterAspect.CharacterBody.ValueRW;
            DynamicBuffer<KinematicCharacterHit> characterHitsBuffer = characterAspect.CharacterAspect.CharacterHitsBuffer;
            DynamicBuffer<KinematicVelocityProjectionHit> velocityProjectionHitsBuffer = characterAspect.CharacterAspect.VelocityProjectionHits;
            DynamicBuffer<KinematicCharacterDeferredImpulse> deferredImpulsesBuffer = characterAspect.CharacterAspect.DeferredImpulsesBuffer;
            DynamicBuffer<StatefulKinematicCharacterHit> statefulCharacterHitsBuffer = characterAspect.CharacterAspect.StatefulHitsBuffer;

            // Data before update
            float3 initialRelativeVelocity = math.forward() * 5f;
            float3 initialParentLocalAnchorPoint = math.up() * 5f;
            float3 initialParentVelocity = math.right();
            bool prevGrounded = true;
            Entity initialParentEntity = new Entity { Index = 456, Version = 2 };
            characterBody.RelativeVelocity = initialRelativeVelocity;
            characterBody.ParentEntity = initialParentEntity;
            characterBody.ParentLocalAnchorPoint = initialParentLocalAnchorPoint;
            characterBody.WasGroundedBeforeCharacterUpdate = false;
            characterBody.ParentVelocity = initialParentVelocity;
            characterBody.RotationFromParent = quaternion.Euler(10f, 20f, 30f);
            characterBody.PreviousParentEntity = new Entity { Index = 123, Version = 2 };
            characterBody.IsGrounded = prevGrounded;
            characterBody.GroundHit = new BasicHit { Entity = new Entity { Index = 234, Version = 2 } };
            characterBody.LastPhysicsUpdateDeltaTime = 1f;
            characterHitsBuffer.Add(default);
            velocityProjectionHitsBuffer.Add(default);
            deferredImpulsesBuffer.Add(default);
            statefulCharacterHitsBuffer.Add(default);

            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);

            // Preserved values
            Assert.AreEqual(initialRelativeVelocity, characterBody.RelativeVelocity);
            Assert.AreEqual(initialParentEntity, characterBody.ParentEntity);
            Assert.AreEqual(initialParentVelocity, characterBody.ParentVelocity);
            Assert.AreEqual(initialParentLocalAnchorPoint, characterBody.ParentLocalAnchorPoint);
            Assert.AreEqual(1, statefulCharacterHitsBuffer.Length);

            // Changed values
            Assert.AreEqual(prevGrounded, characterBody.WasGroundedBeforeCharacterUpdate);
            Assert.AreEqual(initialParentEntity, characterBody.PreviousParentEntity);
            Assert.AreEqual(World.Time.DeltaTime, characterBody.LastPhysicsUpdateDeltaTime);

            // Reseted values
            Assert.AreEqual(quaternion.identity, characterBody.RotationFromParent);
            Assert.AreEqual(false, characterBody.IsGrounded);
            Assert.AreEqual(new BasicHit(), characterBody.GroundHit);
            Assert.AreEqual(0, characterHitsBuffer.Length);
            Assert.AreEqual(0, velocityProjectionHitsBuffer.Length);
            Assert.AreEqual(0, deferredImpulsesBuffer.Length);
        }

        [Test]
        public void UpdateParentMovement()
        {
            Entity movingPlatformEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Kinematic, CollisionResponsePolicy.Collide, true);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(50f, 0f, 50f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            LocalTransform initialPlatformTransform = World.EntityManager.GetComponentData<LocalTransform>(movingPlatformEntity);
            LocalTransform initialCharacterTransform = World.EntityManager.GetComponentData<LocalTransform>(characterEntity);
            RigidTransform characterTransformRelativeToPlatform = math.mul(math.inverse(initialPlatformTransform.ToRigidTransform()), initialCharacterTransform.ToRigidTransform());
            PhysicsVelocity platformVelocity = new PhysicsVelocity
            {
                Linear = math.right() * 5f,
                Angular = new float3(math.radians(10f), math.radians(5f), math.radians(7f)),
            };
            World.EntityManager.SetComponentData(movingPlatformEntity, platformVelocity);
            KinematicCharacterBody characterBody = World.EntityManager.GetComponentData<KinematicCharacterBody>(characterEntity);
            characterBody.ParentEntity = movingPlatformEntity;
            World.EntityManager.SetComponentData(characterEntity, characterBody);

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);
            CharacterTestUtils.StepPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.UpdateTrackedTransforms(World);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_ParentMovement(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position, characterAspect.CharacterAspect.CharacterBody.ValueRW.WasGroundedBeforeCharacterUpdate);

            characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRO;
            RigidTransform newCharacterTransform = characterAspect.CharacterAspect.LocalTransform.ValueRW.ToRigidTransform();
            RigidTransform newPlatformTransform = World.EntityManager.GetComponentData<LocalTransform>(movingPlatformEntity).ToRigidTransform();
            RigidTransform expectedCharacterTransform = math.mul(newPlatformTransform, characterTransformRelativeToPlatform);

            Assert.IsTrue(newCharacterTransform.IsRoughlyEqual(expectedCharacterTransform));
            Assert.AreEqual(movingPlatformEntity, characterBody.ParentEntity);
            Assert.AreEqual(movingPlatformEntity, characterBody.PreviousParentEntity);
            Assert.IsTrue(characterBody.ParentVelocity.IsRoughlyEqual((newCharacterTransform.pos - initialCharacterTransform.Position) / World.Time.DeltaTime));
            Assert.IsTrue(characterBody.RotationFromParent.IsRoughlyEqual(math.mul(math.inverse(initialCharacterTransform.Rotation), newCharacterTransform.rot)));
        }

        [Test]
        public void UpdateGrounding_FlatGround()
        {
            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(0f, 0f, 0f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRO;
            DynamicBuffer<KinematicCharacterHit> characterHitsBuffer = characterAspect.CharacterAspect.CharacterHitsBuffer;
            DynamicBuffer<KinematicVelocityProjectionHit> velocityProjectionHitsBuffer = characterAspect.CharacterAspect.VelocityProjectionHits;

            Assert.AreEqual(floorEntity, characterBody.GroundHit.Entity);
            Assert.IsTrue(characterBody.GroundHit.Normal.IsRoughlyEqual(math.up()));
            Assert.IsTrue(characterBody.IsGrounded);
            Assert.AreEqual(1, characterHitsBuffer.Length);
            Assert.AreEqual(characterBody.GroundHit.Entity, characterHitsBuffer[0].Entity);
            Assert.AreEqual(1, velocityProjectionHitsBuffer.Length);
            Assert.AreEqual(characterBody.GroundHit.Entity, velocityProjectionHitsBuffer[0].Entity);
        }

        [Test]
        public void UpdateGrounding_NoGround()
        {
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(0f, 0f, 0f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRO;
            DynamicBuffer<KinematicCharacterHit> characterHitsBuffer = characterAspect.CharacterAspect.CharacterHitsBuffer;
            DynamicBuffer<KinematicVelocityProjectionHit> velocityProjectionHitsBuffer = characterAspect.CharacterAspect.VelocityProjectionHits;

            Assert.AreEqual(Entity.Null, characterBody.GroundHit.Entity);
            Assert.IsFalse(characterBody.IsGrounded);
            Assert.AreEqual(0, characterHitsBuffer.Length);
            Assert.AreEqual(0, velocityProjectionHitsBuffer.Length);
        }

        [Test]
        public void UpdateGrounding_SmallAngleGround()
        {
            quaternion floorRotation = quaternion.Euler(math.radians(30f), 0f, 0f);
            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), floorRotation, new float3(100f, 1f, 100f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(0f, 50f, 0f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);
            CharacterTestUtils.MoveCharacterToClosestCollideableInDirection(World, characterEntity, physicsWorldSingleton, -math.up());

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRO;
            DynamicBuffer<KinematicCharacterHit> characterHitsBuffer = characterAspect.CharacterAspect.CharacterHitsBuffer;
            DynamicBuffer<KinematicVelocityProjectionHit> velocityProjectionHitsBuffer = characterAspect.CharacterAspect.VelocityProjectionHits;

            Assert.AreEqual(floorEntity, characterBody.GroundHit.Entity);
            Assert.IsTrue(characterBody.GroundHit.Normal.IsRoughlyEqual(math.mul(floorRotation, math.up())));
            Assert.IsTrue(characterBody.IsGrounded);
            Assert.AreEqual(1, characterHitsBuffer.Length);
            Assert.AreEqual(characterBody.GroundHit.Entity, characterHitsBuffer[0].Entity);
            Assert.AreEqual(1, velocityProjectionHitsBuffer.Length);
            Assert.AreEqual(characterBody.GroundHit.Entity, velocityProjectionHitsBuffer[0].Entity);
        }

        [Test]
        public void UpdateGrounding_SteepAngleGround()
        {
            quaternion floorRotation = quaternion.Euler(math.radians(70f), 0f, 0f);
            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), floorRotation, new float3(100f, 1f, 100f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(0f, 50f, 0f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);
            CharacterTestUtils.MoveCharacterToClosestCollideableInDirection(World, characterEntity, physicsWorldSingleton, -math.up());

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRO;
            DynamicBuffer<KinematicCharacterHit> characterHitsBuffer = characterAspect.CharacterAspect.CharacterHitsBuffer;
            DynamicBuffer<KinematicVelocityProjectionHit> velocityProjectionHitsBuffer = characterAspect.CharacterAspect.VelocityProjectionHits;

            Assert.AreEqual(floorEntity, characterBody.GroundHit.Entity);
            Assert.IsTrue(characterBody.GroundHit.Normal.IsRoughlyEqual(math.mul(floorRotation, math.up())));
            Assert.IsFalse(characterBody.IsGrounded);
            Assert.AreEqual(0, characterHitsBuffer.Length);
            Assert.AreEqual(0, velocityProjectionHitsBuffer.Length);
        }

        [Test]
        public void UpdateGrounding_SlopePeak()
        {
            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, 0f, 0f), quaternion.Euler(math.radians(45f), 0f, 0f), new float3(5f, 10f, 10f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(0f, 10f, 0f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);
            CharacterTestUtils.MoveCharacterToClosestCollideableInDirection(World, characterEntity, physicsWorldSingleton, -math.up());

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRO;
            DynamicBuffer<KinematicCharacterHit> characterHitsBuffer = characterAspect.CharacterAspect.CharacterHitsBuffer;
            DynamicBuffer<KinematicVelocityProjectionHit> velocityProjectionHitsBuffer = characterAspect.CharacterAspect.VelocityProjectionHits;

            Assert.AreEqual(floorEntity, characterBody.GroundHit.Entity);
            Assert.IsTrue(characterBody.GroundHit.Normal.IsRoughlyEqual(math.up()));
            Assert.IsTrue(characterBody.IsGrounded);
            Assert.AreEqual(1, characterHitsBuffer.Length);
            Assert.AreEqual(characterBody.GroundHit.Entity, characterHitsBuffer[0].Entity);
            Assert.AreEqual(1, velocityProjectionHitsBuffer.Length);
            Assert.AreEqual(characterBody.GroundHit.Entity, velocityProjectionHitsBuffer[0].Entity);
        }

        [Test]
        public void UpdatePreventGroundingFromFutureSlopeChange_FlatGround()
        {
            BasicStepAndSlopeHandlingParameters stepAndSlopeHandlingParameters = BasicStepAndSlopeHandlingParameters.GetDefault();
            stepAndSlopeHandlingParameters.PreventGroundingWhenMovingTowardsNoGrounding = true;
            stepAndSlopeHandlingParameters.HasMaxDownwardSlopeChangeAngle = true;
            stepAndSlopeHandlingParameters.MaxDownwardSlopeChangeAngle = math.radians(45f);

            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(0f, 0f, 0f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);
            CharacterTestUtils.SetCharacterVelocity(World, characterEntity, math.forward() * 10f);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            ref KinematicCharacterBody characterBody = ref characterAspect.CharacterAspect.CharacterBody.ValueRW;
            Assert.IsTrue(characterBody.IsGrounded);

            characterAspect.CharacterAspect.Update_PreventGroundingFromFutureSlopeChange(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, stepAndSlopeHandlingParameters);

            Assert.IsTrue(characterBody.IsGrounded);
        }

        [Test]
        public void UpdatePreventGroundingFromFutureSlopeChange_Cliff()
        {
            BasicStepAndSlopeHandlingParameters stepAndSlopeHandlingParameters = BasicStepAndSlopeHandlingParameters.GetDefault();
            stepAndSlopeHandlingParameters.PreventGroundingWhenMovingTowardsNoGrounding = true;
            stepAndSlopeHandlingParameters.HasMaxDownwardSlopeChangeAngle = false;

            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(10f, 1f, 10f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(0f, 0f, 4.95f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);
            CharacterTestUtils.SetCharacterVelocity(World, characterEntity, math.forward() * 10f);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            ref KinematicCharacterBody characterBody = ref characterAspect.CharacterAspect.CharacterBody.ValueRW;
            Assert.IsTrue(characterBody.IsGrounded);

            characterAspect.CharacterAspect.Update_PreventGroundingFromFutureSlopeChange(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, stepAndSlopeHandlingParameters);

            Assert.IsFalse(characterBody.IsGrounded);
        }

        [Test]
        public void UpdatePreventGroundingFromFutureSlopeChange_TriangleSlope()
        {
            BasicStepAndSlopeHandlingParameters stepAndSlopeHandlingParameters = BasicStepAndSlopeHandlingParameters.GetDefault();
            stepAndSlopeHandlingParameters.PreventGroundingWhenMovingTowardsNoGrounding = true;
            stepAndSlopeHandlingParameters.HasMaxDownwardSlopeChangeAngle = true;
            stepAndSlopeHandlingParameters.MaxDownwardSlopeChangeAngle = 100f;

            float3 characterStartPosition = new float3(0f, 10f, -0.05f);
            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, 0f, 0f), quaternion.Euler(math.radians(45f), 0f, 0f), new float3(5f, 10f, 10f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, characterStartPosition, quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);
            CharacterTestUtils.MoveCharacterToClosestCollideableInDirection(World, characterEntity, physicsWorldSingleton, -math.up());

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            ref KinematicCharacterBody characterBody = ref characterAspect.CharacterAspect.CharacterBody.ValueRW;
            Assert.IsTrue(characterBody.IsGrounded);

            CharacterTestUtils.SetCharacterVelocity(World, characterEntity, math.forward() * 10f);

            characterAspect.CharacterAspect.Update_PreventGroundingFromFutureSlopeChange(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, stepAndSlopeHandlingParameters);

            Assert.IsTrue(characterBody.IsGrounded);

            CharacterTestUtils.BuildPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.SetCharacterPosition(World, characterEntity, characterStartPosition);
            CharacterTestUtils.MoveCharacterToClosestCollideableInDirection(World, characterEntity, physicsWorldSingleton, -math.up());

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out characterAspect, out baseContext, out context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            characterBody = ref characterAspect.CharacterAspect.CharacterBody.ValueRW;
            Assert.IsTrue(characterBody.IsGrounded);

            CharacterTestUtils.SetCharacterVelocity(World, characterEntity, math.forward() * 10f);

            stepAndSlopeHandlingParameters.MaxDownwardSlopeChangeAngle = 45f;
            characterAspect.CharacterAspect.Update_PreventGroundingFromFutureSlopeChange(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, stepAndSlopeHandlingParameters);

            Assert.IsFalse(characterBody.IsGrounded);
        }

        [Test]
        public void UpdateGroundPushing_Dynamic()
        {
            float pushForceMultiplier = 20f;

            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(10f, 1f, 10f), BodyMotionType.Dynamic, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(5f, 0f, 5f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_GroundPushing(in characterAspect, ref context, ref baseContext, characterAspect.CharacterComponent.ValueRO.Gravity, pushForceMultiplier);

            Assert.AreEqual(1, characterAspect.CharacterAspect.DeferredImpulsesBuffer.Length);
            Assert.IsTrue(World.EntityManager.GetComponentData<PhysicsVelocity>(floorEntity).Linear.IsRoughlyEqual(float3.zero));
            Assert.IsTrue(World.EntityManager.GetComponentData<PhysicsVelocity>(floorEntity).Angular.IsRoughlyEqual(float3.zero));

            CharacterTestUtils.UpdateDeferredImpulses(World);

            Assert.IsTrue(!World.EntityManager.GetComponentData<PhysicsVelocity>(floorEntity).Linear.IsRoughlyEqual(float3.zero));
            Assert.IsTrue(!World.EntityManager.GetComponentData<PhysicsVelocity>(floorEntity).Angular.IsRoughlyEqual(float3.zero));
        }

        [Test]
        public void UpdateGroundPushing_Kinematic()
        {
            float pushForceMultiplier = 20f;

            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(10f, 1f, 10f), BodyMotionType.Kinematic, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(5f, 0f, 5f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_GroundPushing(in characterAspect, ref context, ref baseContext, characterAspect.CharacterComponent.ValueRO.Gravity, pushForceMultiplier);

            Assert.AreEqual(0, characterAspect.CharacterAspect.DeferredImpulsesBuffer.Length);
            Assert.IsTrue(World.EntityManager.GetComponentData<PhysicsVelocity>(floorEntity).Linear.IsRoughlyEqual(float3.zero));
            Assert.IsTrue(World.EntityManager.GetComponentData<PhysicsVelocity>(floorEntity).Angular.IsRoughlyEqual(float3.zero));

            CharacterTestUtils.UpdateDeferredImpulses(World);

            Assert.IsTrue(World.EntityManager.GetComponentData<PhysicsVelocity>(floorEntity).Linear.IsRoughlyEqual(float3.zero));
            Assert.IsTrue(World.EntityManager.GetComponentData<PhysicsVelocity>(floorEntity).Angular.IsRoughlyEqual(float3.zero));
        }

        [Test]
        public void UpdateMovementAndDecollisions_FlatGround()
        {
            float3 initialCharacterPosition = new float3(0f, 0f, 0f);
            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, initialCharacterPosition, quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            float3 initialCharacterVelocity = math.forward() * 10f;
            CharacterTestUtils.SetCharacterVelocity(World, characterEntity, initialCharacterVelocity);

            characterAspect.CharacterAspect.Update_MovementAndDecollisions(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;
            LocalTransform characterTransform = characterAspect.CharacterAspect.LocalTransform.ValueRW;
            DynamicBuffer<KinematicCharacterHit> characterHitsBuffer = characterAspect.CharacterAspect.CharacterHitsBuffer;

            Assert.IsTrue(characterBody.RelativeVelocity.IsRoughlyEqual(initialCharacterVelocity));
            Assert.IsTrue(math.distance(initialCharacterPosition, characterTransform.Position) > (World.Time.DeltaTime * math.length(initialCharacterVelocity) * 0.9f));
            Assert.AreEqual(1, characterHitsBuffer.Length);
        }

        [Test]
        public void UpdateMovementAndDecollisions_FlatGroundWithObstacle()
        {
            float3 initialCharacterPosition = new float3(0f, 0f, 0f);
            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity obstacleEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, 0f, 5f), quaternion.Euler(math.radians(90f), 0f, 0f), new float3(100f, 1f, 100f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, initialCharacterPosition, quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            float3 initialCharacterVelocity = math.forward() * 10000f;
            CharacterTestUtils.SetCharacterVelocity(World, characterEntity, initialCharacterVelocity);

            characterAspect.CharacterAspect.Update_MovementAndDecollisions(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;
            LocalTransform characterTransform = characterAspect.CharacterAspect.LocalTransform.ValueRW;
            DynamicBuffer<KinematicCharacterHit> characterHitsBuffer = characterAspect.CharacterAspect.CharacterHitsBuffer;

            Assert.IsTrue(characterBody.RelativeVelocity.IsRoughlyEqual(float3.zero));
            Assert.IsTrue(math.distance(initialCharacterPosition, characterTransform.Position) > 1f);
            Assert.AreEqual(2, characterHitsBuffer.Length);
            Assert.AreEqual(obstacleEntity, characterHitsBuffer[1].Entity);
        }

        [Test]
        public void UpdateMovementAndDecollisions_SlopedGround()
        {
            float3 initialCharacterPosition = new float3(0f, 10f, 0f);
            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.Euler(math.radians(-20f), 0f, 0f), new float3(100f, 1f, 100f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, initialCharacterPosition, quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);
            CharacterTestUtils.MoveCharacterToClosestCollideableInDirection(World, characterEntity, physicsWorldSingleton, -math.up());
            initialCharacterPosition = World.EntityManager.GetComponentData<LocalTransform>(characterEntity).Position;

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            float3 initialCharacterVelocity = math.forward() * 10f;
            CharacterTestUtils.SetCharacterVelocity(World, characterEntity, initialCharacterVelocity);

            characterAspect.CharacterAspect.Update_MovementAndDecollisions(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;
            LocalTransform characterTransform = characterAspect.CharacterAspect.LocalTransform.ValueRW;
            DynamicBuffer<KinematicCharacterHit> characterHitsBuffer = characterAspect.CharacterAspect.CharacterHitsBuffer;

            Assert.IsTrue(math.distance(initialCharacterPosition, characterTransform.Position) > (World.Time.DeltaTime * math.length(characterBody.RelativeVelocity) * 0.9f));
            Assert.IsTrue(characterBody.RelativeVelocity.y > 0.1f);
            Assert.AreEqual(1, characterHitsBuffer.Length);
        }

        [Test]
        public void UpdateMovementAndDecollisions_StepUp()
        {
            BasicStepAndSlopeHandlingParameters stepAndSlopeHandlingParameters = BasicStepAndSlopeHandlingParameters.GetDefault();
            stepAndSlopeHandlingParameters.StepHandling = true;
            stepAndSlopeHandlingParameters.MaxStepHeight = 0.6f;
            stepAndSlopeHandlingParameters.CharacterWidthForStepGroundingCheck = 1f;

            float3 initialCharacterPosition = new float3(0f, 0f, 0f);
            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity stepEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, 0f, 1.01f), quaternion.identity, new float3(1f, 1f, 1f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, initialCharacterPosition, quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            TestCharacterComponent characterComponent = World.EntityManager.GetComponentData<TestCharacterComponent>(characterEntity);
            characterComponent.StepAndSlopeHandling = stepAndSlopeHandlingParameters;
            World.EntityManager.SetComponentData(characterEntity, characterComponent);

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            float3 initialCharacterVelocity = math.forward() * 10f;
            CharacterTestUtils.SetCharacterVelocity(World, characterEntity, initialCharacterVelocity);

            characterAspect.CharacterAspect.Update_MovementAndDecollisions(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;
            LocalTransform characterTransform = characterAspect.CharacterAspect.LocalTransform.ValueRW;
            DynamicBuffer<KinematicCharacterHit> characterHitsBuffer = characterAspect.CharacterAspect.CharacterHitsBuffer;

            Assert.IsTrue(characterBody.RelativeVelocity.IsRoughlyEqual(initialCharacterVelocity));
            Assert.IsTrue(math.distance(initialCharacterPosition, characterTransform.Position) > (World.Time.DeltaTime * math.length(initialCharacterVelocity) * 0.9f));
            Assert.IsTrue(characterTransform.Position.y > 0.4f);
            Assert.AreEqual(2, characterHitsBuffer.Length);
            Assert.AreEqual(stepEntity, characterHitsBuffer[1].Entity);
        }

        [Test]
        public void UpdateMovementAndDecollisions_UpdateGrounding_StepDown()
        {
            BasicStepAndSlopeHandlingParameters stepAndSlopeHandlingParameters = BasicStepAndSlopeHandlingParameters.GetDefault();
            stepAndSlopeHandlingParameters.StepHandling = true;
            stepAndSlopeHandlingParameters.MaxStepHeight = 0.6f;
            stepAndSlopeHandlingParameters.CharacterWidthForStepGroundingCheck = 1f;

            float3 initialCharacterPosition = new float3(0f, 0.5f, 0.5f);
            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity stepEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, 0f, 0f), quaternion.identity, new float3(1f, 1f, 1f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, initialCharacterPosition, quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            TestCharacterComponent characterComponent = World.EntityManager.GetComponentData<TestCharacterComponent>(characterEntity);
            characterComponent.StepAndSlopeHandling = stepAndSlopeHandlingParameters;
            World.EntityManager.SetComponentData(characterEntity, characterComponent);
            KinematicCharacterProperties characterProperties = World.EntityManager.GetComponentData<KinematicCharacterProperties>(characterEntity);
            characterProperties.SnapToGround = true;
            characterProperties.GroundSnappingDistance = 0.6f;
            World.EntityManager.SetComponentData(characterEntity, characterProperties);

            // Frame 1 --------------------------------------------------------------------------------------------------------------------------------
            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;
            LocalTransform characterTransform = characterAspect.CharacterAspect.LocalTransform.ValueRW;

            Assert.IsTrue(characterTransform.Position.y > 0.4f);
            Assert.IsTrue(characterBody.IsGrounded);
            Assert.AreEqual(stepEntity, characterBody.GroundHit.Entity);

            float desiredDisplacement = 0.6f;
            float3 initialCharacterVelocity = math.forward() * (desiredDisplacement / World.Time.DeltaTime);
            CharacterTestUtils.SetCharacterVelocity(World, characterEntity, initialCharacterVelocity);

            characterAspect.CharacterAspect.Update_MovementAndDecollisions(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            // Frame 2 --------------------------------------------------------------------------------------------------------------------------------
            CharacterTestUtils.BuildPhysicsWorld(World, out physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out characterAspect, out baseContext, out context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;
            characterTransform = characterAspect.CharacterAspect.LocalTransform.ValueRW;
            DynamicBuffer<KinematicCharacterHit> characterHitsBuffer = characterAspect.CharacterAspect.CharacterHitsBuffer;

            Assert.IsTrue(characterBody.RelativeVelocity.IsRoughlyEqual(initialCharacterVelocity));
            Assert.IsTrue(math.distance(initialCharacterPosition, characterTransform.Position) > (World.Time.DeltaTime * math.length(initialCharacterVelocity) * 0.9f));
            Assert.IsTrue(characterTransform.Position.y < 0.02f);
            Assert.IsTrue(characterBody.IsGrounded);
            Assert.AreEqual(floorEntity, characterBody.GroundHit.Entity);
            Assert.AreEqual(1, characterHitsBuffer.Length);
        }

        [Test]
        public void UpdateMovementAndDecollisions_PushDynamic()
        {
            float3 initialCharacterPosition = new float3(-0.25f, 0.01f, -100f);
            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity dynamicBoxEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, 1f, 0f), quaternion.Euler(0f, math.radians(45f), 0f), new float3(2f, 2f, 2f), BodyMotionType.Dynamic, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, initialCharacterPosition, quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);
            CharacterTestUtils.MoveCharacterToClosestCollideableInDirection(World, characterEntity, physicsWorldSingleton, math.forward());
            initialCharacterPosition = World.EntityManager.GetComponentData<LocalTransform>(characterEntity).Position;

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            float3 initialCharacterVelocity = math.forward() * 10f;
            CharacterTestUtils.SetCharacterVelocity(World, characterEntity, initialCharacterVelocity);

            characterAspect.CharacterAspect.Update_MovementAndDecollisions(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);

            CharacterTestUtils.UpdateDeferredImpulses(World);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out characterAspect, out baseContext, out context);
            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;
            DynamicBuffer<KinematicCharacterHit> characterHitsBuffer = characterAspect.CharacterAspect.CharacterHitsBuffer;
            PhysicsVelocity boxVelocity = World.EntityManager.GetComponentData<PhysicsVelocity>(dynamicBoxEntity);

            Assert.IsTrue(characterBody.RelativeVelocity.x < -0.1f);
            Assert.IsTrue(boxVelocity.Linear.x > 0.1f);
            Assert.IsTrue(!boxVelocity.Angular.IsRoughlyEqual(float3.zero));
            Assert.AreEqual(2, characterHitsBuffer.Length);
            Assert.AreEqual(dynamicBoxEntity, characterHitsBuffer[1].Entity);
        }

        [Test]
        public void UpdateMovingPlatformDetection_NoTrackedTransform()
        {
            Entity movingPlatformEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Kinematic, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(10f, 0f, 10f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;

            Assert.AreEqual(Entity.Null, characterBody.ParentEntity);
            Assert.AreEqual(float3.zero, characterBody.ParentLocalAnchorPoint);
        }

        [Test]
        public void UpdateMovingPlatformDetection_Kinematic()
        {
            Entity movingPlatformEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Kinematic, CollisionResponsePolicy.Collide, true);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(10f, 0f, 10f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;

            Assert.AreEqual(movingPlatformEntity, characterBody.ParentEntity);
            Assert.IsTrue(characterBody.ParentLocalAnchorPoint.IsRoughlyEqual(new float3(10f, 0.5f, 10f)));
        }

        [Test]
        public void UpdateMovingPlatformDetection_Dynamic()
        {
            Entity movingPlatformEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Dynamic, CollisionResponsePolicy.Collide, true);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(10f, 0f, 10f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;

            Assert.AreEqual(movingPlatformEntity, characterBody.ParentEntity);
            Assert.IsTrue(characterBody.ParentLocalAnchorPoint.IsRoughlyEqual(new float3(10f, 0.5f, 10f)));
        }

        [Test]
        public void UpdateParentMomentum_Linear()
        {
            Entity movingPlatformEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Kinematic, CollisionResponsePolicy.Collide, true);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(0f, 0.01f, 0f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            PhysicsVelocity movingPlatformVelocity = World.EntityManager.GetComponentData<PhysicsVelocity>(movingPlatformEntity);
            movingPlatformVelocity.Linear = math.forward() * 10f;
            World.EntityManager.SetComponentData(movingPlatformEntity, movingPlatformVelocity);
            float3 initialCharacterVelocity = math.forward() * 15f;
            CharacterTestUtils.SetCharacterVelocity(World, characterEntity, initialCharacterVelocity);

            // Frame 1 --------------------------------------------------------------------------------------------------------------------------------
            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);
            CharacterTestUtils.StepPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.UpdateTrackedTransforms(World);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_ParentMovement(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position, characterAspect.CharacterAspect.CharacterBody.ValueRW.WasGroundedBeforeCharacterUpdate);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);
            characterAspect.CharacterAspect.Update_ParentMomentum(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;

            Assert.AreEqual(movingPlatformEntity, characterBody.ParentEntity);
            Assert.AreEqual(Entity.Null, characterBody.PreviousParentEntity);
            Assert.IsTrue(characterBody.RelativeVelocity.IsRoughlyEqual(initialCharacterVelocity - movingPlatformVelocity.Linear));
            Assert.IsTrue(characterBody.ParentVelocity.IsRoughlyEqual(movingPlatformVelocity.Linear));

            // Frame 2 --------------------------------------------------------------------------------------------------------------------------------
            CharacterTestUtils.BuildPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.StepPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.UpdateTrackedTransforms(World);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out characterAspect, out baseContext, out context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_ParentMovement(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position, characterAspect.CharacterAspect.CharacterBody.ValueRW.WasGroundedBeforeCharacterUpdate);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);
            characterAspect.CharacterAspect.Update_ParentMomentum(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);

            characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;

            Assert.AreEqual(movingPlatformEntity, characterBody.ParentEntity);
            Assert.AreEqual(movingPlatformEntity, characterBody.PreviousParentEntity);
            Assert.IsTrue(characterBody.RelativeVelocity.IsRoughlyEqual(initialCharacterVelocity - movingPlatformVelocity.Linear));
            Assert.IsTrue(characterBody.ParentVelocity.IsRoughlyEqual(movingPlatformVelocity.Linear));

            // Frame 3 --------------------------------------------------------------------------------------------------------------------------------
            World.EntityManager.DestroyEntity(movingPlatformEntity);
            CharacterTestUtils.BuildPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.StepPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.UpdateTrackedTransforms(World);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out characterAspect, out baseContext, out context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_ParentMovement(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position, characterAspect.CharacterAspect.CharacterBody.ValueRW.WasGroundedBeforeCharacterUpdate);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);
            characterAspect.CharacterAspect.Update_ParentMomentum(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);

            characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;

            Assert.AreEqual(Entity.Null, characterBody.ParentEntity);
            Assert.AreEqual(movingPlatformEntity, characterBody.PreviousParentEntity);
            Assert.IsTrue(characterBody.RelativeVelocity.IsRoughlyEqual(initialCharacterVelocity));
            Assert.IsTrue(characterBody.ParentVelocity.IsRoughlyEqual(float3.zero));

            // Frame 4 --------------------------------------------------------------------------------------------------------------------------------
            CharacterTestUtils.BuildPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.StepPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.UpdateTrackedTransforms(World);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out characterAspect, out baseContext, out context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_ParentMovement(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position, characterAspect.CharacterAspect.CharacterBody.ValueRW.WasGroundedBeforeCharacterUpdate);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);
            characterAspect.CharacterAspect.Update_ParentMomentum(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);

            characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;

            Assert.AreEqual(Entity.Null, characterBody.ParentEntity);
            Assert.AreEqual(Entity.Null, characterBody.PreviousParentEntity);
            Assert.IsTrue(characterBody.RelativeVelocity.IsRoughlyEqual(initialCharacterVelocity));
            Assert.IsTrue(characterBody.ParentVelocity.IsRoughlyEqual(float3.zero));
        }

        [Test]
        public void UpdateParentMomentum_Angular()
        {
            float3 initialCharacterPosition = new float3(10f, 0.01f, 10f);
            Entity movingPlatformEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Kinematic, CollisionResponsePolicy.Collide, true);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, initialCharacterPosition, quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            PhysicsVelocity movingPlatformVelocity = World.EntityManager.GetComponentData<PhysicsVelocity>(movingPlatformEntity);
            movingPlatformVelocity.Angular = math.up() * math.radians(300f);
            World.EntityManager.SetComponentData(movingPlatformEntity, movingPlatformVelocity);
            float3 initialCharacterVelocity = math.forward() * 15f;
            CharacterTestUtils.SetCharacterVelocity(World, characterEntity, initialCharacterVelocity);

            // Frame 1 --------------------------------------------------------------------------------------------------------------------------------
            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);
            CharacterTestUtils.StepPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.UpdateTrackedTransforms(World);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_ParentMovement(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position, characterAspect.CharacterAspect.CharacterBody.ValueRW.WasGroundedBeforeCharacterUpdate);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);
            characterAspect.CharacterAspect.Update_ParentMomentum(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);

            KinematicCharacterBody characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;

            float3 estimatedMovingPlatformVelocity = World.EntityManager.GetComponentData<TrackedTransform>(movingPlatformEntity).CalculatePointVelocity(initialCharacterPosition, World.Time.DeltaTime);

            Assert.AreEqual(movingPlatformEntity, characterBody.ParentEntity);
            Assert.AreEqual(Entity.Null, characterBody.PreviousParentEntity);
            Assert.IsTrue(characterBody.RelativeVelocity.IsRoughlyEqual(initialCharacterVelocity - estimatedMovingPlatformVelocity, 0.1f));
            Assert.IsTrue(characterBody.ParentVelocity.IsRoughlyEqual(estimatedMovingPlatformVelocity));

            float3 characterRelativeVelocityAfterParenting = characterBody.RelativeVelocity;
            RigidTransform characterRigidTransformAfterFrame1 = new RigidTransform(characterAspect.CharacterAspect.LocalTransform.ValueRO.Rotation, characterAspect.CharacterAspect.LocalTransform.ValueRO.Position);

            // Frame 2 --------------------------------------------------------------------------------------------------------------------------------
            CharacterTestUtils.BuildPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.StepPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.UpdateTrackedTransforms(World);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out characterAspect, out baseContext, out context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_ParentMovement(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position, characterAspect.CharacterAspect.CharacterBody.ValueRW.WasGroundedBeforeCharacterUpdate);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);
            characterAspect.CharacterAspect.Update_ParentMomentum(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);

            characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;

            TrackedTransform movingPlatformTrackedTransform = World.EntityManager.GetComponentData<TrackedTransform>(movingPlatformEntity);
            RigidTransform displacedCharacterTransform = math.mul(movingPlatformTrackedTransform.CurrentFixedRateTransform, math.mul(math.inverse(movingPlatformTrackedTransform.PreviousFixedRateTransform), characterRigidTransformAfterFrame1));
            estimatedMovingPlatformVelocity = (displacedCharacterTransform.pos - characterRigidTransformAfterFrame1.pos) / World.Time.DeltaTime;

            Assert.AreEqual(movingPlatformEntity, characterBody.ParentEntity);
            Assert.AreEqual(movingPlatformEntity, characterBody.PreviousParentEntity);
            Assert.IsTrue(characterBody.RelativeVelocity.IsRoughlyEqual(characterRelativeVelocityAfterParenting));
            Assert.IsTrue(characterBody.ParentVelocity.IsRoughlyEqual(estimatedMovingPlatformVelocity));

            // Frame 3 --------------------------------------------------------------------------------------------------------------------------------
            World.EntityManager.DestroyEntity(movingPlatformEntity);
            CharacterTestUtils.BuildPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.StepPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.UpdateTrackedTransforms(World);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out characterAspect, out baseContext, out context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_ParentMovement(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position, characterAspect.CharacterAspect.CharacterBody.ValueRW.WasGroundedBeforeCharacterUpdate);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);
            characterAspect.CharacterAspect.Update_ParentMomentum(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);

            characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;

            Assert.AreEqual(Entity.Null, characterBody.ParentEntity);
            Assert.AreEqual(movingPlatformEntity, characterBody.PreviousParentEntity);
            Assert.IsTrue(!characterBody.RelativeVelocity.IsRoughlyEqual(characterRelativeVelocityAfterParenting));
            Assert.IsTrue(characterBody.ParentVelocity.IsRoughlyEqual(float3.zero));

            // Frame 4 --------------------------------------------------------------------------------------------------------------------------------
            CharacterTestUtils.BuildPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.StepPhysicsWorld(World, out physicsWorldSingleton);
            CharacterTestUtils.UpdateTrackedTransforms(World);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out characterAspect, out baseContext, out context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_ParentMovement(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position, characterAspect.CharacterAspect.CharacterBody.ValueRW.WasGroundedBeforeCharacterUpdate);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_MovingPlatformDetection(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);
            characterAspect.CharacterAspect.Update_ParentMomentum(ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW);

            characterBody = characterAspect.CharacterAspect.CharacterBody.ValueRW;

            Assert.AreEqual(Entity.Null, characterBody.ParentEntity);
            Assert.AreEqual(Entity.Null, characterBody.PreviousParentEntity);
            Assert.IsTrue(!characterBody.RelativeVelocity.IsRoughlyEqual(characterRelativeVelocityAfterParenting));
            Assert.IsTrue(characterBody.ParentVelocity.IsRoughlyEqual(float3.zero));
        }

        [Test]
        public void UpdateProcessStatefulCharacterHits()
        {
            Entity floorEntity = CharacterTestUtils.CreateBoxBody(World, new float3(0f, -0.5f, 0f), quaternion.identity, new float3(100f, 1f, 100f), BodyMotionType.Static, CollisionResponsePolicy.Collide, false);
            Entity characterEntity = CharacterTestUtils.CreateCharacter(World, new float3(0f, 0f, 0f), quaternion.identity, 2f, 0.5f, AuthoringKinematicCharacterProperties.GetDefault());

            // Frame 1 --------------------------------------------------------------------------------------------------------------------------------
            CharacterTestUtils.BuildPhysicsWorld(World, out PhysicsWorldSingleton physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out TestCharacterAspect characterAspect, out KinematicCharacterUpdateContext baseContext, out TestCharacterUpdateContext context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_ProcessStatefulCharacterHits();

            DynamicBuffer<StatefulKinematicCharacterHit> statefulCharacterHitsBuffer = characterAspect.CharacterAspect.StatefulHitsBuffer;

            Assert.AreEqual(1, statefulCharacterHitsBuffer.Length);
            Assert.AreEqual(CharacterHitState.Enter, statefulCharacterHitsBuffer[0].State);
            Assert.AreEqual(floorEntity, statefulCharacterHitsBuffer[0].Hit.Entity);

            // Frame 2 --------------------------------------------------------------------------------------------------------------------------------
            CharacterTestUtils.BuildPhysicsWorld(World, out physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out characterAspect, out baseContext, out context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_ProcessStatefulCharacterHits();

            statefulCharacterHitsBuffer = characterAspect.CharacterAspect.StatefulHitsBuffer;

            Assert.AreEqual(1, statefulCharacterHitsBuffer.Length);
            Assert.AreEqual(CharacterHitState.Stay, statefulCharacterHitsBuffer[0].State);
            Assert.AreEqual(floorEntity, statefulCharacterHitsBuffer[0].Hit.Entity);

            // Frame 3 --------------------------------------------------------------------------------------------------------------------------------
            World.EntityManager.DestroyEntity(floorEntity);
            CharacterTestUtils.BuildPhysicsWorld(World, out physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out characterAspect, out baseContext, out context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_ProcessStatefulCharacterHits();

            statefulCharacterHitsBuffer = characterAspect.CharacterAspect.StatefulHitsBuffer;

            Assert.AreEqual(1, statefulCharacterHitsBuffer.Length);
            Assert.AreEqual(CharacterHitState.Exit, statefulCharacterHitsBuffer[0].State);
            Assert.AreEqual(floorEntity, statefulCharacterHitsBuffer[0].Hit.Entity);

            // Frame 4 --------------------------------------------------------------------------------------------------------------------------------
            CharacterTestUtils.BuildPhysicsWorld(World, out physicsWorldSingleton);

            CharacterTestUtils.GetCharacterAspectWithContexts(World, characterEntity, physicsWorldSingleton, out characterAspect, out baseContext, out context);
            characterAspect.CharacterAspect.Update_Initialize(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, World.Time.DeltaTime);
            characterAspect.CharacterAspect.Update_Grounding(in characterAspect, ref context, ref baseContext, ref characterAspect.CharacterAspect.CharacterBody.ValueRW, ref characterAspect.CharacterAspect.LocalTransform.ValueRW.Position);
            characterAspect.CharacterAspect.Update_ProcessStatefulCharacterHits();

            statefulCharacterHitsBuffer = characterAspect.CharacterAspect.StatefulHitsBuffer;

            Assert.AreEqual(0, statefulCharacterHitsBuffer.Length);
        }
    }
}