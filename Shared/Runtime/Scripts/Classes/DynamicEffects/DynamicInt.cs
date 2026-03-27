using System;

namespace StatusEffectFramework
{
    public class DynamicInt
    {
        internal event Action OnValueChanged;
        internal ValueModifier ValueModifier;
        internal bool PostEvaluate;
        private int m_Value;
        public int Value { get => m_Value; set { Value = value; OnValueChanged(); } }

        public DynamicInt(DynamicFloatEffect dynamicEffect, Effect effect, int value = 0)
        {
            ValueModifier = effect.ValueModifier;
            PostEvaluate = dynamicEffect.PostEvaluate;
            m_Value = value;
        }
    }
}
