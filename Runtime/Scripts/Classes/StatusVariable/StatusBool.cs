using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusBool : StatusVariable
    {
        [SerializeField] public event Action<bool, bool> OnValueChanged;

        public StatusNameBool StatusName => m_StatusName;
        public bool BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; BaseValueChanged(); } }
        public bool Value => m_Value;

        [SerializeField] protected StatusNameBool m_StatusName;
        [SerializeField] protected bool m_BaseValue;
        [SerializeField] protected bool m_Value;

        public StatusBool(bool baseValue)
        {
            m_BaseValue = baseValue;
        }

        public StatusBool(bool baseValue, StatusNameBool statusName)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;
            m_Value = GetValue();
        }

        public static implicit operator bool(StatusBool statusBool) => statusBool.Value;

        public override void SetManager(IStatusManager instance)
        {
            base.SetManager(instance);

            bool previousValue = m_Value;
            m_Value = GetValue();
            OnValueChanged?.Invoke(previousValue, m_Value);
        }

        protected virtual void BaseValueChanged()
        {
            bool previousValue = m_Value;
            m_Value = GetValue();
            OnValueChanged?.Invoke(previousValue, m_Value);
        }

        protected override void InstanceUpdate(StatusEffect statusEffect)
        {
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Select(e => e.StatusName).Contains(m_StatusName))
            {
                bool previousValue = m_Value;
                m_Value = GetValue();
                OnValueChanged?.Invoke(previousValue, m_Value);
            }
        }

        protected bool GetValue()
        {
            if (Instance == null)
                return m_BaseValue;

            bool value = m_BaseValue;
            int priority = -1;

            bool effectValue;

            foreach (StatusEffect statusEffect in Instance.Effects)
            {
                foreach (Effect effect in statusEffect.Data.Effects)
                {
                    if (effect.StatusName != m_StatusName)
                        continue;

                    effectValue = effect.UseBaseValue ? Convert.ToBoolean(statusEffect.Data.BaseValue) : effect.BoolValue;

                    if (priority < effect.Priority)
                    {
                        priority = effect.Priority;
                        value = effectValue;
                    }
                }
            }

            return value;
        }
#if UNITY_EDITOR

        protected virtual async void BaseValueUpdate()
        {
            await Task.Yield();
            bool previousValue = m_Value;
            m_Value = GetValue();
            OnValueChanged?.Invoke(previousValue, m_Value);
        }
#endif
    }
}
