
# Step handling

You can control step handling through the [`StepHandling`](xref:Unity.CharacterController.BasicStepAndSlopeHandlingParameters.StepHandling), [`MaxStepHeight`](xref:Unity.CharacterController.BasicStepAndSlopeHandlingParameters.MaxStepHeight), and [`ExtraStepChecksDistance`](xref:Unity.CharacterController.BasicStepAndSlopeHandlingParameters.ExtraStepChecksDistance) fields of the character component.


## How step handling works internally

In your character aspect's `OnMovementHit`, `CharacterAspect.Default_OnMovementHit` is called. This internally calls `CheckForSteppingUpHit`, which tries to detect steps in the character movement direction. If a step was detected, the character's translation will be moved up to match the detected step height. The character will then cast its collider again in the movement direction, and will most likely not detect an obstruction since it has moved upwards high enough in the previous step to be positioned higher than the step obstruction. It will finally move forward for the remainder of the distance it was supposed to move this frame.

The following image shows what `CheckForSteppingUpHit` does. It casts the character shape up, forward, and then down to detect a valid step. If it detects a valid hit,  it moves the character upwards at the height of the step:

![](images/howto_steppingup.jpg)

A character also has to detect if [it's grounded](movement-grounding.md) on a step, even if it's not moving. This is so the character can stay still on the edge of a step where the slope angle would make it think it is ungrounded. The character processor's `IsGroundedOnHit` calls `CharacterAspect.Default_IsGroundedOnHit` by default, which internally calls `IsGroundedOnSteps`. This uses various raycasts to perform the necessary checks for valid ground checking when a character stands on the corner of a step.

You can use the `ExtraStepChecksDistance` parameter to determine the forward and backward distance of additional raycasts. It can be useful to ground the character on steps that aren't perfectly vertical. In the following image, the orange lines represent the `ExtraStepChecksDistance`, and the red lines are the raycasts performed at that distance to try to detect valid ground:

![](images/howto_stepgrounding.jpg)

Because step handling is difficult to implement, you can also consider placing invisible ramp colliders in your game as an alternative.