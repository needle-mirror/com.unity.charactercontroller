using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace Unity.CharacterController
{
    /// <summary>
    /// Authoring component for an entity that requires transform tracking
    /// </summary>
    [DisallowMultipleComponent]
    public class TrackedTransformAuthoring : MonoBehaviour
    {
        class Baker : Baker<TrackedTransformAuthoring>
        {
            public override void Bake(TrackedTransformAuthoring authoring)
            {
                RigidTransform currentTransform = new RigidTransform(authoring.transform.rotation, authoring.transform.position);
                TrackedTransform trackedTransform = new TrackedTransform
                {
                    CurrentFixedRateTransform = currentTransform,
                    PreviousFixedRateTransform = currentTransform,
                };

                AddComponent(trackedTransform);
            }
        }
    }
}
