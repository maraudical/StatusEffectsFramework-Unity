using System;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusBool : StatusVariable
    {
        public StatusNameBool statusName;
        public bool baseValue;
#if UNITY_EDITOR
#pragma warning disable CS0414 // Remove unread private members
        [SerializeField] private bool _value;
#pragma warning restore CS0414 // Remove unread private members
#endif
        public bool value => GetValue();

        public StatusBool(bool baseValue, StatusNameBool statusName = null)
        {
            this.statusName = statusName;
            this.baseValue = baseValue;
        }

        protected bool GetValue()
        {
            if (monoBehaviour == null)
                return baseValue;

            bool value = baseValue;
            int priority = -1;

            bool effectValue;

            foreach (StatusEffect statusEffect in monoBehaviour.effects)
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
#if UNITY_EDITOR
        protected override void OnReferencesChanged()
        {
            _value = GetValue();
        }
#endif
    }
}
