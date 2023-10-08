using System;
using System.Collections.Generic;
using UnityEngine;

namespace StatusEffects
{
    [CreateAssetMenu(fileName = "New Status Effect Data", menuName = "Status Effect Data", order = 1)]
    public class StatusEffectData : ScriptableObject
    {
        public StatusEffectGroup group;
        public new string name;
        [Space]
        public float baseValue;
        [Space]
        [Tooltip("If you want effects to be applied multiple times on the same MonoBehaviour enable this.")]
        public bool allowEffectStacking;
        public NonStackingBehaviour nonStackingBehaviour;
        [Space]
        public List<Effect> effects;
        [Space]
        public CustomEffect customEffect;
    }

    [Serializable]
    public class Effect
    {
        [StatusString] public string statusName;
        public ValueType valueType;
        public ValueModifier valueModifier;
        public bool useBaseValue;
        public float floatValue;
        public int intValue;
        public bool boolValue;
        [Min (0)] public int priority;
    }
}
