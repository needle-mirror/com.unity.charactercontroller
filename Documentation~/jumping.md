
# Jumping

Because of the way that [character grounding](movement-grounding.md) works, there are some extra mechanics to consider when you want to make a character jump. For example, if you add an upwards velocity to the character to attempt to make it jump, the character will remain snapped to the ground by the ground snapping mechanism.


## Implement jumping

You can also use the pre-made [`CharacterControlUtilities.StandardJump`](xref:Unity.CharacterController.CharacterControlUtilities.StandardJump*) method for a quick complete jump implementation. Alternatively, if you want to write your own jump implementation, you must remember to not only set an upwards velocity for the character, but also to set the [`IsGrounded`](xref:Unity.CharacterController.KinematicCharacterBody.IsGrounded) field to `false` to manually unground the character. This makes sure that the ground snapping mechanism doesn't snap your character back to ground.


### Double jumping

To implement double-jumping, you must keep track of how many jumps your character has done in air, and then reset that value whenever the character is grounded. When the character is in the air, you can allow it to jump as it would jump on ground, but only if the amount of in air jumps made is lower than the maximum number of in air jumps allowed.


### Jump higher when holding jump input

Sometimes you want your character to jump higher when holding the jump input. To do this, add a variable for the jump "base force", the "hold acceleration", and the "hold duration". When a user first presses the jump input, you can make the character jump regularly by assigning an upwards velocity equivalent to the jumping "base force" variable. On consecutive frames where the user is still holding the jump input, and the time elapsed is lower than the jump "hold duration" variable, you can add additional upward acceleration equivalent to the "hold acceleration" variable to the character's velocity.


### Jumping grace times

If your character is in air and you want to make sure pressing jump slightly before the character lands on the ground still results in a jump, you can do so by always remembering the time at which the jump input was pressed, even when the character is in-air. When your character becomes grounded, you can check the last time the jump input was pressed, and then jump regularly if that time was within a certain grace duration.

If your character recently became ungrounded and you want to make sure pressing jump still results in a jump even though the character isn't grounded anymore, you can remember the last time the character was grounded. Then, when in air and pressing jump, you can check the last time the character was grounded, and then jump regularly if that time was within a certain grace duration.


### Landing and leaving ground

To detect if your character has just landed or left ground, you can use `KinematicCharacterBody.HasBecomeGrounded` or `KinematicCharacterBody.HasBecomeUngrounded`.
