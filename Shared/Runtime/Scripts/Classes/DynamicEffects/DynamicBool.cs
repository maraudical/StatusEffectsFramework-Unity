using System;

namespace StatusEffectFramework
{
    public class DynamicBool
    {
        internal event Action OnValueChanged;
        public StatusName StatusName { get; private set; }
        public ValueModifier ValueModifier { get; private set; }
        public bool PostEvaluate { get; private set; }
        public int Priority { get; private set; }
        public bool Value { get => m_Value; set { Value = value; OnValueChanged?.Invoke(); } }
        private bool m_Value;

        public DynamicBool(DynamicBoolEffect dynamicBoolEffect, Effect effect, bool value = 0)
        {
            StatusName = effect.StatusName;
            ValueModifier = effect.ValueModifier;
            PostEvaluate = dynamicBoolEffect.PostEvaluate;
            Priority = effect.Priority;
            m_Value = value;
        }
    }
}
