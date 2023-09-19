using System;
using System.Collections.Generic;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusFloat : StatusVariable
    {
        public float baseValue;
#if UNITY_EDITOR
#pragma warning disable CS0414 // Remove unread private members
        [SerializeField] private float _value;
        [SerializeField] private bool _initialized;
#pragma warning restore CS0414 // Remove unread private members
#endif
        public float value => GetValue();

        public StatusFloat(string statusName, float baseValue)
        {
            this.statusName = statusName;
            this.baseValue = baseValue;
        }

        protected float GetValue()
        {
            if (monoBehaviour == null)
                return baseValue;

            float additiveValue = 0;
            float multiplicativeValue = 1;
            float postAdditiveValue = 0;

            foreach (StatusEffect statusEffect in monoBehaviour.effects)
            {
                foreach (Effect effect in statusEffect.data.effects)
                {
                    if (effect.statusName != statusName)
                        continue;

                    switch (effect.valueModifier)
                    {
                        case ValueModifier.Additive:
                            additiveValue += effect.floatValue;
                            break;
                        case ValueModifier.Multiplicative:
                            multiplicativeValue += effect.floatValue;
                            break;
                        case ValueModifier.PostAdditive:
                            postAdditiveValue += effect.floatValue;
                            break;

                    }
                }
            }
            
            return (baseValue + additiveValue) * multiplicativeValue + postAdditiveValue;
        }

        protected override void OnReferencesChanged()
        {
#if UNITY_EDITOR
            _value = GetValue();
            _initialized = true;
#endif
        }
    }
}
