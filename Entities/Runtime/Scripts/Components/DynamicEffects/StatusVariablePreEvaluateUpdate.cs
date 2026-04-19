using Unity.Entities;
using Unity.NetCode;

namespace StatusEffectFramework.Entities
{
#if NETCODE
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    [GhostEnabledBit]
#endif
    public struct StatusVariablePreEvaluateUpdate : IComponentData, IEnableableComponent { }
}
