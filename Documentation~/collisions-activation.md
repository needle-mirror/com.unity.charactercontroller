
# Collision activation

If you want to deactivate the collision handling of a character, there are several options to disable:

* Set the character `PhysicsCollider`'s collision response to `None`. This will prevent other rigidbodies from detecting collisions with your character, but it will not prevent your character from detecting hits with other rigidbodies.
* Set `KinematicCharacterProperties.EvaluateGrounding` to `false`. This will prevent the character from detecting the ground and projecting its velocity onto it.
* Set `KinematicCharacterProperties.DetectMovementCollisions` to `false`. This will prevent the character from detecting and solving hits resulting from its velocity-based movement.
* Set `KinematicCharacterProperties.DecollideFromOverlaps` to `false`. This will prevent the character from de-colliding itself from detected hits.

## Ignore collisions

To ignore collisions procedurally with code use the [`CanCollideWithHit`](xref:Unity.CharacterController.IKinematicCharacterProcessor`1.CanCollideWithHit*) processor callback.