using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    [BurstCompile]
    public struct UnmanagedStatusRegistry : IComponentData
    {
        public ushort Version { get; internal set; }

        /// <summary>
        /// Gets the <see cref="UnmanagedStatusEffectData"/> for an <paramref name="id"/> as a reference.
        /// </summary>
        /// <param name="exists">True if the data was found. If false the returned reference is null and must not be used.</param>
        [BurstCompile]
        public ref UnmanagedStatusEffectData GetStatusEffectDataOrNullRef(ushort id, out bool exists)
        {
            return ref Data.Value.IdToStatusEffectData.GetValueRefOrNullRef(id, out exists);
        }

        [BurstCompile]
        public bool HasStatusEffectData(ushort id) => Data.Value.IdToStatusEffectData.ContainsKey(id);

        [BurstCompile]
        public bool TryGetId(Hash128 key, out ushort id) => Data.Value.KeyToId.TryGetValue(key, out id);

        /// <inheritdoc cref="GetStatusEffectDataOrNullRef"/>
        /// <remarks>Logs an error if the data doesn't exist.</remarks>
        [BurstCompile]
        internal ref UnmanagedStatusEffectData GetStatusEffectDataOrNullRefDebug(ushort id, out bool exists)
        {
            ref var data = ref GetStatusEffectDataOrNullRef(id, out exists);

            if (Hint.Unlikely(!exists))
                UnityEngine.Debug.LogError($"An {nameof(UnmanagedStatusEffectData)} with id \"{id}\" does not exist in the {nameof(UnmanagedStatusRegistry)}. Make sure all loaded {nameof(StatusEffectData)} have been added to the {nameof(StatusRegistry)}!");

            return ref data;
        }

        internal const int CollectionsInitialCapacity = 16;

        /// <summary>
        /// Every dynamic effect buffer type used by the registry, sorted. Systems that access these buffers
        /// through raw pointers register them so job dependencies are tracked.
        /// </summary>
        internal ref BlobArray<TypeIndex> DynamicEffectTypes => ref Data.Value.DynamicEffectTypes;

        /// <inheritdoc cref="DynamicEffectTypes"/>
        internal ref BlobArray<TypeIndex> ModuleTypes => ref Data.Value.ModuleTypes;

        /// <summary>
        /// All registry data lives in a single blob so it is created, swapped and disposed as one unit.
        /// </summary>
        internal BlobAssetReference<UnmanagedStatusRegistryData> Data;
    }

    internal struct UnmanagedStatusRegistryData
    {
        public BlobHashMap<ushort, UnmanagedStatusEffectData> IdToStatusEffectData;
        public BlobHashMap<Hash128, ushort> KeyToId;
        public BlobArray<TypeIndex> DynamicEffectTypes;
        public BlobArray<TypeIndex> ModuleTypes;
    }
}