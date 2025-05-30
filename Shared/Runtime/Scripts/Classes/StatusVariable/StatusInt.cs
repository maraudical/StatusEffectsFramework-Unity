using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusInt : StatusVariable
    {
        public event Action<int, int> OnValueChanged;
        public event Action<int, int> OnBaseValueChanged;
        public event Action<bool, bool> OnSignProtectedChanged;

        public StatusNameInt StatusName => m_StatusName;
        public int BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; BaseValueChanged(); } }
        public bool SignProtected { get { return m_SignProtected; } set { m_SignProtected = value; SignProtectedChanged(); } }
        public int Value => Instance != null ? m_Value : m_BaseValue;

        [SerializeField] protected StatusNameInt m_StatusName;
        [SerializeField] protected int m_BaseValue;
        protected int m_PreviousBaseValue;
        [SerializeField] protected bool m_SignProtected;
        protected bool m_PreviousSignProtected;
        [SerializeField] protected int m_Value;
        protected int m_PreviousValue;

        public StatusInt(int baseValue, bool signProtected = true)
        {
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;

            if (Instance != null)
            {
                UpdateBaseValue();
                m_PreviousBaseValue = baseValue;
                UpdateSignProtected();
                m_PreviousSignProtected = signProtected;
            }
        }

        public StatusInt(int baseValue, StatusNameInt statusName, bool signProtected = true)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;

            if (Instance != null)
            {
                UpdateBaseValue();
                m_PreviousBaseValue = baseValue;
                UpdateSignProtected();
                m_PreviousSignProtected = signProtected;
            }
            UpdateValue();
        }

        public static implicit operator int(StatusInt statusInt) => statusInt.Value;

        public override void SetManager(IStatusManager instance)
        {
            base.SetManager(instance);
            m_PreviousBaseValue = m_BaseValue;
            m_PreviousSignProtected = m_SignProtected;
            m_Value = m_BaseValue;
            UpdateValue();
        }

        protected virtual void BaseValueChanged()
        {
            UpdateBaseValue();
            m_PreviousBaseValue = m_BaseValue;
            UpdateValue();
        }

        protected virtual void SignProtectedChanged()
        {
            UpdateSignProtected();
            m_PreviousSignProtected = m_SignProtected;
            UpdateValue();
        }

        protected override void InstanceUpdate(StatusEffect statusEffect)
        {
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Select(e => e.StatusName).Contains(m_StatusName))
                UpdateValue();
        }

        protected virtual int GetValue()
        {
            if (Instance == null)
                return m_BaseValue;

            bool positive = Mathf.Sign(m_BaseValue) > 0;
            int additiveValue = 0;
            int multiplicativeValue = 1;
            int postAdditiveValue = 0;
            int minimumPriority = -1;
            int minimumValue = int.MinValue;
            int maximumPriority = -1;
            int maximumValue = int.MaxValue;
            int overwritePriority = -1;
            int overwriteValue = 0;

            int effectValue;

            foreach (StatusEffect statusEffect in Instance.Effects)
            {
                foreach (Effect effect in statusEffect.Data.Effects)
                {
                    if (effect.StatusName != m_StatusName)
                        continue;

                    effectValue = statusEffect.Stacks * (effect.UseBaseValue ? (int)statusEffect.Data.BaseValue : effect.IntValue);

                    switch (effect.ValueModifier)
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
                        case ValueModifier.Minimum:
                            if (minimumPriority < effect.Priority)
                            {
                                minimumPriority = effect.Priority;
                                minimumValue = effectValue;
                            }
                            else if (minimumPriority == effect.Priority)
                                minimumValue = Mathf.Max(minimumValue, effectValue);
                            break;
                        case ValueModifier.Maximum:
                            if (maximumPriority < effect.Priority)
                            {
                                maximumPriority = effect.Priority;
                                maximumValue = effectValue;
                            }
                            else if (maximumPriority == effect.Priority)
                                maximumValue = Mathf.Min(maximumValue, effectValue);
                            break;
                        case ValueModifier.Overwrite:
                            if (overwritePriority <= effect.Priority)
                            {
                                overwritePriority = effect.Priority;
                                overwriteValue = effectValue;
                            }
                            break;
                    }
                }
            }

            if (overwritePriority >= 0)
                return Mathf.Clamp(overwriteValue, overwritePriority <= minimumPriority ? minimumValue : int.MinValue, overwritePriority <= maximumPriority ? maximumValue : int.MaxValue);
            else if (m_SignProtected)
                return Mathf.Clamp((m_BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue, Mathf.Max(positive ? 0 : int.MinValue, minimumValue), Mathf.Min(positive ? int.MaxValue : 0, maximumValue));
            else
                return Mathf.Clamp((m_BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue, minimumValue, maximumValue);
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

        protected void UpdateSignProtected()
        {
            if (m_SignProtected != m_PreviousSignProtected)
                OnSignProtectedChanged?.Invoke(m_PreviousSignProtected, m_SignProtected);
        }
#if UNITY_EDITOR

        protected virtual async void BaseValueUpdate()
        {
            await Task.Yield();

            UpdateBaseValue();
            m_PreviousBaseValue = m_BaseValue;
            UpdateValue();
        }

        protected virtual async void SignProtectedUpdate()
        {
            await Task.Yield();

            UpdateSignProtected();
            m_PreviousSignProtected = m_SignProtected;
            UpdateValue();
        }
#endif
    }
}
