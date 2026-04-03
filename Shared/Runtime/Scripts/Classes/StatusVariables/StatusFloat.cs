using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;

namespace StatusEffectFramework
{
    [Serializable]
    public class StatusFloat : StatusVariable
    {
        public event Action<float, float> OnValueChanged;
        public event Action<float, float> OnPreEvaluationValueChanged;
        public event Action<float, float> OnBaseValueChanged;
        public event Action<bool, bool> OnSignProtectedChanged;

        public StatusNameFloat StatusName => m_StatusName;
        public float BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; BaseValueChanged(); } }
        public bool SignProtected { get { return m_SignProtected; } set { m_SignProtected = value; SignProtectedChanged(); } }
        public float Value => Manager != null ? PostEvaluationValue : m_BaseValue;
        public float PreEvaluationValue { get; protected set; }
        public float PostEvaluationValue { get; protected set; }

        [SerializeField] protected StatusNameFloat m_StatusName;
        [SerializeField] protected float m_BaseValue;
        [SerializeField] protected bool m_SignProtected;

        protected float m_PreviousBaseValue;
        protected bool m_PreviousSignProtected;
        protected float m_PreviousPreEvaluationValue;
        protected float m_PreviousPostEvaluationValue;

        public StatusFloat(float baseValue, bool signProtected = true)
        {
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;

            if (Manager != null)
            {
                UpdateBaseValue();
                m_PreviousBaseValue = baseValue;
                UpdateSignProtected();
                m_PreviousSignProtected = signProtected;
            }
        }

        public StatusFloat(float baseValue, StatusNameFloat statusName, bool signProtected = true)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;

            if (Manager != null)
            {
                UpdateBaseValue();
                m_PreviousBaseValue = baseValue;
                UpdateSignProtected();
                m_PreviousSignProtected = signProtected;
            }

            PreEvaluationValue = m_BaseValue;
            PostEvaluationValue = 0;
        }

        public static implicit operator float(StatusFloat statusFloat) => statusFloat.Value;

        public override void SetManager(IStatusManager instance)
        {
            if (Manager != null)
                foreach (StatusEffect statusEffect in Manager.Effects)
                    foreach (var dynamicFloat in statusEffect.DynamicFloats)
                        if (dynamicFloat.StatusName == m_StatusName)
                        {
                            if (dynamicFloat.PostEvaluate)
                                dynamicFloat.OnValueChanged -= UpdatePostEvaluationValue;
                            else
                                dynamicFloat.OnValueChanged -= UpdatePreEvaluationValue;
                        }

            base.SetManager(instance);
            m_PreviousBaseValue = m_BaseValue;
            m_PreviousSignProtected = m_SignProtected;
            m_PreviousPreEvaluationValue = m_BaseValue;
            m_PreviousPostEvaluationValue = 0;
            foreach (StatusEffect statusEffect in Manager.Effects)
                foreach (var dynamicFloat in statusEffect.DynamicFloats)
                    if (dynamicFloat.StatusName == m_StatusName)
                    {
                        if (dynamicFloat.PostEvaluate)
                            dynamicFloat.OnValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicFloat.OnValueChanged += UpdatePreEvaluationValue;
                    }

            UpdatePreEvaluationValue();
            UpdatePostEvaluationValue();
        }

        protected virtual void BaseValueChanged()
        {
            UpdateBaseValue();
            m_PreviousBaseValue = m_BaseValue;
            UpdatePreEvaluationValue();
        }

        protected virtual void SignProtectedChanged()
        {
            UpdateSignProtected();
            m_PreviousSignProtected = m_SignProtected;
            UpdatePreEvaluationValue();
            UpdatePostEvaluationValue();
        }

        protected override void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int previousStacks, int currentStacks)
        {
            if (action is StatusEffectAction.AddedStatusEffect)
                foreach (var dynamicFloat in statusEffect.DynamicFloats)
                    if (dynamicFloat.StatusName == m_StatusName)
                        if (dynamicFloat.PostEvaluate)
                            dynamicFloat.OnValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicFloat.OnValueChanged += UpdatePreEvaluationValue;
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Any(effect => effect.StatusName == m_StatusName))
                UpdatePreEvaluationValue();
        }

        protected float GetPreEvaluationValue()
        {
            if (Manager == null)
                return m_BaseValue;

            var statusFloatValue = new StatusFloatValue(m_BaseValue, m_SignProtected);

            float effectValue = default;

            foreach (StatusEffect statusEffect in Manager.Effects)
            {
                foreach (Effect effect in statusEffect.Data.Effects)
                {
                    if (effect.StatusName != m_StatusName)
                        continue;

                    switch (effect.ValueType)
                    {
                        case ValueType.ExplicitValue:
                            effectValue = statusEffect.Stacks * effect.FloatValue;
                            break;
                        case ValueType.BaseValue:
                            effectValue = statusEffect.Stacks * statusEffect.Data.BaseValue;
                            break;
                        case ValueType.DynamicValue:
                            continue;
                    }

                    statusFloatValue.ApplyEffect(effect.ValueModifier, effectValue, effect.Priority);
                }

                foreach (var dynamicFloat in statusEffect.DynamicFloats)
                    if (!dynamicFloat.PostEvaluate)
                        statusFloatValue.ApplyEffect(dynamicFloat.ValueModifier, dynamicFloat.Value, dynamicFloat.Priority);
            }

            return statusFloatValue.GetValue();
        }

        protected float GetPostEvaluationValue()
        {
            if (Manager == null)
                return 0;

            var statusFloatValue = new StatusFloatValue(m_BaseValue, m_SignProtected);
            
            foreach (StatusEffect statusEffect in Manager.Effects)
                foreach (var dynamicFloat in statusEffect.DynamicFloats)
                    if (dynamicFloat.PostEvaluate)
                        statusFloatValue.ApplyEffect(dynamicFloat.ValueModifier, dynamicFloat.Value, dynamicFloat.Priority);

            return statusFloatValue.GetValue();
        }

        protected void UpdatePreEvaluationValue()
        {
            m_PreviousPreEvaluationValue = PreEvaluationValue;
            PreEvaluationValue = GetPreEvaluationValue();
            if (PreEvaluationValue != m_PreviousPreEvaluationValue)
                OnValueChanged?.Invoke(m_PreviousPreEvaluationValue, PreEvaluationValue);
        }

        protected void UpdatePostEvaluationValue()
        {
            m_PreviousPostEvaluationValue = PostEvaluationValue;
            PostEvaluationValue = GetPostEvaluationValue();
            if (PostEvaluationValue != m_PreviousPostEvaluationValue)
                OnValueChanged?.Invoke(m_PreviousPostEvaluationValue, PostEvaluationValue);
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
            UpdatePreEvaluationValue();
        }

        protected virtual async void SignProtectedUpdate()
        {
            await Task.Yield();

            UpdateSignProtected();
            m_PreviousSignProtected = m_SignProtected;
            UpdatePreEvaluationValue();
            UpdatePostEvaluationValue();
        }
