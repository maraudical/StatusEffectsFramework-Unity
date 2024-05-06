using System;
using System.Linq;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusBool : StatusVariable
    {
        public StatusNameBool statusName;
        [SerializeField] private bool baseValue;
        public bool value;
        
        public StatusBool(bool baseValue)
        {
            this.baseValue = baseValue;
        }

        public StatusBool(bool baseValue, StatusNameBool statusName)
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

        protected bool GetValue()
        {
            if (instance == null)
                return baseValue;

            bool value = baseValue;
            int priority = -1;

            bool effectValue;

            foreach (StatusEffect statusEffect in instance.effects)
            {
                foreach (Effect effect in statusEffect.data.effects)
                {
                    if (effect.statusName != statusName)
                        continue;

                    effectValue = effect.useBaseValue ? Convert.ToBoolean(statusEffect.data.baseValue) : effect.boolValue;

                    if (priority < effect.priority)
                    {
                        priority = effect.priority;
                        value = effectValue;
                    }
                }
            }

            return value;
        }

        public static implicit operator bool(StatusBool statusBool) => statusBool.value;

        public override void SetInstance(StatusManager instance)
        {
            base.SetInstance(instance);

            value = GetValue();
        }
    }
}
