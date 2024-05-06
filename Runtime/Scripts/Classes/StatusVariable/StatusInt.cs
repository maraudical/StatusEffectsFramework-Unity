using System;
using System.Linq;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusInt : StatusVariable
    {
        public StatusNameInt statusName;
        [SerializeField] private int baseValue;
        public int value;
        
        public StatusInt(int baseValue)
        {
            this.baseValue = baseValue;
        }

        public StatusInt(int baseValue, StatusNameInt statusName)
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

        protected virtual int GetValue()
        {
            if (instance == null)
                return baseValue;

            int additiveValue = 0;
            int multiplicativeValue = 1;
            int postAdditiveValue = 0;

            int effectValue;

            foreach (StatusEffect statusEffect in instance.effects)
            {
                foreach (Effect effect in statusEffect.data.effects)
                {
                    if (effect.statusName != statusName)
                        continue;

                    effectValue = statusEffect.stack * (effect.useBaseValue ? (int)statusEffect.data.baseValue : effect.intValue);

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

        public static implicit operator int(StatusInt statusInt) => statusInt.value;

        public override void SetInstance(StatusManager instance)
        {
            base.SetInstance(instance);

            value = GetValue();
        }
    }
}
