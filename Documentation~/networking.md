
# Networking

One of the most common approaches to networking a character controller involves:

* Client-side prediction on the client that owns the character
* Interpolation on clients that don't own the character

This section explains how to do this with the [Netcode package](https://docs.unity3d.com/Packages/com.unity.netcode@latest).

## Ghost component setup

Your Player and Character prefabs both need a `GhostAuthoringComponent`, with `Default Ghost Mode` set to `Owner Predicted` and `Has Owner` set to `true`

Your Player prefab's `GhostAuthoringComponent` also needs `Support Auto Command Target` and `Track Interpolation Delay` set to true

## What to synchronize on the character
The following fields on the character entity need to be synchronized over network, using `[GhostField]`:

* `LocalTransform.Position`
* `LocalTransform.Rotation` (depending on the game, you might want to synchronize only the Y euler angle instead of the full rotation, and you'd have to reconstruct the full rotation from that angle in a prediction system)
* `KinematicCharacterBody.IsGrounded`
* `KinematicCharacterBody.RelativeVelocity`

This constitutes all of the basic required character state data to synchronize over network. However, you must remember that any character feature you add may or may not require additional data to synchronize.

In order to synchronize fields of the `KinematicCharacterBody` component without modifying the sources of the package, you can use NetCode's [Ghost Variants](https://docs.unity3d.com/Packages/com.unity.netcode@latest/index.html?subfolder=/manual/ghost-snapshots.html#ghost-component-variants).

It's also worth noting that the `PhysicsVelocity` component on the character entity must **not** be synchronized (because its value is never used and it should always be zero, and therefore it would be wasteful to synchronize it). `PhysicsVelocity` is synchronized by default in NetCode, so you must manually set it to not be synchronized on character ghosts.

### First Person Character synchronization
If using the First-Person standard character, `FirstPersonCharacterComponent.ViewPitchDegrees` must also be synchronized.

### Third Person Character synchronization
If using the Third-Person standard character, the character orbit camera entity must be a owner-predicted ghost, and its `LocalTransform.Rotation` must be synchronized. The `OrbitCameraSystem` would also have to update in the `PredictedSimulationSystemGroup`

### Parent entity synchronization (moving platforms)
If you use the character's ParentEntity mechanism in your game (used internally for "standing on moving platforms" logic), you must synchronize `KinematicCharacterBody.ParentEntity`, `KinematicCharacterBody.ParentLocalAnchorPoint`, and `KinematicCharacterBody.ParentVelocity`
    * You must also remember that any entity that can be a character's ParentEntity must be a predicted ghost, and they must have their `TrackedTransform.PreviousFixedRateTransform` synchronized.
    * `KinematicCharacterBody.ParentVelocity` synchronization can be omitted if you make sure you only ever de-ground the character from its parent entity (or destroy the parent entity while the character is parented to it) between the `Update_ParentMovement` and `Update_MovingPlatformDetection` steps of the character update.
    * `KinematicCharacterBody.ParentLocalAnchorPoint` synchronization can be omitted if you take care of setting it manually & deterministically after the `Update_Initialize` and before the `Update_ParentMovement` steps of the character update. For full precision, you would have to find the point of contact between the character collider and the parent entity using a physics query, and calculate the contact point's local position compared to the parent entity. Remember that you cannot use the character's ground hit for this, because this would be happening before the character has detected grounding. Otherwise, if you don't mind a precision/quality loss for the logic that makes the character follow a rotating moving platform, you can also set it to the local position of the bottom of the character compared to the parent entity. Since we must choose between a performance penalty (find contact point with a physics query), a quality penalty (using character capsule bottom instead of true contact point), or a bandwidth penalty (synchronizing `KinematicCharacterBody.ParentLocalAnchorPoint`) in a netcode context, this isn't done by default.


## Prediction

Certain changes are necessary in order to enable prediction for all standard character and player systems:
* `First/ThirdPersonCharacterVariableUpdateSystem` need `[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]` and `[UpdateAfter(typeof(PredictedFixedStepSimulationSystemGroup))]`
* `First/ThirdPersonPlayerInputsSystem` need `[UpdateInGroup(typeof(GhostInputSystemGroup))]`
* `First/ThirdPersonPlayerVariableStepControlSystem` need `[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]` and `[UpdateBefore(typeof(First/ThirdPersonCharacterVariableUpdateSystem))]`
* `First/ThirdPersonPlayerFixedStepControlSystem` need `[UpdateInGroup(typeof(PredictedFixedStepSimulationSystemGroup), OrderFirst = true)]`

`First/ThirdPersonCharacterPhysicsUpdateSystem` don't need to change, because they are part of the physics update group, which is automatically moved to the prediction loop by NetCode.

## Player commands
You must replace input handling in all your player systems with Netcode commands. You should use the `IInputComponentData` approach, which provides easy support for button press events using `InputEvent`.

* `First/ThirdPersonPlayerInputsSystem` will become responsible for storing player commands in your `IInputComponentData` component or in your commands buffer.
* `First/ThirdPersonPlayerVariableStepControlSystem` and `First/ThirdPersonPlayerFixedStepControlSystem` will become responsible for getting commands from the appropriate prediction tick, and applying them to the controlled character.


## Character interpolation
There are a few particularities to keep in mind when it comes to character interpolation in a Netcode context:

* On clients, client-owned characters will be predicted and will update at a fixed timestep in the fixed step prediction group; this means they need character interpolation.
* On clients, remote characters will be interpolated by NetCode; this means they must not have the built-in character interpolation.
* On server, there must be no interpolation because there is no presentation.

Because of this situation, you need the `CharacterInterpolation` component to only be present on clients, and on characters that are predicted. The following code can be copied into your project, and will take care of making character interpolation compatible with this networking approach:

```cs
[GhostComponentVariation(typeof(CharacterInterpolation))]
[GhostComponent(PrefabType = GhostPrefabType.PredictedClient)]
public struct CharacterInterpolation_GhostVariant
{ }
```