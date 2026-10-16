using System;
using System.Diagnostics;

namespace StatusEffectsFramework
{
    /// <summary>
    /// The value of an <see cref="Effect"/> that is set at runtime, created by a <see cref="DynamicEffectFloat"/>.
    /// Change <see cref="Value"/> and the status that it applies to is evaluated again.
    /// </summary>
    public class DynamicFloat
    {
        internal event Action ValueChanged;
        /// <summary>
        /// The status that the value is applied to.
        /// </summary>
        public RegistrantKey<StatusNameFloat> StatusName { get; private set; }
        /// <summary>
        /// How the value is combined with the other effects.
        /// </summary>
        public ValueModifier ValueModifier { get; private set; }
        /// <summary>
        /// Whether the value is applied after all other effects. See <see cref="DynamicEffect.PostEvaluate"/>.
        /// </summary>
        public bool PostEvaluate { get; private set; }
        /// <summary>
        /// The priority of the <see cref="Effect"/> that this value belongs to.
        /// </summary>
        public int Priority { get; private set; }
        /// <summary>
        /// The current value. Setting it to a different value re-evaluates the status.
        /// </summary>
        public float Value { get => m_Value; set { if (value == m_Value) return; m_Value = value; ValueChanged?.Invoke(); } }
        private float m_Value;

        public DynamicFloat(DynamicEffectFloat dynamicFloatEffect, Effect effect, float value = 0)
        {
            StatusName = new RegistrantKey<StatusNameFloat>(effect.StatusName.Key);
            ValueModifier = effect.ValueModifier;
            PostEvaluate = dynamicFloatEffect.PostEvaluate;
            Priority = effect.Priority;
            m_Value = value;
        }
    }
}
