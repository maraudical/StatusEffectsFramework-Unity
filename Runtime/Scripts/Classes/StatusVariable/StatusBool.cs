using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusBool : StatusVariable
    {
        public StatusNameBool statusName;
        [SerializeField] private bool _baseValue;
        public bool value;
        
        public StatusBool(bool baseValue)
        {
            _baseValue = baseValue;
        }

        public StatusBool(bool baseValue, StatusNameBool statusName)
        {
            this.statusName = statusName;
            _baseValue = baseValue;
            value = GetValue();
        }

        public void ChangeBaseValue(bool value)
        {
            _baseValue = value;
            this.value = GetValue();
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
                return _baseValue;

            bool value = _baseValue;
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

        public override void SetManager(StatusManager instance)
        {
            base.SetManager(instance);

            value = GetValue();
        }
#if UNITY_EDITOR

        private async void BaseValueUpdate()
        {
            await Task.Yield();
            value = GetValue();
        }
#endif
    }
}
