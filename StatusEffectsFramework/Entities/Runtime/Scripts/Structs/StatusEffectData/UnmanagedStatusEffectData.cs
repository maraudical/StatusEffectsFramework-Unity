#if ENTITIES
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;

namespace StatusEffectsFramework.Entities
{
    public struct UnmanagedStatusEffectData
    {
        #region Public Properties
        public ushort Id { get; internal set; }
        public StatusEffectGroup Group { get; internal set; }
        public ushort ComparableName { get; internal set; }
        public float BaseValue { get; internal set; }
        public UnityObjectRef<Sprite> Icon { get; internal set; }
        public float4 Color { get; internal set; }
#if LOCALIZED
        public BlobString StatusEffectNameTable { get; internal set; }
        public BlobString StatusEffectNameEntry { get; internal set; }
        public BlobString AcronymTable { get; internal set; }
        public BlobString AcronymEntry { get; internal set; }
        public BlobString DescriptionTable { get; internal set; }
        public BlobString DescriptionEntry { get; internal set; }
#else
        public BlobString StatusEffectName  { get; internal set; }
        public BlobString Acronym  { get; internal set; }
        public BlobString Description  { get; internal set; }
#endif
        public bool AllowEffectStacking { get; internal set; }
        public NonStackingBehaviour NonStackingBehaviour { get; internal set; }
        public int MaxStacks { get; internal set; }
        public BlobArray<UnmanagedEffect> Effects { get; internal set; }
        public BlobArray<UnmanagedCondition> Conditions { get; internal set; }
        public BlobArray<ModuleInfo> Modules { get; internal set; }
        #endregion
    }
}
#endif