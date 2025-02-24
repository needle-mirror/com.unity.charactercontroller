# Character components

This package contains character specific [components](https://docs.unity3d.com/Packages/com.unity.entities@latest/index.html?subfolder=/manual/concepts-components.html) that you can set data on to customize the character controllers you create.


## KinematicCharacterProperties

`KinematicCharacterProperties` contains the character data that defines how the character behaves. Nothing in the character update writes to this component: it only reads from it.

Examples of fields contained in this component:
* `InterpolatePosition` and `InterpolateRotation`: Sets whether the character movement should be interpolated.
* `EvaluateGrounding`: Sets whether the character should detect and evaluate grounding.
* `MaxContinuousCollisionsIterations`: Determines the amount of collider cast iterations the character movement update should do before it breaks out of the loop.
* `SimulateDynamicBody`: Sets whether the character should simulate forces when interacting with dynamic Rigidbody components.

## KinematicCharacterBody

`KinematicCharacterBody` contains the character data that the character update and your code calculates and writes to.

Examples of fields contained in this component:
* `IsGrounded` and `GroundHit`: Contains information about the grounding that was detected during the update.
* `RelativeVelocity`: Determines what the current velocity of the character is, relative to its parent (if any).
* `ParentEntity`: Determines the parent entity of the character. This is typically used for moving platforms.


## StoredKinematicCharacterData

`StoredKinematicCharacterData` is a component that you will most likely never have to interact with, but it enables the character update to be performed safely in parallel and in a deterministic way.

During its physics update, a character might need to access data on other character entities, such as `Mass` or `RelativeVelocity`, and this data might change during each character's update (which may be happening in parallel). Therefore, to allow determinism, it's important to store a snapshot of all the character data before the character updates are executed. `StoredKinematicCharacterData` is where that data is stored, and this process of storing the data happens automatically.


## Character dynamic buffer components

The character entity includes several dynamic buffer components in to store information during its update:

* `KinematicCharacterHit`: Contains all hits that were detected during the character update.
* `StatefulKinematicCharacterHit`: Contains all hits that were detected during the character update, but with state information (Enter/Exit/Stay).
* `KinematicCharacterDeferredImpulse`: Stores impulses during the character update, and is used to apply them later on a single thread.
* `KinematicVelocityProjectionHit`: Stores all hits that should participate in the character's velocity projection.


## Other

* `TrackedTransform`: You can add this component to any moving entity that can be assigned as the `ParentEntity` of the character.
