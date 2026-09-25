#if ENTITIES
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Tags an entity whose status variables still need their ids resolved from the
    /// <see cref="UnmanagedStatusRegistry"/>. Removed by the <see cref="StatusVariableIdResolverSystem"/>
    /// once the entity has been resolved.
    /// </summary>
    public struct StatusResolver : IComponentData { }
}
#endif
