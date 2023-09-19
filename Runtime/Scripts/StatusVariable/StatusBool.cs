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

        public bool GetValue()
        {
            if (statusEffectReferences == null)
                statusEffectReferences = new HashSet<StatusEffect>();

            bool value = baseValue;
            int priority = -1;

            foreach (StatusEffect statusEffect in statusEffectReferences)
            {
                foreach (Effect effect in statusEffect.data.effects)
                {
                    if (effect.statusName != statusName)
                        continue;

                    if (priority < effect.priority)
                    {
                        priority = effect.priority;
                        value = effect.boolValue;
                    }
                }
            }

            return value;
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
