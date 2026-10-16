using System;
using System.Linq;
using UnityEngine;
using Unity.Mathematics;

namespace StatusEffectsFramework
{
    /// <summary>
    /// A float that <see cref="StatusEffect"/>s can change. Call <see cref="StatusVariable.SetManager"/> to start applying them, then
    /// read <see cref="Value"/>.
    /// </summary>
    [Serializable]
    public class StatusFloat : StatusVariable<StatusFloatState>
    {
        /// <summary>
        /// Raised with the previous and the new <see cref="Value"/> when it changes.
        /// </summary>
        public event Action<float, float> OnValueChanged;
        /// <summary>
        /// Raised with the previous and the new <see cref="PreEvaluationValue"/> when it changes.
        /// </summary>
        public event Action<float, float> OnPreEvaluationValueChanged;
        /// <summary>
        /// Raised with the previous and the new <see cref="BaseValue"/> when it changes.
        /// </summary>
        public event Action<float, float> OnBaseValueChanged;
        /// <summary>
        /// Raised with the previous and the new <see cref="SignProtected"/> when it changes.
        /// </summary>
        public event Action<bool, bool> OnSignProtectedChanged;

        /// <summary>
        /// The status that the effects must target to change this variable.
        /// </summary>
        public RegistrantKey<StatusNameFloat> StatusName => m_StatusName;
        /// <summary>
        /// The value before any status effect is applied. Changing it evaluates the status effects again.
        /// </summary>
        public float BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; UpdateBaseValue(); } }
        /// <summary>
        /// Limits the value to the sign of the base value: a positive base value can't go below 0 and a negative one can't go above 0.
        /// </summary>
        public bool SignProtected { get { return m_SignProtected; } set { m_SignProtected = value; UpdateSignProtected(); } }
        /// <summary>
        /// The value after all status effects are applied.
        /// </summary>
        public float Value => Manager != null ? PostEvaluationValue : m_BaseValue;
        /// <summary>
        /// The <see cref="BaseValue"/> with the status effects applied, except the dynamic effects that
        /// <see cref="DynamicEffect.PostEvaluate"/>.
        /// </summary>
        public float PreEvaluationValue { get; protected set; }
#if UNITY_EDITOR
        [field: SerializeField]
