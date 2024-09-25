using System;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class Effect
    {
        public StatusName StatusName;
        public ValueModifier ValueModifier;
        public bool UseBaseValue;
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;
        [Min(0)] public int Priority;
    }
}
