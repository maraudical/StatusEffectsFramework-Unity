#if ENTITIES
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    public struct StatusReferences : IComponentData
    {
        internal const int k_ModuleCollectionsInitialCapacity = 5;
        public bool onlyOne;
        public bool TryGetReference(Hash128 id, out BlobAssetReference<UnmanagedStatusEffectData> reference) => IdToStatusEffectDataMap.Value.TryGetValue(id, out reference);

        internal BlobAssetReference<BlobHashMap<Hash128, BlobAssetReference<UnmanagedStatusEffectData>>> IdToStatusEffectDataMap;
    }
}
#endif