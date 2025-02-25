#if ENTITIES
using Unity.Entities;
using Unity.Mathematics;
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

namespace StatusEffects.Entities
{
    public struct StatusEffectData
    {
        public Hash128 Id;
        public StatusEffectGroup Group;
        public Hash128 ComparableName;
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
        public BlobArray<Effect> Effects;
        public BlobArray<Condition> Conditions;
        /// <summary>
        /// <see cref="IBufferElementData"/> index for a <see cref="ModulePrefabs"/> on the <see cref="StatusReferences"/> singleton.
        /// </summary>
        public int ModulePrefabIndex;
    }
}
#endif