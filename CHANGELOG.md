# Changelog


## [1.3.12] - 2025-07-22

### Fixed
* Fixed incorrect center of mass evaluation in `PhysicsUtilities.SolveCollisionImpulses`


## [1.3.10] - 2025-02-24

### Changed
* Updated package dependencies
* Updated Standard Characters (samples) to use com.unity.inputsystem package by default.


## [1.2.4] - 2024-09-17

### Changed
* Updated package dependencies



## [1.2.0-pre.2] - 2024-05-30

### Changed
* Updated package dependencies

### Added
* Added new version of `KinematicCharacterUtilities.BakeCharacter` that takes an `IBaker` as parameter, as opposed to a `Baker<T>`

### Removed
* Removed the `RotateAroundPoint` method of `MathUtilities`, which was unused by the package.


## [1.1.0-exp.10] - 2023-09-21

### Changed
* Refactoring of "Standard Characters" sample in order to make them easier to convert to netcode.
* Uniform scaling of character entities at runtime, using the `LocalTransform.Scale` value, is now supported. Note that since characters are physics objects, any uniform scale set in authoring will be baked to an entity with a `LocalTransform.Scale` of 1.
* `KinematicCharacterAspect` query functions now take character scale as parameter.
* Removed core character components from the required entity query for character interpolation system updates. This allows any entity with `CharacterInterpolation` and transform components to re-use the character's interpolation strategy.

### Fixed
* Added checks to validate the presence of components on entities before accessing them in `KinematicCharacterDeferredImpulsesJob`
* Changed fixed-step character interpolation job to take into account only entities with the enabled `Simulate` component (fixed interpolation issue in netcode contexts).
* Fixed an incorrect calculation of character decollision vectors under certain circumstances.

### Removed
* The `KinematicCharacterVariableUpdateGroup` was removed, since there are cases where it is important for this group to be user-defined in order to have better control over when it updates. In order to replace it, add these rules to your systems (or create a new systems group that has these rules):
    * `[UpdateInGroup(typeof(SimulationSystemGroup))]`
    * `[UpdateAfter(typeof(FixedStepSimulationSystemGroup))]`
    * `[UpdateBefore(typeof(TransformSystemGroup))]`


## [1.0.0-exp.22] - 2023-05-11

### Changed

* Character aspects are no longer passed by ref in standard character jobs
* Removed all reliance on `PhysicsBodyAuthoring` and `PhysicsShapeAuthoring` components
* `DisableCharacterDynamicPairsSystem` was renamed to `DisableCharacterDynamicContactsSystem`

### Fixed

* Characters can now raise trigger events with trigger shapes that are on dynamic rigidbodies


## [1.0.0-exp.5] - 2023-03-30

### Fixed

* Fixed an error occuring when the Havok package is present in the project


## [1.0.0-exp.4] - 2023-03-23

### Upgrade guide

Follow these steps to upgrade your existing character controllers to this new version:
* In your character prefabs, make sure the `PhysicsShape`'s collision response is set to "Collide". (previously, it was set to "Raise Trigger Events" by default)
* The default implementation of your character Aspect's `CanCollideWithHit` should now simply do: `return PhysicsUtilities.IsCollidable(hit.Material);`

### Added

* Added a `DisableCharacterDynamicPairsSystem`, which disables physics body pairs between dynamic rigidbodies and "simulated dynamic" characters. This means "simulated dynamic" characters no longer need to rely on having their collision response set to "None" or "Raise Trigger Events" in order to properly be pushed by other rigidbodies. All character collision responses should now be set to "Collide". This system's update can be disabled by destroying the `DisableCharacterDynamicPairs` singleton at runtime

### Changed

* All authorings now explicitly specify transform usage flags

### Removed

* `KinematicCharacterUtilities.IsHitCollidableOrCharacter` was removed. Use `PhysicsUtilities.IsCollidable` instead
* `KinematicCharacterProperties.SetCollisionDetectionActive` was removed. Use `KinematicCharacterUtilities.SetCollisionDetectionActive` instead

### Fixed

* Character interpolation is now ignored on disabled characters (disabled `KinematicCharacterBBody` component)


## [1.0.0-exp.2] - 2023-02-22

Initial release
