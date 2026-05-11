#if ENTITIES
using System;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectFramework.Entities
{
#if NETCODE
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
#endif
    [ChunkSerializable]
    public struct DynamicFloats : IBufferElementData
    {
        public uint Id;
        public TypeIndex TypeIndex;
        public Hash128 StatusName;
        public ValueModifier ValueModifier;
        public bool PostEvaluate;
        public int Priority;
        public float Value;
        public DynamicEffectInfo DynamicEffectInfo;
    }
}
#endif