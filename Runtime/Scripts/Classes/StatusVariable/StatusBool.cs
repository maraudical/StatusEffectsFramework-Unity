using StatusEffects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusBool : StatusVariable
    {
        public bool baseValue;
#if UNITY_EDITOR
#pragma warning disable CS0414 // Remove unread private members
        [SerializeField] private bool _value;
        [SerializeField] private bool _initialized;
#pragma warning restore CS0414 // Remove unread private members
#endif
        public bool value => GetValue();

        public StatusBool(string statusName, bool baseValue)
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
                    if (effect.statusName != statusName || effect.valueType != ValueType.Bool)
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

        protected override void OnReferencesChanged()
        {
#if UNITY_EDITOR
            _value = GetValue();
            _initialized = true;
#endif
        }
    }
}
