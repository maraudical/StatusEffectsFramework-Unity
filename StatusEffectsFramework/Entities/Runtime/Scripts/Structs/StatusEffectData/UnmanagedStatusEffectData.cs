#if ENTITIES
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace StatusEffectsFramework.Entities
{
    public struct UnmanagedStatusEffectData
    {
        #region Public Properties
        public ushort Id;
        public StatusEffectGroup Group;
        public ushort ComparableName;
        public float BaseValue;
        public UnityObjectRef<Sprite> Icon;
        public float4 Color;
#if LOCALIZED
        public BlobString StatusEffectNameTable;
        public BlobString StatusEffectNameEntry;
        public BlobString AcronymTable;
        public BlobString AcronymEntry;
        public BlobString DescriptionTable;
        public BlobString DescriptionEntry;
#else
        public BlobString StatusEffectName;
        public BlobString Acronym;
        public BlobString Description;
#endif
        public bool AllowEffectStacking;
        public NonStackingBehaviour NonStackingBehaviour;
        public int MaxStacks;
        public BlobArray<UnmanagedEffect> Effects;
        public BlobArray<UnmanagedCondition> Conditions;
        public BlobArray<ModuleInfo> Modules;
        #endregion
    }
}
#endif