#if ENTITIES && ADDRESSABLES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    public struct StatusReferences : IComponentData
    {
        public bool TryGetReference(FixedString64Bytes id, out BlobAssetReference<StatusEffectData> reference) => StatusEffectDatas.Value.TryGetValue(id, out reference);

        public BlobAssetReference<BlobHashMap<FixedString64Bytes, BlobAssetReference<StatusEffectData>>> StatusEffectDatas;
    }
}
#endif