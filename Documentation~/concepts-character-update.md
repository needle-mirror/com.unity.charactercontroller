# Character update

The character controller update happens at two different points in the frame, and is executed by the combination of a character aspect ([`KinematicCharacterAspect`](xref:Unity.CharacterController.KinematicCharacterAspect)) and a character processor ([`IKinematicCharacterProcessor`](xref:Unity.CharacterController.IKinematicCharacterProcessor`1)).


## The character aspect

The [`KinematicCharacterAspect`](xref:Unity.CharacterController.KinematicCharacterAspect) holds all the data required for character updates, and contains methods for major character update steps and utilities.

These are the main steps for a typical character update, available in [`KinematicCharacterAspect`](xref:Unity.CharacterController.KinematicCharacterAspect):

|**Method**|**Description**|
|---|---|
|[`Update_Initialize`](xref:Unity.CharacterController.KinematicCharacterAspect.Update_Initialize*)| Clears and initializes core character data and buffers at the start of the update.|
|[`Update_ParentMovement`](xref:Unity.CharacterController.KinematicCharacterAspect.Update_ParentMovement*)| Moves the character based on its assigned [`ParentEntity`](xref:Unity.CharacterController.KinematicCharacterBody.ParentEntity), if any.|
|[`Update_Grounding`](xref:Unity.CharacterController.KinematicCharacterAspect.Update_Grounding*)| Detects character grounding.|
|[`Update_PreventGroundingFromFutureSlopeChange`](xref:Unity.CharacterController.KinematicCharacterAspect.Update_PreventGroundingFromFutureSlopeChange*)| Cancels a character's grounded status based on the definitions in the [`BasicStepAndSlopeHandlingParameters`](xref:Unity.CharacterController.BasicStepAndSlopeHandlingParameters) given to the method. For example, cancel grounding if the character is heading towards a ledge.|
|[`Update_GroundPushing`](xref:Unity.CharacterController.KinematicCharacterAspect.Update_Grounding*)| Applies a constant force to the current ground entity, if the entity is dynamic.|
|[`Update_MovementAndDecollisions`](xref:Unity.CharacterController.KinematicCharacterAspect.Update_MovementAndDecollisions*)| Moves the character with its velocity and solves collisions.|
|[`Update_MovingPlatformDetection`](xref:Unity.CharacterController.KinematicCharacterAspect.Update_MovingPlatformDetection*)| Detects valid moving platform entities, and assigns them as the character's `ParentEntity`. For more information, see the documentation on [Parenting](concepts-parenting.md).|
|[`Update_ParentMomentum`](xref:Unity.CharacterController.KinematicCharacterAspect.Update_ParentMomentum*)| Preserves the velocity momentum when a character is detached from a parent body.|
|[`Update_ProcessStatefulCharacterHits`](xref:Unity.CharacterController.KinematicCharacterAspect.Update_ProcessStatefulCharacterHits)| Fills the `StatefulKinematicCharacterHit` buffer on the character entity with character hits that have an `Enter`, `Exit`, or `Stay` state.|

The standard characters that come with the package already take care of calling these in the correct order and at the correct time, in order to solve character physics.

The `KinematicCharacterAspect` also contains various methods for querying the world with the character's shape and collision filtering. For example:

* [`CastColliderClosestCollisions`](xref:Unity.CharacterController.KinematicCharacterAspect.CastColliderClosestCollisions*)
* [`CastColliderAllCollisions`](xref:Unity.CharacterController.KinematicCharacterAspect.CastColliderAllCollisions*)
* [`RaycastClosestCollisions`](xref:Unity.CharacterController.KinematicCharacterAspect.RaycastClosestCollisions*)
* [`RaycastAllCollisions`](xref:Unity.CharacterController.KinematicCharacterAspect.RaycastAllCollisions*)
* [`CalculateDistanceClosestCollisions`](xref:Unity.CharacterController.KinematicCharacterAspect.CalculateDistanceClosestCollisions*)
* [`CalculateDistanceAllCollisions`](xref:Unity.CharacterController.KinematicCharacterAspect.CalculateDistanceAllCollisions*)


## The character processor

The character processor is a user-implemented aspect that implements the [`IKinematicCharacterProcessor`](xref:Unity.CharacterController.IKinematicCharacterProcessor) interface. It serves two main purposes:

* Provide a way to customize the logic executed by the update steps of the [`KinematicCharacterAspect`](xref:Unity.CharacterController.KinematicCharacterAspect).
* Provide a way for users to define additional components that can be accessed during the character updates.

In the standard characters, an initial version of this processor aspect is already created, and it is meant to be customized by users. Both the fixed character physics update and the character variable update use this processor aspect in order to gain access to all of the data and methods that they could require.

The character processor is able to customize the logic executed by the update steps of the [`KinematicCharacterAspect`](xref:Unity.CharacterController.KinematicCharacterAspect) by passing itself as a parameter to these functions, which in turn call functions on the character processor. In these "callbacks", users can modify the default implementation to suit their needs.
In summary, systems will schedule jobs iterating on the processor aspect implementing [`IKinematicCharacterProcessor`](xref:Unity.CharacterController.IKinematicCharacterProcessor), which will then call character update steps in the [`KinematicCharacterAspect`](xref:Unity.CharacterController.KinematicCharacterAspect), which will then call back to functions of [`IKinematicCharacterProcessor`](xref:Unity.CharacterController.IKinematicCharacterProcessor).

The following character processor methods can be used to customize character logic. For full descriptions of each of these methods see the [`IKinematicCharacterProcessor` API documentation](xref:Unity.CharacterController.IKinematicCharacterProcessor`1):

|**Method**|**Description**|
|---|---|
|`UpdateGroundingUp`| Updates the up direction that a character compares slope normals with. You must write this direction to `KinematicCharacterBody.GroundingUp`.|
|`CanCollideWithHit`| Checks if the character can collide with a hit. Returns true if the character can collide, and false otherwise.|
|`OnMovementHit`| Determines what happens when the character collider casts have detected a hit during the movement iterations. By default, this should call `KinematicCharacterAspect.Default_OnMovementHit`.|
|`IsGroundedOnHit`| Determines if the character is grounded on the hit or not. By default, it calla `KinematicCharacterAspect.Default_IsGroundedOnHit`, which checks the slope angle and velocity direction to determine the final result.|
|`OverrideDynamicHitMasses`| Modifies the mass ratios between the character and another dynamic body when they collide. This is only called for characters that have `KinematicCharacterProperties.SimulateDynamicBody` set to true. You can leave this method empty if you don't want to modify the mass ratio.|
|`ProjectVelocityOnHits`| Determines how the character velocity gets projected on hits, based on all hits so far this frame. By default, you should call `KinematicCharacterAspect.Default_ProjectVelocityOnHits` here. You shouldn't need to change this callback, unless you want to make your character bounce on certain surfaces, for example.|
