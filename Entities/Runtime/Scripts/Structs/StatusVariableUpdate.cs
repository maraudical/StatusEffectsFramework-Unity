#if ENTITIES
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
#if NETCODE
    [GhostEnabledBit]
#endif
    public struct StatusVariableUpdate : IComponentData, IEnableableComponent { }
}
#endif