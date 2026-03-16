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
    public struct DynamicEffectFloats : IBufferElementData
    {
#if NETCODE
        [GhostField]
#endif
        public Hash128 Id;
#if NETCODE
        [GhostField]
#endif
        public ValueModifier ValueModifier;
#if NETCODE
        [GhostField]
#endif
        public float Value;
    }
}
#endif