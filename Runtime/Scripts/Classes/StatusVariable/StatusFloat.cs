using System;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusFloat : StatusVariable
    {
        public StatusNameFloat statusName;
        public float baseValue;
        public float value => GetValue();

#if UNITY_EDITOR
#pragma warning disable CS0414 // Remove unread private members
        [SerializeField] private float _value;
#pragma warning restore CS0414 // Remove unread private members

#endif
        public StatusFloat(float baseValue)
        {
            this.baseValue = baseValue;
        }

        public StatusFloat(float baseValue, StatusNameFloat statusName, MonoBehaviour monoBehaviour)
        {
            this.statusName = statusName;
            this.baseValue = baseValue;
            this.monoBehaviour = monoBehaviour;
        }

        protected float GetValue()
        {
#if UNITY_EDITOR
            if (monoBehaviour == null)
                return baseValue;
#endif
            if (iStatus == null)
                iStatus = monoBehaviour as IStatus;

            float additiveValue = 0;
            float multiplicativeValue = 1;
            float postAdditiveValue = 0;

            float effectValue;

            foreach (StatusEffect statusEffect in iStatus.effects)
            {
                foreach (Effect effect in statusEffect.data.effects)
                {
                    if (effect.statusName != statusName)
                        continue;

                    effectValue = statusEffect.stack * (effect.useBaseValue ? statusEffect.data.baseValue : effect.floatValue);

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
        public override void OnStatusEffect(MonoBehaviour monoBehaviour)
        {
            _value = GetValue();
            this.monoBehaviour = monoBehaviour;
        }
#endif
    }
}
