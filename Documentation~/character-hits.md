
# Character hits

A character can detect various hits during its update. These hits can be a result of ground detection, movement sweeps, or overlap resolution.

To access those hits, every character has a `DynamicBuffer<KinematicCharacterHit>` buffer that contains all hits detected for this frame. Similarly, every character also has a `DynamicBuffer<StatefulKinematicCharacterHit>` for stateful hits, which are hits with an "Enter/Exit/Stay" state. However, Unity only processes those hits if [`KinematicCharacterProperties.ProcessStatefulCharacterHits`](xref:Unity.CharacterController.KinematicCharacterProperties.ProcessStatefulCharacterHits*) is enabled.

Make sure you only iterate on hits after all update steps of the `KinematicCharacterAspect` have been called, otherwise you'll be at risk of missing some hits.