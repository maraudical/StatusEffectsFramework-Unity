#if ENTITIES
using UnityEngine;

namespace StatusEffectsFramework.Entities
{
    public struct UnmanagedCondition
    {
        public ConditionalConfigurable SearchableConfigurable;
        public ushort SearchableData;
        public ushort SearchableComparableName;
        public StatusEffectGroup SearchableGroup;
        public bool Exists;
        public bool Add;
        public bool Scaled;
        public bool UseStacks;
        [Min(1)] public int Stacks;
        public ConditionalConfigurable ActionConfigurable;
        public ushort ActionData;
        public ushort ActionComparableName;
        public StatusEffectGroup ActionGroup;
        public ConditionalTiming Timing;
        [Min(0)] public float Duration;
    }
}
#endif