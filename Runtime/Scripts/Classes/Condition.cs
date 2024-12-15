using System;
using UnityEngine;
#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
using UnityEngine.AddressableAssets;
#endif

namespace StatusEffects
{
    [Serializable]
    public class Condition
    {
        public ConditionalConfigurable SearchableConfigurable;
#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
        public AssetReferenceT<StatusEffectData> SearchableDataReference;
#endif
        public StatusEffectData SearchableData;
        public ComparableName SearchableComparableName;
        public StatusEffectGroup SearchableGroup;
        public bool Exists = true;
        public bool Add = true;
        public bool Scaled = true;
        public bool UseStacks;
        [Min(1)] public int Stacks = 1;
        public ConditionalConfigurable ActionConfigurable;
#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
        public AssetReferenceT<StatusEffectData> ActionDataReference;
#endif
        public StatusEffectData ActionData;
        public ComparableName ActionComparableName;
        public StatusEffectGroup ActionGroup;
        public ConditionalTiming Timing;
        [Min(0)] public float Duration;
    }
}
