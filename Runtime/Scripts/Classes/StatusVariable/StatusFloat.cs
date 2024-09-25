using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusFloat : StatusVariable
    {
        [SerializeField] public event Action<float, float> OnValueChanged;

        public StatusNameFloat StatusName => m_StatusName;
        public float BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; BaseValueChanged(); } }
        public bool SignProtected { get { return m_SignProtected; } set { m_SignProtected = value; SignProtectedChanged(); } }
        public float Value => m_Value;

        [SerializeField] protected StatusNameFloat m_StatusName;
        [SerializeField] protected float m_BaseValue;
        [SerializeField] protected bool m_SignProtected;
        [SerializeField] protected float m_Value;

        public StatusFloat(float baseValue, bool signProtected = true)
        {
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;
        }

        public StatusFloat(float baseValue, StatusNameFloat statusName, bool signProtected = true)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;
            m_Value = GetValue();
        }

        public static implicit operator float(StatusFloat statusFloat) => statusFloat.Value;

        public override void SetManager(IStatusManager instance)
        {
            base.SetManager(instance);

            float previousValue = m_Value;
            m_Value = GetValue();
            OnValueChanged?.Invoke(previousValue, m_Value);
        }

        protected virtual void BaseValueChanged()
        {
            float previousValue = m_Value;
            m_Value = GetValue();
            OnValueChanged?.Invoke(previousValue, m_Value);
        }

        protected virtual void SignProtectedChanged()
        {
            float previousValue = m_Value;
            m_Value = GetValue();
            OnValueChanged?.Invoke(previousValue, m_Value);
        }

        protected override void InstanceUpdate(StatusEffect statusEffect)
        {
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Select(e => e.StatusName).Contains(m_StatusName))
            {
                float previousValue = m_Value;
                m_Value = GetValue();
                OnValueChanged?.Invoke(previousValue, m_Value);
            }
        }

        protected float GetValue()
        {
            if (Instance == null)
                return m_BaseValue;

            bool positive = Mathf.Sign(m_BaseValue) >= 0;
            float additiveValue = 0;
            float multiplicativeValue = 1;
            float postAdditiveValue = 0;

            float effectValue;

            foreach (StatusEffect statusEffect in Instance.Effects)
            {
                foreach (Effect effect in statusEffect.Data.Effects)
                {
                    if (effect.StatusName != m_StatusName)
                        continue;

                    effectValue = statusEffect.Stack * (effect.UseBaseValue ? statusEffect.Data.BaseValue : effect.FloatValue);
                    
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

                    }
                }
            }
            
            if (m_SignProtected)
                return Mathf.Clamp((m_BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue, positive ? 0 : float.NegativeInfinity, positive ? float.PositiveInfinity : 0);
            else
                return (m_BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue;
        }
#if UNITY_EDITOR

        protected virtual async void BaseValueUpdate()
        {
            await Task.Yield();
            float previousValue = m_Value;
            m_Value = GetValue();
            OnValueChanged?.Invoke(previousValue, m_Value);
        }

        protected virtual async void SignProtectedUpdate()
        {
            await Task.Yield();
            float previousValue = m_Value;
            m_Value = GetValue();
            OnValueChanged?.Invoke(previousValue, m_Value);
        }
#endif
    }
}
