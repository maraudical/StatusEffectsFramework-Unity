using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using NUnit.Framework.Internal;

#if BURST
using Unity.Burst;
#endif

namespace StatusEffectFramework
{
    [Serializable]
    public class StatusBool : StatusVariable
    {
        public event Action<bool, bool> OnValueChanged;
        public event Action<bool, bool> OnPreEvaluationValueChanged;
        public event Action<bool, bool> OnBaseValueChanged;

        public StatusNameBool StatusName => m_StatusName;
        public bool BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; UpdateBaseValue(); } }
        public bool Value => Manager != null ? PostEvaluationValue : m_BaseValue;
        public bool PreEvaluationValue { get; protected set; }
        public bool PostEvaluationValue { get; protected set; }

        [SerializeField] protected StatusNameBool m_StatusName;
        [SerializeField] protected bool m_BaseValue;

        protected bool m_PreviousBaseValue;
        protected bool m_PreviousPreEvaluationValue;
        protected bool m_PreviousPostEvaluationValue;

        public StatusBool(bool baseValue, bool signProtected = true)
        {
            m_BaseValue = baseValue;
            m_PreviousBaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusBool(bool baseValue, StatusNameBool statusName, bool signProtected = true)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;
            m_PreviousBaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public static implicit operator bool(StatusBool statusBool) => statusBool.Value;

        public override void SetManager(IStatusManager instance)
        {
            if (Manager != null)
                foreach (StatusEffect statusEffect in Manager.Effects)
                    foreach (var dynamicBool in statusEffect.DynamicBools)
                        if (dynamicBool.StatusName == m_StatusName)
                        {
                            if (dynamicBool.PostEvaluate)
                                dynamicBool.OnValueChanged -= UpdatePostEvaluationValue;
                            else
                                dynamicBool.OnValueChanged -= UpdatePreEvaluationValue;
                        }

            base.SetManager(instance);

            foreach (StatusEffect statusEffect in Manager.Effects)
                foreach (var dynamicBool in statusEffect.DynamicBools)
                    if (dynamicBool.StatusName == m_StatusName)
                    {
                        if (dynamicBool.PostEvaluate)
                            dynamicBool.OnValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicBool.OnValueChanged += UpdatePreEvaluationValue;
                    }

            UpdatePreEvaluationValue();
        }

        protected override void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int previousStacks, int currentStacks)
        {
            if (action is StatusEffectAction.AddedStatusEffect)
                foreach (var dynamicBool in statusEffect.DynamicBools)
                    if (dynamicBool.StatusName == m_StatusName)
                        if (dynamicBool.PostEvaluate)
                            dynamicBool.OnValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicBool.OnValueChanged += UpdatePreEvaluationValue;
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Any(effect => effect.StatusName == m_StatusName))
                UpdatePreEvaluationValue();
        }

        protected bool GetPreEvaluationValue()
        {
            if (Manager == null)
                return m_BaseValue;

            var statusBoolValue = new StatusBoolValue(m_BaseValue);

            bool effectValue = default;

            foreach (StatusEffect statusEffect in Manager.Effects)
            {
                foreach (Effect effect in statusEffect.Data.Effects)
                {
                    if (effect.StatusName != m_StatusName)
                        continue;

                    switch (effect.ValueType)
                    {
                        case ValueType.ExplicitValue:
                            effectValue = effect.BoolValue;
                            break;
                        case ValueType.BaseValue:
                            effectValue = Convert.ToBoolean(statusEffect.Data.BaseValue);
                            break;
                        case ValueType.DynamicValue:
                            continue;
                    }

                    statusBoolValue.ApplyEffect(effectValue, effect.Priority);
                }

                foreach (var dynamicBool in statusEffect.DynamicBools)
                    if (!dynamicBool.PostEvaluate && dynamicBool.StatusName == m_StatusName)
                        statusBoolValue.ApplyEffect(dynamicBool.Value, dynamicBool.Priority);
            }

            return statusBoolValue.GetValue();
        }

        protected bool GetPostEvaluationValue()
        {
            if (Manager == null)
                return PreEvaluationValue;

            var statusBoolValue = new StatusBoolValue(PreEvaluationValue);

            foreach (StatusEffect statusEffect in Manager.Effects)
                foreach (var dynamicBool in statusEffect.DynamicBools)
                    if (dynamicBool.PostEvaluate && dynamicBool.StatusName == m_StatusName)
                        statusBoolValue.ApplyEffect(dynamicBool.Value, dynamicBool.Priority);

            return statusBoolValue.GetValue();
        }

        protected void UpdatePreEvaluationValue()
        {
            m_PreviousPreEvaluationValue = PreEvaluationValue;
            PreEvaluationValue = GetPreEvaluationValue();
            if (PreEvaluationValue != m_PreviousPreEvaluationValue)
            {
                OnPreEvaluationValueChanged?.Invoke(m_PreviousPreEvaluationValue, PreEvaluationValue);
                UpdatePostEvaluationValue();
            }
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
            {
                OnBaseValueChanged?.Invoke(m_PreviousBaseValue, m_BaseValue);
                m_PreviousBaseValue = m_BaseValue;
                UpdatePreEvaluationValue();
            }
        }
#if UNITY_EDITOR

        protected virtual async void BaseValueUpdate()
        {
            await Task.Yield();

            UpdateBaseValue();
        }
#endif
    }

#if BURST
    [BurstCompile]
#endif
    internal struct StatusBoolValue
    {
        public bool Value;

        public int Priority;

        public StatusBoolValue(bool baseValue)
        {
            Value = baseValue;
            Priority = int.MinValue;
        }

#if BURST
        [BurstCompile]
#endif
        public void ApplyEffect(bool value, int priority)
        {
            if (Priority < priority)
            {
                Priority = priority;
                Value = value;
            }
        }

#if BURST
        [BurstCompile]
#endif
        public bool GetValue() => Value;
    }
}
