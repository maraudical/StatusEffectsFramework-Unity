#if ENTITIES && ADDRESSABLES
using Unity.Collections;
using UnityEngine;

namespace StatusEffects.NetCode.Entities
{
    public struct Condition
    {
        public ConditionalConfigurable SearchableConfigurable;
        public FixedString64Bytes SearchableData;
        public FixedString64Bytes SearchableComparableName;
        public StatusEffectGroup SearchableGroup;
        public bool Exists;
        public bool Add;
        public bool Scaled;
        public bool UseStacks;
        [Min(1)] public int Stacks;
        public ConditionalConfigurable ActionConfigurable;
        public FixedString64Bytes ActionData;
        public FixedString64Bytes ActionComparableName;
        public StatusEffectGroup ActionGroup;
        public ConditionalTiming Timing;
        [Min(0)] public float Duration;
    }
}
#endif