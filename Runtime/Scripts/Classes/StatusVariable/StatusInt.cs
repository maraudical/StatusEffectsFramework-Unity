using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusInt : StatusVariable
    {
        [SerializeField] public event Action<int, int> OnValueChanged;

        public StatusNameInt StatusName => m_StatusName;
        public int BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; BaseValueChanged(); } }
        public bool SignProtected { get { return m_SignProtected; } set { m_SignProtected = value; SignProtectedChanged(); } }
        public int Value => m_Value;

        [SerializeField] protected StatusNameInt m_StatusName;
        [SerializeField] protected int m_BaseValue;
        [SerializeField] protected bool m_SignProtected;
        [SerializeField] protected int m_Value;

        public StatusInt(int baseValue, bool signProtected = true)
        {
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;
        }

        public StatusInt(int baseValue, StatusNameInt statusName, bool signProtected = true)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;
            m_Value = GetValue();
            
        }

        public static implicit operator int(StatusInt statusInt) => statusInt.Value;

        public override void SetManager(IStatusManager instance)
        {
            base.SetManager(instance);

            int previousValue = m_Value;
            m_Value = GetValue();
            OnValueChanged?.Invoke(previousValue, m_Value);
        }

        protected virtual void BaseValueChanged()
        {
            int previousValue = m_Value;
            m_Value = GetValue();
            OnValueChanged?.Invoke(previousValue, m_Value);
        }

        protected virtual void SignProtectedChanged()
        {
            int previousValue = m_Value;
            m_Value = GetValue();
            OnValueChanged?.Invoke(previousValue, m_Value);
        }

        protected override void InstanceUpdate(StatusEffect statusEffect)
        {
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Select(e => e.StatusName).Contains(m_StatusName))
            {
                int previousValue = m_Value;
                m_Value = GetValue();
                OnValueChanged?.Invoke(previousValue, m_Value);
            }
        }

        protected virtual int GetValue()
        {
            if (Instance == null)
                return m_BaseValue;

            bool positive = Mathf.Sign(m_BaseValue) > 0;
            int additiveValue = 0;
            int multiplicativeValue = 1;
            int postAdditiveValue = 0;

            int effectValue;

            foreach (StatusEffect statusEffect in Instance.Effects)
            {
                foreach (Effect effect in statusEffect.Data.Effects)
                {
                    if (effect.StatusName != m_StatusName)
                        continue;

                    effectValue = statusEffect.Stack * (effect.UseBaseValue ? (int)statusEffect.Data.BaseValue : effect.IntValue);

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
                return Mathf.Clamp((m_BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue, positive ? 0 : int.MinValue, positive ? int.MaxValue : 0);
            else
                return (m_BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue;
        }
#if UNITY_EDITOR

        protected virtual async void BaseValueUpdate()
        {
            await Task.Yield();
            int previousValue = m_Value;
            m_Value = GetValue();
            OnValueChanged?.Invoke(previousValue, m_Value);
        }

        protected virtual async void SignProtectedUpdate()
        {
            await Task.Yield();
            int previousValue = m_Value;
            m_Value = GetValue();
            OnValueChanged?.Invoke(previousValue, m_Value);
        }
#endif
    }
}
