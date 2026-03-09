#if ENTITIES
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectFramework.Entities
{
#if NETCODE
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
#endif
    internal struct StatusManagerComponent : IComponentData
    {
#if NETCODE
        [GhostField]
#endif
        public uint AvailableId;
    }
}
#endif