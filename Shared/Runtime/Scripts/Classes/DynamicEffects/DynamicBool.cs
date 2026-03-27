using System;

namespace StatusEffectFramework
{
    public class DynamicBool
    {
        internal event Action OnValueChanged;
        internal ValueModifier ValueModifier;
        internal bool PostEvaluate;
        internal int Priority;
        private bool m_Value;
        public bool Value { get => m_Value; set { Value = value; OnValueChanged(); } }

        public DynamicBool(DynamicBoolEffect dynamicEffect, Effect effect, bool value = false) 
        { 
            ValueModifier = effect.ValueModifier;
            PostEvaluate = dynamicEffect.PostEvaluate;
            Priority = effect.Priority;
            m_Value = value;
        }
    }
}
