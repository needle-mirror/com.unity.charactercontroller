
# Grounding customization


## Prevent grounding on hits

You can use the `IsGroundedOnHit` callback of your character aspect to prevent grounding procedurally with code. For example, you could look at the presence of certain components on the hit entity, or look at the physics tags or categories of that object, and `return false;` in the function when you want the character to not be able to consider itself grounded on that hit.


## Constrain ground movement to the ground plane

You can set the [`ConstrainVelocityToGroundPlane`](xref:Unity.CharacterController.BasicStepAndSlopeHandlingParameters.ConstrainVelocityToGroundPlane) to true to make your character unable to "bump up into the air" when moving fast into slopes that are too steep to be grounded on:

![](images/howto_constrainvel_true.gif)

When set to false, your character won't attempt to constrain its velocity to the ground, and bumps up in the air:

![](images/howto_constrainvel_false.gif)