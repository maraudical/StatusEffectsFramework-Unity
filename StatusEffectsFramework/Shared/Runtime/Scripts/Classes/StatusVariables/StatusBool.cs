using System;
using System.Linq;
using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// A bool that <see cref="StatusEffect"/>s can change. Call <see cref="StatusVariable.SetManager"/> to start applying them, then
    /// read <see cref="Value"/>.
    /// </summary>
    [Serializable]
    public class StatusBool : StatusVariable<StatusBoolState>
    {
        /// <summary>
        /// Raised with the previous and the new <see cref="Value"/> when it changes.
        /// </summary>
        public event Action<bool, bool> OnValueChanged;
        /// <summary>
        /// Raised with the previous and the new <see cref="PreEvaluationValue"/> when it changes.
        /// </summary>
        public event Action<bool, bool> OnPreEvaluationValueChanged;
        /// <summary>
        /// Raised with the previous and the new <see cref="BaseValue"/> when it changes.
        /// </summary>
        public event Action<bool, bool> OnBaseValueChanged;

        /// <summary>
        /// The status that the effects must target to change this variable.
        /// </summary>
        public RegistrantKey<StatusNameBool> StatusName => m_StatusName;
        /// <summary>
        /// The value before any status effect is applied. Changing it evaluates the status effects again.
        /// </summary>
        public bool BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; UpdateBaseValue(); } }
        /// <summary>
        /// The value after all status effects are applied.
        /// </summary>
        public bool Value => Manager != null ? PostEvaluationValue : m_BaseValue;
        /// <summary>
        /// The <see cref="BaseValue"/> with the status effects applied, except the dynamic effects that
        /// <see cref="DynamicEffect.PostEvaluate"/>.
        /// </summary>
        public bool PreEvaluationValue { get; protected set; }
#if UNITY_EDITOR
        [field: SerializeField]
#endif
        /// <summary>
        /// The <see cref="PreEvaluationValue"/> with the post evaluation dynamic effects applied. This is the final value.
        /// </summary>
        public bool PostEvaluationValue { get; protected set; }

        [Tooltip("The Status Name that effects target to change this variable.")]
        [SerializeField] protected RegistrantKey<StatusNameBool> m_StatusName;
        [Tooltip("The value before any status effect is applied.")]
        [SerializeField] protected bool m_BaseValue;

        protected bool m_PreviousBaseValue;
        protected bool m_PreviousPreEvaluationValue;
        protected bool m_PreviousPostEvaluationValue;

        public StatusBool(bool baseValue)
        {
            m_BaseValue = baseValue;
            m_PreviousBaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusBool(bool baseValue, RegistrantKey<StatusNameBool> statusName)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;
            m_PreviousBaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        /// <summary>
        /// Converts to the <see cref="Value"/>.
        /// </summary>
        public static implicit operator bool(StatusBool statusBool) => statusBool.Value;

        public override StatusBoolState GetState() => new StatusBoolState(m_BaseValue);

        public override void ApplyState(in StatusBoolState state)
        {
            m_BaseValue = state.BaseValue;
            UpdateBaseValue();
        }

        public override void SetManager(IStatusManager instance)
        {
            if (Manager != null)
                foreach (StatusEffect statusEffect in Manager.StatusEffects)
                    foreach (var dynamicBool in statusEffect.DynamicBools)
                        if (dynamicBool.StatusName == m_StatusName)
                        {
                            if (dynamicBool.PostEvaluate)
                                dynamicBool.ValueChanged -= UpdatePostEvaluationValue;
                            else
                                dynamicBool.ValueChanged -= UpdatePreEvaluationValue;
                        }

            base.SetManager(instance);

            foreach (StatusEffect statusEffect in Manager.StatusEffects)
                foreach (var dynamicBool in statusEffect.DynamicBools)
                    if (dynamicBool.StatusName == m_StatusName)
                    {
                        if (dynamicBool.PostEvaluate)
                            dynamicBool.ValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicBool.ValueChanged += UpdatePreEvaluationValue;
                    }

            m_PreviousBaseValue = m_BaseValue;
            UpdatePreEvaluationValue(true);
        }

        protected override void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int previousStacks, int currentStacks)
        {
            if (action is StatusEffectAction.AddedStatusEffect)
                foreach (var dynamicBool in statusEffect.DynamicBools)
                    if (dynamicBool.StatusName == m_StatusName)
                        if (dynamicBool.PostEvaluate)
                            dynamicBool.ValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicBool.ValueChanged += UpdatePreEvaluationValue;
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Any(effect => effect.StatusName.Key == m_StatusName.Key))
                UpdatePreEvaluationValue(true);
        }

        protected bool GetPreEvaluationValue()
        {
            if (Manager == null)
                return m_BaseValue;

            var statusBoolValue = new StatusBoolValue(m_BaseValue);

            bool effectValue = default;

            foreach (StatusEffect statusEffect in Manager.StatusEffects)
            {
                foreach (Effect effect in statusEffect.Data.Effects)
                {
                    if (effect.StatusName.Key != m_StatusName.Key)
                        continue;

                    switch (effect.ValueSource)
                    {
                        case ValueSource.ExplicitValue:
                            effectValue = effect.BoolValue;
                            break;
                        case ValueSource.BaseValue:
                            effectValue = Convert.ToBoolean(statusEffect.Data.BaseValue);
                            break;
                        case ValueSource.DynamicValue:
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

            foreach (StatusEffect statusEffect in Manager.StatusEffects)
                foreach (var dynamicBool in statusEffect.DynamicBools)
                    if (dynamicBool.PostEvaluate && dynamicBool.StatusName == m_StatusName)
                        statusBoolValue.ApplyEffect(dynamicBool.Value, dynamicBool.Priority);

            return statusBoolValue.GetValue();
        }

        protected void UpdatePreEvaluationValue() => UpdatePreEvaluationValue(false);
        protected void UpdatePreEvaluationValue(bool updatePostEvaluation)
        {
            m_PreviousPreEvaluationValue = PreEvaluationValue;
            PreEvaluationValue = GetPreEvaluationValue();
            if (PreEvaluationValue != m_PreviousPreEvaluationValue)
            {
                OnPreEvaluationValueChanged?.Invoke(m_PreviousPreEvaluationValue, PreEvaluationValue);
                UpdatePostEvaluationValue();
                return;
            }
            if (updatePostEvaluation)
                UpdatePostEvaluationValue();
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
                RaiseStateChanged();
                UpdatePreEvaluationValue();
            }
        }
#if UNITY_EDITOR

        internal virtual void InternalUpdateBaseValue() => UpdateBaseValue();
#endif
    }

    internal struct StatusBoolValue
    {
        public bool Value;

        public int Priority;

        // Order of the status effect the value came from. The lowest (oldest) wins ties.
        public int Order;

        public StatusBoolValue(bool baseValue)
        {
            Value = baseValue;
            Priority = int.MinValue;
            Order = int.MinValue;
        }

        /// <param name="value">The effect's value.</param>
        /// <param name="priority">The highest priority wins.</param>
        /// <param name="order">Order of the status effect the value came from, oldest first. When omitted,
        /// the first effect applied wins ties.</param>
        public void ApplyEffect(bool value, int priority, int order = int.MaxValue)
        {
            if (Priority < priority || (Priority == priority && order < Order))
            {
                Priority = priority;
                Value = value;
                Order = order;
            }
        }

        public bool GetValue() => Value;
    }
}
