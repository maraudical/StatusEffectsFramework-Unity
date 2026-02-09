#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public struct StatusReferences : IComponentData
    {
        public bool TryGetReference(Hash128 id, out BlobAssetReference<StatusEffectData> reference) => BlobAsset.Value.TryGetValue(id, out reference);

        internal BlobAssetReference<BlobHashMap<Hash128, BlobAssetReference<StatusEffectData>>> BlobAsset;
        internal BlobAssetReference<BlobHashMap<ulong, ulong>> ModuleToSystemTypeMap;
    }
}
#endif