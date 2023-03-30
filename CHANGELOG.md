# Changelog

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