#endif

        struct StatusFloatValue
        {
            public float BaseValue;

            public float AdditiveValue;
            public float MultiplicativeValue;
            public float PostAdditiveValue;
            public int MinimumPriority;
            public float MinimumValue;
            public int MaximumPriority;
            public float MaximumValue;
            public int OverwritePriority;
            public float OverwriteValue;

            public StatusFloatValue(float baseValue, bool signProtected)
            {
                BaseValue = baseValue;
                AdditiveValue = 0;
                MultiplicativeValue = 1;
                PostAdditiveValue = 0;
                MinimumPriority = -1;
                MinimumValue = float.NegativeInfinity;
                MaximumPriority = -1;
                MaximumValue = float.PositiveInfinity;
                OverwritePriority = -1;
                OverwriteValue = 0;
                if (signProtected)
                {
                    if (Mathf.Sign(baseValue) >= 0)
                        MinimumValue = 0;
                    else
                        MaximumValue = 0;
                }  
            }

            public void ApplyEffect(ValueModifier valueModifier, float value, int priority)
            {
                switch (valueModifier)
                {
                    case ValueModifier.Additive:
                        AdditiveValue += value;
                        break;
                    case ValueModifier.Multiplicative:
                        MultiplicativeValue += value;
                        break;
                    case ValueModifier.PostAdditive:
                        PostAdditiveValue += value;
                        break;
                    case ValueModifier.Minimum:
                        if (MinimumPriority < priority)
                        {
                            MinimumPriority = priority;
                            MinimumValue = value;
                        }
                        else if (MinimumPriority == priority)
                            MinimumValue = Mathf.Max(MinimumValue, value);
                        break;
                    case ValueModifier.Maximum:
                        if (MaximumPriority < priority)
                        {
                            MaximumPriority = priority;
                            MaximumValue = value;
                        }
                        else if (MaximumPriority == priority)
                            MaximumValue = Mathf.Min(MaximumValue, value);
                        break;
                    case ValueModifier.Overwrite:
                        if (OverwritePriority <= priority)
                        {
                            OverwritePriority = priority;
                            OverwriteValue = value;
                        }
                        break;
                }
            }

            public float GetValue()
            {
                if (OverwritePriority >= 0)
                    return Mathf.Clamp(OverwriteValue, OverwritePriority <= MinimumPriority ? MinimumValue : float.NegativeInfinity, OverwritePriority <= MaximumPriority ? MaximumValue : float.PositiveInfinity);
                else
                    return Mathf.Clamp((BaseValue + AdditiveValue) * MultiplicativeValue + PostAdditiveValue, MinimumValue, MaximumValue);
            }
        }
    }
}
