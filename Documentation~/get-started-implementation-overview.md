
# Standard characters implementation overview

This is an overview of the contents of the **Standard Characters** assets that you can import into your project via the **Samples** tab of the Character Controller Package Manager window (**Window > Package Manager > Character Controller > Samples**). Unity adds the standard character files to your project, under the `Samples/Character Controller/[version]/Standard Characters` folder.

## Prefabs

The Standard Characters include the following prefabs:

* Player prefabs (**ThirdPersonPlayer**, **FirstPersonPlayer**): Represents the human that controls the character and camera, if applicable, with input.
* Character prefabs (**ThirdPersonCharacter**, **FirstPersonCharacter**): Represents the character that moves in the world.
* Camera prefab (**OrbitCamera**): Represents the camera that orbits around the [third-person character](get-started-third-person.md).

## Controlling the character

Here are the main steps of how input is converted into character movement by the standard character systems:

* The Player gathers raw input from input systems
* The Player converts that raw input to a format that the character can easily work with, and writes that data to the Control component (`ThirdPersonCharacterControl` or `FirstPersonCharacterControl`) on the character. This control component acts as a common interface for controlling the character, whether it's a human or an AI controlling it.
* The character moves with the input stored in its control component

The following steps describe how this happens in code, using the third-person character as an example:

* `InitializationSystemGroup`
    * `ThirdPersonPlayerInputsSystem` gathers input from Unity's input system every frame. It iterates on all entities with a `ThirdPersonPlayerInputs` component, and writes the raw input to that component.
* `SimulationSystemGroup`
    * `FixedStepSimulationSystemGroup`
        * `ThirdPersonPlayerFixedStepControlSystem` handles taking the raw input from `ThirdPersonPlayerInputs`, and converting it to a format that the character can work with in the `ThirdPersonCharacterControl` component. It does this for inputs that are meant to be consumed during the fixed update.
        * `ThirdPersonCharacterPhysicsUpdateSystem` schedules a job that calls `ThirdPersonCharacterAspect.PhysicsUpdate`. This is where the character moves and solves collisions, based on inputs stored in the `ThirdPersonCharacterControl` component.
    * `ThirdPersonPlayerVariableStepControlSystem` handles taking the raw input from `ThirdPersonPlayerInputs`, and converting it to a format that the character and camera can work with in the `ThirdPersonCharacterControl` and `OrbitCameraControl` components respectively. It does this for inputs that are meant to be consumed during the regular (non-fixed) update.
    * `ThirdPersonCharacterVariableUpdateSystem` schedules a job that calls `ThirdPersonCharacterAspect.VariableUpdate`. This is where the character handles rotation, based on inputs stored in the `ThirdPersonCharacterControl` component.
    * `OrbitCameraSimulationSystem` handles camera rotation and basic distance calculations, based on inputs stored in the `OrbitCameraControl` component.
    * `TransformSystemGroup` updates.
    * `OrbitCameraLateUpdateSystem` handles camera smoothing and obstruction calculations. This is done after the transforms update so that moving physics obstructions can be smoothed out with interpolation.

The first person character update is very similar, except it doesn't have "orbit camera" systems and components.


## Customize character movement

### Velocity

Character velocity control is mainly implemented in your character aspect's `HandleVelocityControl` method. Here's a summary of what it does:

```cs
void HandleVelocityControl()
{
    // if the character is grounded
        // Move on ground by interpolating velocity towards a target max velocity
        // Handle jumping if there is jump input
    // otherwise, if the character is in air
        // Move in air by accelerating velocity towards a target max velocity, but also prevent that acceleration if we're moving against a steep wall
        // Apply gravity to character's velocity
        // Apply drag to character's velocity
}
```
This implementation is nothing more than a default starting point. Feel free to customize, change or replace anything in `HandleVelocityControl` to implement any character movement feature you need for your game.

### Rotation

Rotation is implemented in your character aspect's `VariableUpdate`. This is a default starting point that you could fully customize to suit your needs.

### Velocity and rotation update frequencies

Velocity and rotation are handled in different locations because they have a different recommended frequency at which they should be updated.

Velocity is best handled at a fixed timestep, because it directly participates in collisions and physics. The fixed timestep is ideal because of its consistency and predictability.

Rotation is often best handled at a variable timestep (in sync with the rendering rate), because if it was handled at a fixed timestep with interpolation instead, there would be a noticeable mouse input unresponsiveness.

This is especially noticeable for first-person characters or third-person characters that can aim directly in the camera direction while it rotates. Rotation can also participate in collisions and physics in certain cases such as when a character capsule rotates on an axis other than Y, but this typically has less of an impact on physics solving than the velocity does.

In any case, you should know that the decision to handle rotation at a variable rate is not a hard requirement; but only a default suggestion. If you believe that rotation would be better handled at a fixed timestep for your game, then you can move the rotation-handling code to your character aspect's `PhysicsUpdate` instead, and enable rotation interpolation in your character authoring.


## Separate player and character entities

There are a few reasons why character and player are two different entities, two different sets of components, and two different sets of systems in the standard characters:

* A character prefab and system should be controllable by either a human player or an AI controller. For this reason, the character prefab should be independent of any notion of who is controlling it. Sometimes the controller will be a human player (such as when you assign the Character prefab as the **Controlled Character** of the Player prefab), but other times the same character prefab could be assigned to an AI controller. This is why the character Control component exists (`First/ThirdPersonCharacterControl`). It acts as a common interface for controlling the character, whether the controller is human or AI.
* AI characters shouldn't have any knowledge of a camera, but a human-controlled character must often move relative to the camera orientation. Because of this, calculating a camera-relative move input is the responsibility of the system that handles the human player logic and not of the character systems. The human player knows about the camera, and feeds camera-relative input to its controlled character. That way, the character can move relative to the camera, without ever having to keep any kind of reference to a camera.
* There are often cases where you need to destroy the Character entity, but you want the concept of the Player to survive. For example, in an online shooter game, you might have your character die, but your Player (score, name, money, unlockables, etc...) persists while awaiting the respawning of the Character. In these cases, because the Player and Character are separate entities, you can easily destroy the character entity, spawn a new one, and assign that new character as the "controlled character" of the Player.
* At runtime, you might want to switch the entity that your player controls. You could switch characters, or you could switch to controlling a horse or vehicle. When you switch the controlled entity like this, you might want to preserve data belonging to the player controlling the entity. The separation of Player and Character once again makes cases like these easier to deal with.

However, this is by no means a hard requirement for things to work. If you want to, you can change things so that the character entity and systems also handle player input directly.


## Player input

Player input is implemented in a way that you might not expect:

* It's split into 3 different systems.
* It uses a `FixedInputEvent` struct for button press input events, instead of using a Boolean.
* The player writes input to their controlled character, instead of the character systems querying inputs directly.

While it would be possible to make things work with a simpler structure, things are structured this way due to the combination of several different considerations:

* Some inputs need processing at a variable rate, and some need processing at a fixed rate.
* There are certain challenges related to handling input that must be processed at a fixed timestep. For more information, see the documentation on [Input handling](input-handling.md).
* This structure allows characters to be controlled by either humans or AI, without requiring different implementations based on who or what controls them.
* This structure allows characters to be already setup for easy Netcode compatibility (storing player commands and using player commands are handled in different systems).

