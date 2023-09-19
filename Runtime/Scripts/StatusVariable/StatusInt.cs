using StatusEffects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusInt : StatusVariable
    {
        public int baseValue;
#if UNITY_EDITOR
#pragma warning disable CS0414 // Remove unread private members
        [SerializeField] private int _value;
        [SerializeField] private bool _initialized;
#pragma warning restore CS0414 // Remove unread private members
#endif
        public int value => GetValue();

        public StatusInt(string statusName, int baseValue)
        {
            this.statusName = statusName;
            this.baseValue = baseValue;
        }

        protected virtual int GetValue()
        {
            if (monoBehaviour == null)
                return baseValue;

            int additiveValue = 0;
            int multiplicativeValue = 1;
            int postAdditiveValue = 0;

            foreach (StatusEffect statusEffect in monoBehaviour.effects)
            {
                foreach (Effect effect in statusEffect.data.effects)
                {
                    if (effect.statusName != statusName)
                        continue;

                    switch (effect.valueModifier)
                    {
                        case ValueModifier.Additive:
                            additiveValue += effect.intValue;
                            break;
                        case ValueModifier.Multiplicative:
                            multiplicativeValue += effect.intValue;
                            break;
                        case ValueModifier.PostAdditive:
                            postAdditiveValue += effect.intValue;
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
