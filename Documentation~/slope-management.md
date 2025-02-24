
# Slope management


## Prevent grounding based on slope angle changes

There are times when you might want your character to ignore the ground when it's moving off of a ledge towards [no valid ground](movement-grounding.md), or when going over a certain downward change in slope angle. This is so that your character can launch off the slope instead of sticking to it and making the movement feel unnatural.

The [`BasicStepAndSlopeHandlingParameters`](xref:Unity.CharacterController.BasicStepAndSlopeHandlingParameters) struct includes several parameters for this:
* `PreventGroundingWhenMovingTowardsNoGrounding`: Prevents a character from sticking to the ground when moving off of a ledge towards invalid ground.
* `HasMaxDownwardSlopeChangeAngle`: Prevents a character from sticking to the ground when moving over a downward slope change.
* `MaxDownwardSlopeChangeAngle`: The maximum angle in degrees where your character can stick to the ground when moving over a downward slope change.

Your character aspect's `Update_PreventGroundingFromFutureSlopeChange` uses these values to detect how the slope angle changes ahead of the character, and whether the character is heading towards an invalid ground side of a ledge. The following image shows how this function works:

![](images/howto_slopechangedetection.jpg)

* **A**: The height of the initial forward raycast, that the `verticalOffset` parameter of the method determines.
* **B**: A forward raycast of length equal to the `deltaTimeIntoFuture` parameter of the method multiplied by the length of the character's velocity.
* **C**: The downward raycasts of length equal to the `downDetectionDepth` parameter of the method added to the `verticalOffset`.
* **D**: The distance that the second downward raycast is from the first, equal to the `secondaryNoGroundingCheckDistance` parameter of the method.
* **E**: A backward raycast of length equal to **B** + **D**. This ray goes all the way back to the starting point horizontally.

1. The **B** raycast attempts to detect any obstruction forward. If it hits an obstruction, Unity calculates the positive (upward) `futureSlopeChangeAnglesRadians` between the grounding and the hit slope.
1. If the raycast doesn't hit an obstruction, Unity performs the first **C** raycast. If this raycast hits an obstruction, Unity calculates the negative (downward) `futureSlopeChangeAnglesRadians` between the grounding and the slope.
1. If the character isn't grounded on that hit, or the raycast didn't hit anything, Unity determines that the character is moving towards no grounding and sets the `isMovingTowardsNoGrounding` out parameter to true.
1. If `isMovingTowardsNoGrounding` is true at this point, Unity performs a second **C** raycast further ahead, to make sure the first **C** raycast didn't go through a small crack in the ground. If the second **C** raycast finds a grounded hit, Unity resets `isMovingTowardsNoGrounding` to false.
1. Finally, Unity performs a backwards **E** raycast that performs a final attempt at detecting the slope angles.


## Scale velocity on slopes

To make your character move slower uphill, use the [`CharacterControlUtilities.GetSlopeAngleTowardsDirection`](xref:Unity.CharacterController.CharacterControlUtilities.GetSlopeAngleTowardsDirection*) method. This calculates the signed slope angle in a given movement direction. The resulting angle is positive if the slope goes up, and negative if the slope goes down.

In your character aspect's `HandleVelocityControl`, you can apply a multiplier to your desired character velocity based on that signed slope angle.
