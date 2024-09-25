using System;

namespace StatusEffects
{
    [Serializable]
    public class Condition
    {
        public ConditionalConfigurable SearchableConfigurable;
        public StatusEffectData SearchableData;
        public ComparableName SearchableComparableName;
        public StatusEffectGroup SearchableGroup;
        public bool Exists = true;
        public bool Add = true;
        public ConditionalConfigurable ActionConfigurable;
        public StatusEffectData ActionData;
        public ComparableName ActionComparableName;
        public StatusEffectGroup ActionGroup;
        public ConditionalTiming Timing;
        public float Duration;
    }
}
