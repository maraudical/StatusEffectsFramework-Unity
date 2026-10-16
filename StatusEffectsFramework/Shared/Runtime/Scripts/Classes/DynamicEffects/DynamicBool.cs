using System;

namespace StatusEffectsFramework
{
    /// <summary>
    /// The value of an <see cref="Effect"/> that is set at runtime, created by a <see cref="DynamicEffectBool"/>.
    /// Change <see cref="Value"/> and the status that it applies to is evaluated again.
    /// </summary>
    public class DynamicBool
    {
        internal event Action ValueChanged;
        /// <summary>
        /// The status that the value is applied to.
        /// </summary>
        public RegistrantKey<StatusNameBool> StatusName { get; private set; }
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
        public bool Value { get => m_Value; set { if (value == m_Value) return; m_Value = value; ValueChanged?.Invoke(); } }
        private bool m_Value;

        public DynamicBool(DynamicEffectBool dynamicBoolEffect, Effect effect, bool value = false)
        {
            StatusName = new RegistrantKey<StatusNameBool>(effect.StatusName.Key);
            PostEvaluate = dynamicBoolEffect.PostEvaluate;
            Priority = effect.Priority;
            m_Value = value;
        }
    }
}
