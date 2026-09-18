#if ENTITIES
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    public struct UnmanagedStatusRegistry : IComponentData
    {
        public ushort Version { get; internal set; }

        public bool TryGetStatusEffectData(ushort id, out BlobAssetReference<UnmanagedStatusEffectData> reference) => IdToStatusEffectData.Value.TryGetValue(id, out reference);
        public bool TryGetId(Hash128 key, out ushort id) => KeyToId.Value.TryGetValue(key, out id);

        internal const int CollectionsInitialCapacity = 16;
        
        internal ModuleOffsets ModuleOffsets;
        internal DynamicFloatOffsets DynamicFloatOffsets;
        internal DynamicIntOffsets DynamicIntOffsets;
        internal DynamicBoolOffsets DynamicBoolOffsets;

        internal BlobAssetReference<BlobHashMap<ushort, BlobAssetReference<UnmanagedStatusEffectData>>> IdToStatusEffectData;
        internal BlobAssetReference<BlobHashMap<Hash128, ushort>> KeyToId;
    }

    internal struct ModuleOffsets
    {
        public int Struct;
    }

    internal struct DynamicFloatOffsets
    {
        public int Id;
        public int ValueModifier;
        public int PostEvaluate;
        public int Priority;
        public int Value;
        public int Struct;
    }

    internal struct DynamicIntOffsets
    {
        public int Id;
        public int ValueModifier;
        public int PostEvaluate;
        public int Priority;
        public int Value;
        public int Struct;
    }

    internal struct DynamicBoolOffsets
    {
        public int Id;
        public int PostEvaluate;
        public int Priority;
        public int Value;
        public int Struct;
    }
}
#endif