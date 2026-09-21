#if ENTITIES
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace StatusEffectsFramework.Entities
{
    public struct UnmanagedStatusEffectData
    {
        #region Public Properties
        public ushort Id => InternalId;
        public StatusEffectGroup Group => InternalGroup;
        public ushort ComparableName => InternalComparableName;
        public float BaseValue => InternalBaseValue;
        public UnityObjectRef<Sprite> Icon => InternalIcon;
        public float4 Color => InternalColor;
#if LOCALIZED
        public BlobString StatusEffectNameTable => InternalStatusEffectNameTable;
        public BlobString StatusEffectNameEntry => InternalStatusEffectNameEntry;
        public BlobString AcronymTable => InternalAcronymTable;
        public BlobString AcronymEntry => InternalAcronymEntry;
        public BlobString DescriptionTable => InternalDescriptionTable;
        public BlobString DescriptionEntry => InternalDescriptionEntry;
#else
        public BlobString StatusEffectName => InternalStatusEffectName;
        public BlobString Acronym => InternalAcronym;
        public BlobString Description => InternalDescription;
#endif
        public bool AllowEffectStacking => InternalAllowEffectStacking;
        public NonStackingBehaviour NonStackingBehaviour => InternalNonStackingBehaviour;
        public int MaxStacks => InternalMaxStacks;
        public BlobArray<UnmanagedEffect> Effects => InternalEffects;
        public BlobArray<UnmanagedCondition> Conditions => InternalConditions;
        public BlobArray<ModuleInfo> Modules => InternalModules;
        #endregion

        #region Internal Fields
        internal ushort InternalId;
        internal StatusEffectGroup InternalGroup;
        internal ushort InternalComparableName;
        internal float InternalBaseValue;
        internal UnityObjectRef<Sprite> InternalIcon;
        internal float4 InternalColor;
#if LOCALIZED
        internal BlobString InternalStatusEffectNameTable;
        internal BlobString InternalStatusEffectNameEntry;
        internal BlobString InternalAcronymTable;
        internal BlobString InternalAcronymEntry;
        internal BlobString InternalDescriptionTable;
        internal BlobString InternalDescriptionEntry;
#else
        internal BlobString InternalStatusEffectName;
        internal BlobString InternalAcronym;
        internal BlobString InternalDescription;
#endif
        internal bool InternalAllowEffectStacking;
        internal NonStackingBehaviour InternalNonStackingBehaviour;
        internal int InternalMaxStacks;
        internal BlobArray<UnmanagedEffect> InternalEffects;
        internal BlobArray<UnmanagedCondition> InternalConditions;
        internal BlobArray<ModuleInfo> InternalModules;
        #endregion
    }
}
#endif