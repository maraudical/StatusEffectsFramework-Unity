using System;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class Effect
    {
        public StatusName statusName;
        public ValueModifier valueModifier;
        public bool useBaseValue;
        public float floatValue;
        public int intValue;
        public bool boolValue;
        [Min(0)] public int priority;
    }
}
