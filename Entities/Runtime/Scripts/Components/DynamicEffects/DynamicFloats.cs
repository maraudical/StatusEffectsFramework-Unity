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
    public struct DynamicFloats : IBufferElementData
    {
#if NETCODE
        [GhostField]
#endif
        public uint Id;
#if NETCODE
        [GhostField(Composite = true)]
#endif
        public TypeIndex SystemType;
#if NETCODE
        [GhostField(Composite = true)]
#endif
        public Hash128 StatusName;
#if NETCODE
        [GhostField]
#endif
        public ValueModifier ValueModifier;
#if NETCODE
        [GhostField]
#endif
        public bool PostEvaluate;
#if NETCODE
        [GhostField]
#endif
        public int Priority;
#if NETCODE
        [GhostField]
#endif
        public float Value;
    }
}
#endif