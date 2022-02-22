# Grounding

For the character controller, being "grounded" means that the bottom of the character collider is colliding with something else, and that the character considers itself stable on that surface. For example:
* If the character is standing on a flat surface, it will consider itself "grounded" on that surface.
* If the character is in the air and not standing on anything, it won't consider itself "grounded".
* If the bottom of the character collider is colliding with a very steep slope, the characer won't consider itself "grounded", because the steepness of the slope is too high for it to consider itself stable on that surface.

The [`IsGrounded`](xref:Unity.CharacterController.KinematicCharacterBody.IsGrounded) field indicates whether a character is currently grounded. To customize the rules that determine whether or not a character considers itself stable on a ground hit, you can modify the [`IsGroundedOnHit`](xref:Unity.CharacterController.IKinematicCharacterProcessor`1.IsGroundedOnHit*) callback in your character aspect. By default, this mainly evaluates the slope angle of the ground hit, and returns false if that angle is too high.

The [`GroundHit`](xref:Unity.CharacterController.KinematicCharacterBody.GroundHit) field represents the hit that was evaluated for grounding. It is possible for a [`GroundHit`](xref:Unity.CharacterController.KinematicCharacterBody.GroundHit) to be detected, but for [`IsGrounded`](xref:Unity.CharacterController.KinematicCharacterBody.IsGrounded) to be false. This happens if a collision with the ground was detected, but [`IsGroundedOnHit`](xref:Unity.CharacterController.IKinematicCharacterProcessor`1.IsGroundedOnHit*) returned "false" because the character does not considering itself stable on that hit.

Grounding is detected and evaluated during two different steps of the character update:

* During `KinematicCharacterAspect.Update_Grounding`, a downward shape cast tries to detect ground hits.
* During `KinematicCharacterAspect.Update_MovementAndDecollisions`, forward movement shape casts evaluate new hits in order to determine if they should replace the previously-detected ground hit.

For more information see the documentation on [Character controller aspects](concepts-character-update.md).


## Effects of being grounded

A grounded character will be stand firmly in place when standing on a slope that is mild enough for the character to consider itself stable on it. If the character's [`KinematicCharacterProperties`](xref:Unity.CharacterController.AuthoringKinematicCharacterProperties.MaxGroundedSlopeAngle) was lowered enough to make the character not consider itself stable on that slope angle, the character would start sliding down the slope.

A grounded character will also "snap" to the ground, if [`SnapToGround`](xref:Unity.CharacterController.KinematicCharacterProperties.SnapToGround) is set to true. This means the character will adjust its position to always stick to the ground surface, instead of launching off into the air when encountering downward slope changes.

Finally, a grounded character will always re-orient its velocity along the slope direction, rather than projecting it. This means that the character will not lose any velocity when it moves from a flat surface to a sloped surface.