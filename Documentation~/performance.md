
# Performance overview

This section contains information on best practices around character controller performance.

## Level performance

For the level geometry, use convex collider shapes whenever possible, and make sure your non-convex colliders have as little polygons as possible. It's standard practice in games to create simplified collision meshes for mesh objects.

## Physics performance

Your character doesn't need any physics step to function properly. So, you can safely set the "Simulation Type" to "None" on the Physics Step component, if you don't need any physics step for physics bodies other than characters.

## Grounding performance

To detect grounding, the character does a downward collider cast with the length `KinematicCharacterProperties.GroundSnappingDistance`. You should keep this value as small as possible to get the job done, because the longer the collider cast, the heavier it can be to process.

## KinematicCharacterProperties performance

Don't assign iteration counts that are too high for `KinematicCharacterProperties.MaxContinuousCollisionsIterations` and `KinematicCharacterProperties.MaxOverlapDecollisionIterations`. If in doubt, keep the defaults.

Enabling `KinematicCharacterProperties.ProjectVelocityOnInitialOverlaps` on the character rigidbody adds one CalculateDistance call per character. Only activate it if you need it (it can help prevent tunnelling when your character has a shape that changes its collisions when it is rotated).

Enabling `KinematicCharacterProperties.DetectObstructionsForParentBodyMovement` on the character rigidbody adds one CastCollider call per character when the character has a "ParentEntity" set.

## Slope and step performance

Enabling either `PreventGroundingWhenMovingTowardsNoGrounding` or `HasMaxDownwardSlopeChangeAngle` on your characters has a performance impact, because it adds a few raycast queries per character. Make sure you only use this when necessary.

Enabling step handling adds the cost of potentially up to 3 collider casts on each hit that is considered non-grounded, as well as a few additional raycasts.
