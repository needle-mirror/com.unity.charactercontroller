
# Moving platforms

By default, a character can stand on any entity that has collisions, and a [`TrackedTransform`](xref:Unity.CharacterController.TrackedTransform) component. This component can optionally be added during baking using the [`TrackedTransformAuthoring`](xref:Unity.CharacterController.TrackedTransformAuthoring) component. You can move that entity in any way, for example, through transform, through velocity, at a variable update rate, or at a fixed update rate.

>[!NOTE]
>For accurate physics interactions between the platform and the characters, it's best practice to use a rigidbody for the moving platform entity, and to move it exclusively with `PhysicsVelocity`. Moving platforms that are moved with the transform directly will not be able to properly solve push impulses between the character and the platform.  Moveover, the moving platform rigidbody should be interpolated if the character position is also interpolated.

The code for a simple moving platform is provided here (component, authoring and system):

```cs
/// Component code
using System;
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

[Serializable]
public struct MovingPlatform : IComponentData
{
    public float3 TranslationAxis;
    public float TranslationAmplitude;
    public float TranslationSpeed;
    public float3 RotationAxis;
    public float RotationSpeed;

    [HideInInspector]
    public bool IsInitialized;
    [HideInInspector]
    public float3 OriginalPosition;
    [HideInInspector]
    public quaternion OriginalRotation;
}
```

```cs
/// Authoring code
using Unity.Entities;
using UnityEngine;

public class MovingPlatformAuthoring : MonoBehaviour
{
    public MovingPlatform MovingPlatform;

    public class Baker : Baker<MovingPlatformAuthoring>
    {
        public override void Bake(MovingPlatformAuthoring authoring)
        {
            AddComponent(authoring.MovingPlatform);
        }
    }
}
```

```cs
/// System code
using Unity.Entities;
using Unity.Mathematics;
using Unity.Physics;
using Unity.Physics.Systems;
using Unity.Transforms;

[UpdateInGroup(typeof(BeforePhysicsSystemGroup))]
public partial class MovingPlatformSystem : SystemBase
{
    protected override void OnUpdate()
    {
        float deltaTime = SystemAPI.Time.DeltaTime;
        float invDeltaTime = 1f / deltaTime;
        float time = (float)World.Time.ElapsedTime;

        foreach(var (movingPlatform, physicsVelocity, physicsMass, localTransform, entity) in SystemAPI.Query<RefRW<MovingPlatform>, RefRW<PhysicsVelocity>, PhysicsMass, LocalTransform>().WithEntityAccess())
        {
            if(!movingPlatform.ValueRW.IsInitialized)
            {
                // Remember initial pos/rot, because our calculations depend on them
                movingPlatform.ValueRW.OriginalPosition = localTransform.Position;
                movingPlatform.ValueRW.OriginalRotation = localTransform.Rotation;
                movingPlatform.ValueRW.IsInitialized = true;
            }

            float3 targetPos = movingPlatform.ValueRW.OriginalPosition + (math.normalizesafe(movingPlatform.ValueRW.TranslationAxis) * math.sin(time * movingPlatform.ValueRW.TranslationSpeed) * movingPlatform.ValueRW.TranslationAmplitude);
            quaternion rotationFromMovement = quaternion.Euler(math.normalizesafe(movingPlatform.ValueRW.RotationAxis) * movingPlatform.ValueRW.RotationSpeed * time);
            quaternion targetRot = math.mul(rotationFromMovement, movingPlatform.ValueRW.OriginalRotation);

            // Move with velocity
            physicsVelocity.ValueRW = PhysicsVelocity.CalculateVelocityToTarget(in physicsMass, localTransform.Position, localTransform.Rotation, new RigidTransform(targetRot, targetPos), invDeltaTime);
        }
    }
}
```

The system uses a maths function to make kinematic physics bodies move with a given translation and rotation speed at a fixed timestep. It calculates a `targetPos` and a `targetRot`, and then calls `PhysicsVelocity.CalculateVelocityToTarget` to calculate and apply a physics velocity brings the rigidbody to that target position and rotation over the next fixed update.

To use this example moving platform in a scene:

1. Add the above `MovingPlatform`, `MovingPlatformAuthoring` and `MovingPlatformSystem` scripts to your project.
1. Add a new box with a `PhysicsShape` to your subscene. Call this object `MovingPlatform`.
1. Make sure that the `MovingPlatform` object also has a `PhysicsBody`. Set the `MotionType` to `Kinematic`, and its `Smoothing` to `Interpolation`.
1. Add a `TrackedTransformAuthoring` component to the `MovingPlatform`.
1. Set some parameters in the `MovingPlatform` component

At this point, you can press Play and jump onto the moving platform. Your character should be able to stand on it.
