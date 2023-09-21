using Unity.Entities;
using Unity.Physics.Systems;
using Unity.Transforms;

namespace Unity.CharacterController
{
    /// <summary>
    /// System group for the default character physics update
    /// </summary>
    [UpdateInGroup(typeof(AfterPhysicsSystemGroup))]
    public partial class KinematicCharacterPhysicsUpdateGroup : ComponentSystemGroup
    {
    }
}