#endif
        /// <summary>
        /// The <see cref="PreEvaluationValue"/> with the post evaluation dynamic effects applied. This is the final value.
        /// </summary>
        public float PostEvaluationValue { get; protected set; }

        [Tooltip("The Status Name that effects target to change this variable.")]
        [SerializeField] protected RegistrantKey<StatusNameFloat> m_StatusName;
        [Tooltip("The value before any status effect is applied.")]
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
            m_PreviousBaseValue = baseValue;
            m_PreviousSignProtected = signProtected;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusFloat(float baseValue, RegistrantKey<StatusNameFloat> statusName, bool signProtected = true)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;
            m_PreviousBaseValue = baseValue;
            m_PreviousSignProtected = signProtected;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        /// <summary>
        /// Converts to the <see cref="Value"/>.
        /// </summary>
        public static implicit operator float(StatusFloat statusFloat) => statusFloat.Value;

        public override StatusFloatState GetState() => new StatusFloatState(m_BaseValue, m_SignProtected);

        public override void ApplyState(in StatusFloatState state)
        {
            m_BaseValue = state.BaseValue;
            UpdateBaseValue();
            m_SignProtected = state.SignProtected;
            UpdateSignProtected();
        }

        public override void SetManager(IStatusManager instance)
        {
            if (Manager != null)
                foreach (StatusEffect statusEffect in Manager.StatusEffects)
                    foreach (var dynamicFloat in statusEffect.DynamicFloats)
                        if (dynamicFloat.StatusName == m_StatusName)
                        {
                            if (dynamicFloat.PostEvaluate)
                                dynamicFloat.ValueChanged -= UpdatePostEvaluationValue;
                            else
                                dynamicFloat.ValueChanged -= UpdatePreEvaluationValue;
                        }

            base.SetManager(instance);
            
            foreach (StatusEffect statusEffect in Manager.StatusEffects)
                foreach (var dynamicFloat in statusEffect.DynamicFloats)
                    if (dynamicFloat.StatusName == m_StatusName)
                    {
                        if (dynamicFloat.PostEvaluate)
                            dynamicFloat.ValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicFloat.ValueChanged += UpdatePreEvaluationValue;
                    }

            m_PreviousBaseValue = m_BaseValue;
            m_PreviousSignProtected = m_SignProtected;
            UpdatePreEvaluationValue(true);
        }

        protected override void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int previousStacks, int currentStacks)
        {
            if (action is StatusEffectAction.AddedStatusEffect)
                foreach (var dynamicFloat in statusEffect.DynamicFloats)
                    if (dynamicFloat.StatusName == m_StatusName)
                        if (dynamicFloat.PostEvaluate)
                            dynamicFloat.ValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicFloat.ValueChanged += UpdatePreEvaluationValue;
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Any(effect => effect.StatusName.Key == m_StatusName.Key))
                UpdatePreEvaluationValue(true);
        }

        protected float GetPreEvaluationValue()
        {
            if (Manager == null)
                return m_BaseValue;

            var statusFloatValue = new StatusFloatValue(m_BaseValue, m_SignProtected);

            float effectValue = default;

            foreach (StatusEffect statusEffect in Manager.StatusEffects)
            {
                foreach (Effect effect in statusEffect.Data.Effects)
                {
                    if (effect.StatusName.Key != m_StatusName.Key)
                        continue;

                    switch (effect.ValueSource)
                    {
                        case ValueSource.ExplicitValue:
                            effectValue = statusEffect.Stacks * effect.FloatValue;
                            break;
                        case ValueSource.BaseValue:
                            effectValue = statusEffect.Stacks * statusEffect.Data.BaseValue;
                            break;
                        case ValueSource.DynamicValue:
                            continue;
                    }

                    statusFloatValue.ApplyEffect(effect.ValueModifier, effectValue, effect.Priority);
                }

                foreach (var dynamicFloat in statusEffect.DynamicFloats)
                    if (!dynamicFloat.PostEvaluate && dynamicFloat.StatusName == m_StatusName)
                        statusFloatValue.ApplyEffect(dynamicFloat.ValueModifier, statusEffect.Stacks * dynamicFloat.Value, dynamicFloat.Priority);
            }

            return statusFloatValue.GetValue();
        }

        protected float GetPostEvaluationValue()
        {
            if (Manager == null)
                return PreEvaluationValue;

            var statusFloatValue = new StatusFloatValue(PreEvaluationValue, m_SignProtected);
            
            foreach (StatusEffect statusEffect in Manager.StatusEffects)
                foreach (var dynamicFloat in statusEffect.DynamicFloats)
                    if (dynamicFloat.PostEvaluate && dynamicFloat.StatusName == m_StatusName)
                        statusFloatValue.ApplyEffect(dynamicFloat.ValueModifier, statusEffect.Stacks * dynamicFloat.Value, dynamicFloat.Priority);

            return statusFloatValue.GetValue();
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

        protected void UpdateSignProtected()
        {
            if (m_SignProtected != m_PreviousSignProtected)
            {
                OnSignProtectedChanged?.Invoke(m_PreviousSignProtected, m_SignProtected);
                m_PreviousSignProtected = m_SignProtected;
                RaiseStateChanged();
                UpdatePreEvaluationValue();
            }
        }
#if UNITY_EDITOR

        internal virtual void InternalUpdateBaseValue() => UpdateBaseValue();
        internal virtual void InternalUpdateSignProtected() => UpdateSignProtected();
#endif
    }

    internal struct StatusFloatValue
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
        // Order of the status effect the overwrite came from. The lowest (oldest) wins ties.
        public int OverwriteOrder;

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
            OverwriteOrder = int.MinValue;
            if (signProtected)
            {
                if (math.sign(baseValue) >= 0)
                    MinimumValue = 0;
                else
                    MaximumValue = 0;
            }
        }

        /// <param name="valueModifier">How <paramref name="value"/> is combined with the other effects.</param>
        /// <param name="value">The effect's value.</param>
        /// <param name="priority">Only used by <see cref="ValueModifier.Minimum"/>, <see cref="ValueModifier.Maximum"/>
        /// and <see cref="ValueModifier.Overwrite"/>. The highest priority wins.</param>
        /// <param name="order">Order of the status effect the value came from, oldest first. When omitted,
        /// the first effect applied wins ties.</param>
        public void ApplyEffect(ValueModifier valueModifier, float value, int priority, int order = int.MaxValue)
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
                        MinimumValue = math.max(MinimumValue, value);
                    break;
                case ValueModifier.Maximum:
                    if (MaximumPriority < priority)
                    {
                        MaximumPriority = priority;
                        MaximumValue = value;
                    }
                    else if (MaximumPriority == priority)
                        MaximumValue = math.min(MaximumValue, value);
                    break;
                case ValueModifier.Overwrite:
                    if (OverwritePriority < priority || (OverwritePriority == priority && order < OverwriteOrder))
                    {
                        OverwritePriority = priority;
                        OverwriteValue = value;
                        OverwriteOrder = order;
                    }
                    break;
            }
        }
        
        public float GetValue()
        {
            if (OverwritePriority >= 0)
                return math.clamp(OverwriteValue, OverwritePriority <= MinimumPriority ? MinimumValue : float.NegativeInfinity, OverwritePriority <= MaximumPriority ? MaximumValue : float.PositiveInfinity);
            else
                return math.clamp((BaseValue + AdditiveValue) * MultiplicativeValue + PostAdditiveValue, MinimumValue, MaximumValue);
        }
    }
}
