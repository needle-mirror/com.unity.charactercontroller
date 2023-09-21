---
uid: api-index
---

# Character Controller API reference

This page contains an overview of some key APIs of the Character Controller package.

| **Aspects** | **Description**|
| :--- | :--- |
| [KinematicCharacterAspect](xref:Unity.CharacterController.KinematicCharacterAspect) | The aspect containing all base character data, update steps, and utilities. |

| **Interfaces** | **Description**|
| :--- | :--- |
| [IKinematicCharacterProcessor](xref:Unity.CharacterController.IKinematicCharacterProcessor`1) | Interface for implementing various customizable functions within the character update. Users are expected to create character aspects that implements this interface in order to create their own custom character implementation. The Standard Characters use this as well. |

| **Authorings** | **Description**|
| :--- | :--- |
| [TrackedTransformAuthoring](xref:Unity.CharacterController.TrackedTransformAuthoring) | Used to add a [TrackedTransform](xref:Unity.CharacterController.TrackedTransform) component on entities that can be a character "parent". |

| **Components** | **Description**|
| :--- | :--- |
| [KinematicCharacterProperties](xref:Unity.CharacterController.KinematicCharacterProperties) | Contains the character data that defines how it behaves. Nothing in the character update will write to this component; it only reads from it. |
| [KinematicCharacterBody](xref:Unity.CharacterController.KinematicCharacterBody) | Contains the character data that may get written to by the character update. |
| [StoredKinematicCharacterData](xref:Unity.CharacterController.StoredKinematicCharacterData) | Stores key character data before the character update (data that a character A might need to access on a character B). This allows deterministic parallel execution of the character update. |
| [KinematicCharacterHit](xref:Unity.CharacterController.KinematicCharacterHit) | DynamicBuffer containing all hits that were detected during the character update. |
| [StatefulKinematicCharacterHit](xref:Unity.CharacterController.StatefulKinematicCharacterHit) | DynamicBuffer containing all hits that were detected during the character update, but with state information (Enter/Exit/Stay). |
| [KinematicCharacterDeferredImpulse](xref:Unity.CharacterController.KinematicCharacterDeferredImpulse) | DynamicBuffer containing impulses added during the character update (to be processed later in a single-threaded system) |
| [KinematicVelocityProjectionHit](xref:Unity.CharacterController.KinematicVelocityProjectionHit) | DynamicBuffer containing only the hits that participate in velocity projection.  |
| [TrackedTransform](xref:Unity.CharacterController.TrackedTransform) | Tracks the previous & current transform of a root entity. This is used for entities that can be a character "parent". |

| **Systems** | **Description**|
| :--- | :--- |
| [KinematicCharacterPhysicsUpdateGroup](xref:Unity.CharacterController.KinematicCharacterPhysicsUpdateGroup) | Provides a sensible default update point for fixed-rate character physics update systems (used by Standard Characters). |
| [KinematicCharacterDeferredImpulsesSystem](xref:Unity.CharacterController.KinematicCharacterDeferredImpulsesSystem) | Handles applying impulses stored in the [KinematicCharacterDeferredImpulse](xref:Unity.CharacterController.KinematicCharacterDeferredImpulse) buffer, after the character update. |
| [StoreKinematicCharacterBodyPropertiesSystem](xref:Unity.CharacterController.StoreKinematicCharacterBodyPropertiesSystem) | Handles storing character data in the [StoredKinematicCharacterData](xref:Unity.CharacterController.StoredKinematicCharacterData) component. This allows deterministic parallel execution of the character update. |
| [TrackedTransformFixedSimulationSystem](xref:Unity.CharacterController.TrackedTransformFixedSimulationSystem) | Handles storing "previous transform" data in all [TrackedTransform](xref:Unity.CharacterController.TrackedTransform) components. |
| [CharacterInterpolationRememberTransformSystem ](xref:Unity.CharacterController.CharacterInterpolationRememberTransformSystem ) | Handles remembering the "previous transform" used for interpolation calculations. |
| [CharacterInterpolationSystem ](xref:Unity.CharacterController.CharacterInterpolationSystem ) | Handles interpolating the character visual transform. |

| **Utilities** | **Description**|
| :--- | :--- |
| [KinematicCharacterUtilities](xref:Unity.CharacterController.KinematicCharacterUtilities) | Contains various functions related to character queries, character entity creation, character baking, and others. |
| [CharacterControlUtilities](xref:Unity.CharacterController.CharacterControlUtilities) | Contains various functions related to controlling the character velocity & rotation. |
| [MathUtilities](xref:Unity.CharacterController.MathUtilities) | Contains various math helper functions. |
| [PhysicsUtilities](xref:Unity.CharacterController.PhysicsUtilities) | Contains various physics helper functions. |
