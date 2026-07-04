using Unity.Entities;
using Unity.NetCode;

namespace StatusEffectsFramework.Entities
{
#if NETCODE
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    [GhostEnabledBit]
#endif
    public struct StatusVariablePostEvaluateUpdate : IComponentData, IEnableableComponent { }
}
