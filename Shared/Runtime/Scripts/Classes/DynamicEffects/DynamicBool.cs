using System;

namespace StatusEffectFramework
{
    public class DynamicBool
    {
        internal event Action OnValueChanged;
        public StatusName StatusName { get; private set; }
        public bool PostEvaluate { get; private set; }
        public int Priority { get; private set; }
        public bool Value { get => m_Value; set { Value = value; OnValueChanged?.Invoke(); } }
        private bool m_Value;

        public DynamicBool(DynamicBoolEffect dynamicBoolEffect, Effect effect, bool value = false)
        {
            StatusName = effect.StatusName;
            PostEvaluate = dynamicBoolEffect.PostEvaluate;
            Priority = effect.Priority;
            m_Value = value;
        }
    }
}
