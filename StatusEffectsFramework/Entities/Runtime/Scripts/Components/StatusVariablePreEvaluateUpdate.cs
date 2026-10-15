using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
#if NETCODE
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted, SendDataForChildEntity = true)]
    [GhostEnabledBit]
#endif
    public struct StatusVariablePreEvaluateUpdate : IComponentData, IEnableableComponent { }
}