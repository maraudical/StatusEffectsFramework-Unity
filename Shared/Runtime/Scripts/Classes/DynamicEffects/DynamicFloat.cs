using System;

namespace StatusEffectFramework
{
    public class DynamicFloat
    {
        internal event Action OnValueChanged;
        internal ValueModifier ValueModifier;
        internal bool PostEvaluate;
        private float m_Value;
        public float Value { get => m_Value; set { Value = value; OnValueChanged(); } }

        public DynamicFloat(Effect effect, float value = 0)
        {
            ValueModifier = effect.ValueModifier;
            PostEvaluate = effect.DynamicFloatEffect.PostEvaluate;
            m_Value = value;
        }
    }
}
