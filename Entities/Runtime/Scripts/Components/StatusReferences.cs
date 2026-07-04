#if ENTITIES
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    public struct StatusReferences : IComponentData
    {
        public bool TryGetReference(Hash128 id, out BlobAssetReference<UnmanagedStatusEffectData> reference) => IdToStatusEffectDataMap.Value.TryGetValue(id, out reference);

        internal const int k_CollectionsInitialCapacity = 16;

        public bool onlyOne;
        internal ModuleOffsets ModuleOffsets;
        internal DynamicFloatOffsets DynamicFloatOffsets;
        internal DynamicIntOffsets DynamicIntOffsets;
        internal DynamicBoolOffsets DynamicBoolOffsets;

        internal BlobAssetReference<BlobHashMap<Hash128, BlobAssetReference<UnmanagedStatusEffectData>>> IdToStatusEffectDataMap;
    }

    internal struct ModuleOffsets
    {
        public int Struct;
    }

    internal struct DynamicFloatOffsets
    {
        public int StatusName;
        public int ValueModifier;
        public int PostEvaluate;
        public int Priority;
        public int Value;
        public int Struct;
    }

    internal struct DynamicIntOffsets
    {
        public int StatusName;
        public int ValueModifier;
        public int PostEvaluate;
        public int Priority;
        public int Value;
        public int Struct;
    }

    internal struct DynamicBoolOffsets
    {
        public int StatusName;
        public int PostEvaluate;
        public int Priority;
        public int Value;
        public int Struct;
    }
}
#endif