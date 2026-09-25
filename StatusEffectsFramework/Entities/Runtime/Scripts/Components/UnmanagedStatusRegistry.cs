#if ENTITIES
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    [BurstCompile]
    public struct UnmanagedStatusRegistry : IComponentData
    {
        public ushort Version { get; internal set; }

        [BurstCompile]
        public ref UnmanagedStatusEffectData GetStatusEffectData(ushort id)
        {
            if (Hint.Unlikely(!IdToStatusEffectData.Value.ContainsKey(id)))
                throw new System.InvalidOperationException($"An {nameof(UnmanagedStatusEffectData)} with id \"{id}\" does not exist in the {nameof(UnmanagedStatusRegistry)}. Make sure all loaded {nameof(StatusEffectData)} have been added to the {nameof(StatusRegistry)} before rebuilding!");

            return ref IdToStatusEffectData.Value.GetValueRef(id);
        }

        [BurstCompile]
        public bool TryGetId(Hash128 key, out ushort id) => KeyToId.Value.TryGetValue(key, out id);

        internal const int CollectionsInitialCapacity = 16;

        internal BlobAssetReference<BlobHashMap<ushort, UnmanagedStatusEffectData>> IdToStatusEffectData;
        internal BlobAssetReference<BlobHashMap<Hash128, ushort>> KeyToId;
    }
}
#endif