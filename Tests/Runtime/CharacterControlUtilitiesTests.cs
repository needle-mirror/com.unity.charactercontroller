using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Unity.CharacterController;
using Unity.Mathematics;

namespace Unity.CharacterController.RuntimeTests
{
    class CharacterControlUtilitiesTests : BaseCharacterTestsFixture
    {
        [Test]
        public void GetSlopeAngleTowardsDirection()
        {
            float3 moveDir = math.forward();
            float3 slopeNormal = math.normalizesafe(new float3(0f, 1f, -1f));
            float3 groundingUp = math.up();

            float result = CharacterControlUtilities.GetSlopeAngleTowardsDirection(true, moveDir, slopeNormal, groundingUp);
            Assert.IsTrue(result.IsRoughlyEqual(45f));

            result = CharacterControlUtilities.GetSlopeAngleTowardsDirection(false, moveDir, slopeNormal, groundingUp);
            Assert.IsTrue(result.IsRoughlyEqual(math.radians(45f)));
        }

        [Test]
        public void StandardGroundMove_Interpolated()
        {
            float3 velocity = math.right();
            float3 targetVelocity = math.right() * 2f;
            float sharpness = 10f;
            float deltaTime = 1f;
            float3 groundingUp = math.up();
            float3 groundedHitNormal = math.up();

            float interpolant = MathUtilities.GetSharpnessInterpolant(sharpness, deltaTime);

            CharacterControlUtilities.StandardGroundMove_Interpolated(ref velocity, targetVelocity, sharpness, deltaTime, groundingUp, groundedHitNormal);
            Assert.IsTrue(velocity.IsRoughlyEqual(math.lerp(velocity, targetVelocity, interpolant)));
        }

        [Test]
        public void StandardGroundMove_Accelerated()
        {
            float3 velocity = math.right();
            float3 acceleration = math.right() * 2f;
            float maxSpeed = 10f;
            float deltaTime = 1f;
            float3 groundingUp = math.up();
            float3 groundedHitNormal = math.up();

            CharacterControlUtilities.StandardGroundMove_Accelerated(ref velocity, acceleration, maxSpeed, deltaTime, groundingUp, groundedHitNormal, false);
            Assert.IsTrue(velocity.IsRoughlyEqual(math.right() * 3f));
        }

        [Test]
        public void StandardAirMove()
        {
            float3 velocity = math.right();
            float3 acceleration = math.right() * 2f;
            float maxSpeed = 10f;
            float deltaTime = 1f;
            float3 movementPlaneUp = math.up();

            CharacterControlUtilities.StandardAirMove(ref velocity, acceleration, maxSpeed, movementPlaneUp, deltaTime, false);
            Assert.IsTrue(velocity.IsRoughlyEqual(math.right() * 3f));
        }

        [Test]
        public void InterpolateVelocityTowardsTarget()
        {
            float3 velocity = math.right();
            float3 targetVelocity = math.right() * 2f;
            float sharpness = 10f;
            float deltaTime = 1f;

            float interpolant = MathUtilities.GetSharpnessInterpolant(sharpness, deltaTime);

            CharacterControlUtilities.InterpolateVelocityTowardsTarget(ref velocity, targetVelocity, deltaTime, sharpness);
            Assert.IsTrue(velocity.IsRoughlyEqual(math.lerp(velocity, targetVelocity, interpolant)));
        }

        [Test]
        public void AccelerateVelocity()
        {
            float3 velocity = math.right();
            float3 acceleration = math.right() * 2f;
            float deltaTime = 1f;

            CharacterControlUtilities.AccelerateVelocity(ref velocity, acceleration, deltaTime);
            Assert.IsTrue(velocity.IsRoughlyEqual(math.right() * 3f));
        }

        [Test]
        public void ClampAdditiveVelocityToMaxSpeedOnPlane()
        {
            float3 additiveVelocity = math.right() + math.up();
            float3 originalVelocity = math.right();
            float maxSpeed = 1.5f;
            float3 movementPlaneUp = math.up();

            CharacterControlUtilities.ClampAdditiveVelocityToMaxSpeedOnPlane(ref additiveVelocity, originalVelocity, maxSpeed, movementPlaneUp, true);
            Assert.IsTrue(additiveVelocity.IsRoughlyEqual((math.right() * 0.5f) + math.up()));
        }

        [Test]
        public void StandardJump()
        {
            KinematicCharacterBody characterBody = KinematicCharacterBody.GetDefault();
            characterBody.RelativeVelocity = -math.up() * 5f;
            float3 jumpVelocity = math.up() * 10f;

            CharacterControlUtilities.StandardJump(ref characterBody, jumpVelocity, true, math.up());
            Assert.IsFalse(characterBody.IsGrounded);
            Assert.IsTrue(characterBody.RelativeVelocity.IsRoughlyEqual(math.up() * 10f));

            characterBody.IsGrounded = true;
            characterBody.RelativeVelocity = -math.up() * 5f;
            CharacterControlUtilities.StandardJump(ref characterBody, jumpVelocity, true, math.up());
            Assert.IsFalse(characterBody.IsGrounded);
            Assert.IsTrue(characterBody.RelativeVelocity.IsRoughlyEqual(math.up() * 10f));
        }

        [Test]
        public void ApplyDragToVelocity()
        {
            float3 velocity = math.right();
            CharacterControlUtilities.ApplyDragToVelocity(ref velocity, 1f, 1f);
            Assert.IsTrue(velocity.x.IsRoughlyEqual(0.5f));
        }

        [Test]
        public void GetLinearVelocityForMovePosition()
        {
            float3 result = CharacterControlUtilities.GetLinearVelocityForMovePosition(1f, math.right() * 10f);
            Assert.IsTrue(result.x.IsRoughlyEqual(10f));
        }

        [Test]
        public void SlerpRotationTowardsDirection()
        {
            quaternion a = quaternion.identity;
            float3 dir = math.normalizesafe(new float3(1f, 1f, 1f));
            CharacterControlUtilities.SlerpRotationTowardsDirection(ref a, 1f, dir, float.MaxValue);
            Assert.IsTrue(math.mul(a, math.forward()).IsRoughlyEqual(dir));
        }

        [Test]
        public void SlerpRotationTowardsDirectionAroundUp()
        {
            quaternion a = quaternion.identity;
            float3 dir = math.normalizesafe(new float3(1f, 1f, 1f));
            CharacterControlUtilities.SlerpRotationTowardsDirectionAroundUp(ref a, 1f, dir, math.up(), float.MaxValue);
            Assert.IsTrue(math.mul(a, math.forward()).IsRoughlyEqual(math.normalizesafe(MathUtilities.ProjectOnPlane(dir, math.up()))));
        }

        [Test]
        public void SlerpCharacterUpTowardsDirection()
        {
            quaternion a = quaternion.identity;
            float3 dir = math.normalizesafe(new float3(1f, 1f, 1f));
            CharacterControlUtilities.SlerpCharacterUpTowardsDirection(ref a, 1f, dir, float.MaxValue);
            Assert.IsTrue(math.mul(a, math.up()).IsRoughlyEqual(dir));
        }
    }
}