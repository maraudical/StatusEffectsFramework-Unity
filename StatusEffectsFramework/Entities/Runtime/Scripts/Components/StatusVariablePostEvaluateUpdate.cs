#if ENTITIES
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
#if NETCODE
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    [GhostEnabledBit]
#endif
    public struct StatusVariablePostEvaluateUpdate : IComponentData, IEnableableComponent { }
}
#endif