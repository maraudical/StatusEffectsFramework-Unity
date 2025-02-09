#if ENTITIES
using UnityEngine;
using Hash128 = Unity.Entities.Hash128;

namespace StatusEffects.Entities
{
    public struct Condition
    {
        public ConditionalConfigurable SearchableConfigurable;
        public Hash128 SearchableData;
        public Hash128 SearchableComparableName;
        public StatusEffectGroup SearchableGroup;
        public bool Exists;
        public bool Add;
        public bool Scaled;
        public bool UseStacks;
        [Min(1)] public int Stacks;
        public ConditionalConfigurable ActionConfigurable;
        public Hash128 ActionData;
        public Hash128 ActionComparableName;
        public StatusEffectGroup ActionGroup;
        public ConditionalTiming Timing;
        [Min(0)] public float Duration;
    }
}
#endif