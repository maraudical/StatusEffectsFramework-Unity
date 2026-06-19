#if ENTITIES
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    public struct StatusReferences : IComponentData
    {
        /// <summary>
        /// Every frame, the system will reset the <see cref="ModuleTypesCapacity"/> to this default value. On initialization with will be 15.
        /// </summary>
        public int DefaultModuleTypesCapacity;
        /// <summary>
        /// In a given frame, there is a maximum amount of status effects you can add due to a limitation of having to 
        /// declare how many unique modules you will use. The every frame it will be reset to the default of 
        /// <see cref="DefaultModuleTypesCapacity"/> but if you were adding a lot of different types of status effects 
        /// all at once you can override this value temporarily.
        /// </summary>
        public int ModuleTypesCapacity;
        public bool TryGetReference(Hash128 id, out BlobAssetReference<UnmanagedStatusEffectData> reference) => IdToStatusEffectDataMap.Value.TryGetValue(id, out reference);

        internal BlobAssetReference<BlobHashMap<Hash128, BlobAssetReference<UnmanagedStatusEffectData>>> IdToStatusEffectDataMap;
    }
}
#endif