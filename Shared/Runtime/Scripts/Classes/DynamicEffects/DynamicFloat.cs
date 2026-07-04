using System;
using System.Diagnostics;

namespace StatusEffectsFramework
{
    public class DynamicFloat
    {
        internal event Action OnValueChanged;
        public StatusName StatusName { get; private set; }
        public ValueModifier ValueModifier { get; private set; }
        public bool PostEvaluate { get; private set; }
        public int Priority { get; private set; }
        public float Value { get => m_Value; set { if (value == m_Value) return; m_Value = value; OnValueChanged?.Invoke(); } }
        private float m_Value;

        public DynamicFloat(DynamicEffectFloat dynamicFloatEffect, Effect effect, float value = 0)
        {
            StatusName = effect.StatusName;
            ValueModifier = effect.ValueModifier;
            PostEvaluate = dynamicFloatEffect.PostEvaluate;
            Priority = effect.Priority;
            m_Value = value;
        }
    }
}
