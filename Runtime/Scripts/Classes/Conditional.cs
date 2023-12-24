using System;

namespace StatusEffects
{
    [Serializable]
    public class Condition
    {
        public StatusEffectData searchable;
        public bool exists = true;
        public bool add = true;
        public ConditionalConfigurable configurable;
        public StatusEffectData data;
        public ComparableName comparableName;
        public StatusEffectGroup group;
        public ConditionalTiming timing;
        public float duration;
    }
}
