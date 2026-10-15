#if NETCODE
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffectsFramework.Entities
{
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
    public struct PredictedStatusEffectsFlag : IComponentData { }
}
#endif