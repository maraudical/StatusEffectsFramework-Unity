using StatusEffects.Inspector;
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
        [Space]
#if UNITY_EDITOR
#pragma warning disable CS0414
        private string warningDefault = $"Do not recursively add status datas! " +
                                        $"Avoid adding a status data to itself! " +
                                        $"Make sure there aren't two that add eachother!";
        [InfoBox(hexCode = "#FF0000", style = FontStyle.Bold), SerializeField] 
        private string warning;
        private void OnValidate()
        {
            warning = default;

            foreach (Condition condition in conditions)
            {
                if (condition.add && condition.configurable == this)
                    warning = warningDefault;
            }
        }
#pragma warning restore CS0414
#endif
        public List<Condition> conditions;
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

    [Serializable]
    public class Condition
    {
        public StatusEffectData searchable;
        public bool exists = true;
        public bool add = true;
        public StatusEffectData configurable;
        public bool timed;
        public float duration;
    }
}
