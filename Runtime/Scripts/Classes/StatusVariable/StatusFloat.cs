using System;
using System.Linq;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusFloat : StatusVariable
    {
        public StatusNameFloat statusName;
        [SerializeField] private float baseValue;
        public float value;
        
        public StatusFloat(float baseValue)
        {
            this.baseValue = baseValue;
        }

        public StatusFloat(float baseValue, StatusNameFloat statusName)
        {
            this.statusName = statusName;
            this.baseValue = baseValue;
            value = GetValue();
        }

        protected override void InstanceUpdate(StatusEffect statusEffect)
        {
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.data.effects.Select(e => e.statusName).Contains(statusName))
                value = GetValue();
        }

        protected float GetValue()
        {
            if (instance == null)
                return baseValue;

            float additiveValue = 0;
            float multiplicativeValue = 1;
            float postAdditiveValue = 0;

            float effectValue;

            foreach (StatusEffect statusEffect in instance.effects)
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

        public override void SetInstance(StatusManager instance)
        {
            base.SetInstance(instance);

            value = GetValue();
        }
    }
}
