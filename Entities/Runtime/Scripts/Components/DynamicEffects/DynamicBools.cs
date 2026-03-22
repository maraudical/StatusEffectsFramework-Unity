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
    public struct DynamicBools : IBufferElementData
    {
#if NETCODE
        [GhostField]
#endif
        public uint Id;
#if NETCODE
        [GhostField]
#endif
        public TypeIndex SystemType;
#if NETCODE
        [GhostField]
#endif
        public bool PostEvaluate;
#if NETCODE
        [GhostField]
#endif
        public bool Value;
#if NETCODE
        [GhostField]
#endif
        public int Priority;
    }
}
#endif