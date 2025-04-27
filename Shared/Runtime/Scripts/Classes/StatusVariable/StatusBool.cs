using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusBool : StatusVariable
    {
        public event Action<bool, bool> OnValueChanged;
        public event Action<bool, bool> OnBaseValueChanged;

        public StatusNameBool StatusName => m_StatusName;
        public bool BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; BaseValueChanged(); } }
        public bool Value => Instance != null ? m_Value : m_BaseValue;

        [SerializeField] protected StatusNameBool m_StatusName;
        [SerializeField] protected bool m_BaseValue;
        protected bool m_PreviousBaseValue;
        [SerializeField] protected bool m_Value;
        protected bool m_PreviousValue;

        public StatusBool(bool baseValue)
        {
            m_BaseValue = baseValue;

            if (Instance != null)
            {
                UpdateBaseValue();
                m_PreviousBaseValue = baseValue;
            }
        }

        public StatusBool(bool baseValue, StatusNameBool statusName)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;

            if (Instance != null)
            {
                UpdateBaseValue();
                m_PreviousBaseValue = baseValue;
            }
            UpdateValue();
        }

        public static implicit operator bool(StatusBool statusBool) => statusBool.Value;

        public override void SetManager(IStatusManager instance)
        {
            base.SetManager(instance);
            m_PreviousBaseValue = m_BaseValue;
            m_Value = m_BaseValue;
            UpdateValue();
        }

        protected virtual void BaseValueChanged()
        {
            UpdateBaseValue();
            m_PreviousBaseValue = m_BaseValue;
            UpdateValue();
        }

        protected override void InstanceUpdate(StatusEffect statusEffect)
        {
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Select(e => e.StatusName).Contains(m_StatusName))
                UpdateValue();
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

        protected void UpdateValue()
        {
            m_PreviousValue = m_Value;
            m_Value = GetValue();
            if (m_Value != m_PreviousValue)
                OnValueChanged?.Invoke(m_PreviousValue, m_Value);
        }

        protected void UpdateBaseValue()
        {
            if (m_BaseValue != m_PreviousBaseValue)
                OnBaseValueChanged?.Invoke(m_PreviousBaseValue, m_BaseValue);
        }
#if UNITY_EDITOR

        protected virtual async void BaseValueUpdate()
        {
            await Task.Yield();

            UpdateBaseValue();
            m_PreviousBaseValue = m_BaseValue;
            UpdateValue();
        }
#endif
    }
}
