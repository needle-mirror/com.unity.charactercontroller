# Dynamic rigidbody interactions

To let your character push and be pushed by dynamic rigidbodies, enable the [`SimulateDynamicBody`](xref:Unity.CharacterController.KinematicCharacterProperties.SimulateDynamicBody) option in your character's authoring component.

When `SimulateDynamicBody` is enabled, the character applies force on itself and other rigidbodies to imitate the behavior of a true dynamic rigidbody. This uses the [`Mass`](xref:Unity.CharacterController.KinematicCharacterProperties.Mass) property of the character's authoring component to simulate collision mass ratios.

## SynchronizeCollisionWorld

When dealing with a character that can push or be pushed by other rigidbodies (kinematic or dynamic), you might want to add a `PhysicsStep` component to an entity in your scene, and set `SynchronizeCollisionWorld` to true. This ensures that the `CollisionWorld` that the character update uses for physics queries is updated after the physics systems make the rigidbodies move. The result is that enabling `SynchronizeCollisionWorld`removes some slight visual lag between the character and the object it pushes.