#if ENTITIES && NETCODE
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    [GhostComponent(PrefabType = GhostPrefabType.Client)]
    internal struct StatusEffectsInterpolated : IBufferElementData
    {
        public uint Id;
        public int Stacks;
    }
}
#endif