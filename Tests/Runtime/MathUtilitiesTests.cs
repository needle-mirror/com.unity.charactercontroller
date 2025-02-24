using UnityEngine;
using UnityEditor;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Unity.CharacterController;
using Unity.Mathematics;

namespace Unity.CharacterController.RuntimeTests
{
    class MathUtilitiesTests
    {
        [Test]
        public void FromToRotation()
        {
            quaternion a = quaternion.Euler(math.radians(33f), math.radians(33f), math.radians(33f));
            quaternion b = quaternion.identity;

            quaternion result = MathUtilities.FromToRotation(b, a);
            Assert.IsTrue(result.IsRoughlyEqual(a));

            result = MathUtilities.FromToRotation(result, b);
            Assert.IsTrue(result.IsRoughlyEqual(math.inverse(a)));
        }

        [Test]
        public void AngleRadians()
        {
            float3 a = math.up();
            float3 b = math.forward();

            float result = MathUtilities.AngleRadians(a, b);
            Assert.IsTrue(result.IsRoughlyEqual(math.radians(90f)));
        }

        [Test]
        public void AngleRadiansToDotRatio()
        {
            float radians = math.radians(45f);
            float result = MathUtilities.AngleRadiansToDotRatio(radians);
            Assert.IsTrue(result.IsRoughlyEqual(math.dot(math.up(), math.normalizesafe(new float3(1f, 1f, 0f)))));
        }

        [Test]
        public void DotRatioToAngleRadians()
        {
            float a = math.dot(math.up(), math.normalizesafe(new float3(1f, 1f, 0f)));
            float result = MathUtilities.DotRatioToAngleRadians(a);
            Assert.IsTrue(result.IsRoughlyEqual(math.radians(45f)));
        }

        [Test]
        public void ProjectOnPlane()
        {
            float3 projectedVector = MathUtilities.ProjectOnPlane(new float3(1f, 1f, 1f), math.up());
            Assert.IsTrue(projectedVector.IsRoughlyEqual(new float3(1f, 0f, 1f)));
        }

        [Test]
        public void ReverseProjectOnVector()
        {
            float3 deprojectedVector = MathUtilities.ReverseProjectOnVector(new float3(1f, 1f, 0f), math.up(), 10f);
            Assert.IsTrue(deprojectedVector.IsRoughlyEqual(new float3(0f, 2f, 0f)));
        }

        [Test]
        public void ClampToMaxLength()
        {
            float3 a = math.forward() * 10f;
            float3 result = MathUtilities.ClampToMaxLength(a, 5.5f);
            Assert.IsTrue(result.IsRoughlyEqual(new float3(0f, 0f, 5.5f)));
        }

        [Test]
        public void GetDirFromRotation()
        {
            quaternion a = quaternion.Euler(45f, 45f, 45f);
            Assert.IsTrue(MathUtilities.GetUpFromRotation(a).IsRoughlyEqual(math.mul(quaternion.Euler(45f, 45f, 45f), math.up())));
            Assert.IsTrue(MathUtilities.GetRightFromRotation(a).IsRoughlyEqual(math.mul(quaternion.Euler(45f, 45f, 45f), math.right())));
            Assert.IsTrue(MathUtilities.GetForwardFromRotation(a).IsRoughlyEqual(math.mul(quaternion.Euler(45f, 45f, 45f), math.forward())));
        }

        [Test]
        public void GetSharpnessInterpolant()
        {
            Assert.IsTrue(MathUtilities.GetSharpnessInterpolant(10000f, 10000f).IsRoughlyEqual(1f));
        }

        [Test]
        public void ReorientVectorOnPlaneAlongDirection()
        {
            float3 planeNormal = math.normalizesafe(new float3(1f, 1f, -1f));
            float3 result = MathUtilities.ReorientVectorOnPlaneAlongDirection(math.forward(), planeNormal, math.up());
            Assert.IsTrue(result.x.IsRoughlyEqual(0f));
            Assert.IsTrue(result.y > 0f);
            Assert.IsTrue(math.length(result).IsRoughlyEqual(1f));
        }

        [Test]
        public void CreateRotationWithUpPriority()
        {
            quaternion result = MathUtilities.CreateRotationWithUpPriority(math.right(), new float3(1f, 1f, 1f));
            Assert.IsTrue(math.mul(result, math.up()).IsRoughlyEqual(math.right()));
            Assert.IsTrue(math.mul(result, math.forward()).IsRoughlyEqual(math.forward()));
        }

        [Test]
        public void GetAxisSystemFromForward()
        {
            quaternion a = quaternion.Euler(45f, 45f, 45f);
            float3 fwdA = math.mul(a, math.forward());

            MathUtilities.GetAxisSystemFromForward(fwdA, out float3 rightA, out float3 upA);

            Assert.IsTrue(math.dot(fwdA, rightA).IsRoughlyEqual(0f));
            Assert.IsTrue(math.dot(fwdA, upA).IsRoughlyEqual(0f));
            Assert.IsTrue(math.dot(upA, rightA).IsRoughlyEqual(0f));
        }

        [Test]
        public void CalculatePointDisplacement()
        {
            float3 point = float3.zero;
            RigidTransform fromT = new RigidTransform(quaternion.identity, math.forward() * 10f);
            RigidTransform toT = new RigidTransform(quaternion.Euler(0f, math.radians(90f), 0f), math.up() * 10f);

            float3 result = MathUtilities.CalculatePointDisplacement(point, fromT, toT);

            // The end point should be a local (0,0,-10) translation to the "to" transform.
            // Since it rotates 90 degrees, that point is now (-10,0,0)
            // Since it goes up 10, that point is now (-10,10,0)
            // The translation from (0,0,0) to (-10,10,0) is (-10,10,0)
            Assert.IsTrue(result.IsRoughlyEqual(new float3(-10f, 10f, 0f)));
        }

        [Test]
        public void CalculatePointDisplacementFromVelocity()
        {
            float dt = 1f;
            RigidTransform t = new RigidTransform(quaternion.identity, math.forward() * 10f);
            float3 vel = new float3(0f, 10f, -10f);
            float3 angVel = new float3(0f, math.radians(90f), 0f);
            float3 pt = float3.zero;

            float3 result = MathUtilities.CalculatePointDisplacementFromVelocity(dt, t, vel, angVel, pt);

            Assert.IsTrue(result.IsRoughlyEqual(new float3(-10f, 10f, 0f)));
        }

        [Test]
        public void SetRotationAroundPoint()
        {
            quaternion rot = quaternion.identity;
            float3 pos = float3.zero;
            MathUtilities.SetRotationAroundPoint(ref rot, ref pos, math.normalizesafe(new float3(1f, 1f, 0f)), quaternion.Euler(0f, 0f, math.radians(90f)));

            Assert.IsTrue(rot.IsRoughlyEqual(quaternion.Euler(0f, 0f, math.radians(90f))));
            Assert.IsTrue(pos.IsRoughlyEqual(math.normalizesafe(new float3(1f, 1f, 0f)) + math.normalizesafe(new float3(1f, -1f, 0f))));
        }
    }
}
