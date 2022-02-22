# Dynamic rigidbody interactions

To let your character push and be pushed by dynamic rigidbodies, enable the [`SimulateDynamicBody`](xref:Unity.CharacterController.KinematicCharacterProperties.SimulateDynamicBody) option in your character's authoring component. 

When `SimulateDynamicBody` is enabled, the character applies force on itself and other rigidbodies to imitate the behavior of a true dynamic rigidbody. This uses the [`Mass`](xref:Unity.CharacterController.KinematicCharacterProperties.Mass) property of the character's authoring component to simulate collision mass ratios.

Because the character is a kinematic rigidbody with collisions, if a dynamic rigidbody tries to push the character, the character stops the dynamic rigidbody as soon as the collision happens. To make sure that collisions with the character don't stop the dynamic rigidbodies, you can  set the character's PhysicsShape's collision response to either `None` or `RaiseTriggerEvents`. But you must remember this when performing any raycasts in your game that expect to hit characters.

## SynchronizeCollisionWorld

When dealing with a character that can push or be pushed by other rigidbodies (kinematic or dynamic), you might want to add a `PhysicsStep` component to an entity in your scene, and set `SynchronizeCollisionWorld` to true. This ensures that the `CollisionWorld` that the character update uses for physics queries is updated after the physics systems make the rigidbodies move. The result is that enabling `SynchronizeCollisionWorld`removes some slight visual lag between the character and the object it pushes.