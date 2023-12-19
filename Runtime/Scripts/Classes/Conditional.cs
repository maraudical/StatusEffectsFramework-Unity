using System;

namespace StatusEffects
{
    [Serializable]
    public class Condition
    {
        public StatusEffectData searchable;
        public bool exists = true;
        public bool add = true;
        public StatusEffectData configurable;
        public Timing timing;
        public float duration;
    }
}
