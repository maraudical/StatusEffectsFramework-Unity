using System;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusBool : StatusVariable
    {
        public StatusNameBool statusName;
        public bool baseValue;
        public bool value => GetValue();

#if UNITY_EDITOR
#pragma warning disable CS0414 // Remove unread private members
        [SerializeField] private bool _value;
#pragma warning restore CS0414 // Remove unread private members

#endif
        public StatusBool(bool baseValue)
        {
            this.baseValue = baseValue;
        }

        public StatusBool(bool baseValue, StatusNameBool statusName, MonoBehaviour monoBehaviour)
        {
            this.statusName = statusName;
            this.baseValue = baseValue;
            this.monoBehaviour = monoBehaviour;
        }

        protected bool GetValue()
        {
#if UNITY_EDITOR
            if (monoBehaviour == null)
                return baseValue;
#endif
            if (iStatus == null)
                iStatus = monoBehaviour as IStatus;

            bool value = baseValue;
            int priority = -1;

            bool effectValue;

            foreach (StatusEffect statusEffect in iStatus.effects)
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
        public override void OnStatusEffect(MonoBehaviour monoBehaviour)
        {
            _value = GetValue();
            this.monoBehaviour = monoBehaviour;
        }
#endif
    }
}
