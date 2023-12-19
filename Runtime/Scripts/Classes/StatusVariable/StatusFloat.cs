using System;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusFloat : StatusVariable
    {
        public StatusNameFloat statusName;
        public float baseValue;
#if UNITY_EDITOR
#pragma warning disable CS0414 // Remove unread private members
        [SerializeField] private float _value;
#pragma warning restore CS0414 // Remove unread private members
#endif
        public float value => GetValue();

        public StatusFloat(float baseValue, StatusNameFloat statusName = null)
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

            float effectValue;

            foreach (StatusEffect statusEffect in monoBehaviour.effects)
            {
                foreach (Effect effect in statusEffect.data.effects)
                {
                    if (effect.statusName != statusName)
                        continue;

                    effectValue = effect.useBaseValue ? statusEffect.data.baseValue : effect.floatValue;

                    switch (effect.valueModifier)
                    {
                        case ValueModifier.Additive:
                            additiveValue += effectValue;
                            break;
                        case ValueModifier.Multiplicative:
                            multiplicativeValue += effectValue;
                            break;
                        case ValueModifier.PostAdditive:
                            postAdditiveValue += effectValue;
                            break;

                    }
                }
            }
            
            return (baseValue + additiveValue) * multiplicativeValue + postAdditiveValue;
        }

        public static implicit operator float(StatusFloat statusFloat) => statusFloat.value;
#if UNITY_EDITOR
        protected override void OnReferencesChanged()
        {
            _value = GetValue();
        }
#endif
    }
}
