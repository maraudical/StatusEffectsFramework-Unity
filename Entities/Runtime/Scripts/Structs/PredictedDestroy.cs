#if ENTITIES && NETCODE
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    [GhostComponent(PrefabType = GhostPrefabType.PredictedClient, SendTypeOptimization = GhostSendType.OnlyPredictedClients)]
    [GhostEnabledBit]
    public struct PredictedDestroy : IComponentData, IEnableableComponent { }
}
#endif