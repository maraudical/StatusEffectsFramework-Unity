#if ENTITIES && ADDRESSABLES
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace StatusEffects.NetCode.Entities
{
    public struct StatusEffectData
    {
        public FixedString64Bytes Id;
        public StatusEffectGroup Group;
        public FixedString64Bytes ComparableName;
        public float BaseValue;
        public UnityObjectRef<Sprite> Icon;
#if LOCALIZED
        public BlobString StatusEffectNameTable;
        public BlobString StatusEffectNameEntry;
        public BlobString DescriptionTable;
        public BlobString DescriptionEntry;
#else
        public BlobString StatusEffectName;
        public BlobString Description;
#endif
        public bool AllowEffectStacking;
        public NonStackingBehaviour NonStackingBehaviour;
        public int MaxStacks;
        public BlobArray<Effect> Effects;
        public BlobArray<Condition> Conditions;
        /// <summary>
        /// <see cref="IBufferElementData"/> index for a <see cref="ModulePrefabs"/> on the <see cref="StatusReferences"/> singleton.
        /// </summary>
        public int Modules;
    }
}
#endif