using System;

namespace StatusEffectFramework
{
    public class DynamicInt
    {
        internal event Action OnValueChanged;
        public StatusName StatusName { get; private set; }
        public ValueModifier ValueModifier { get; private set; }
        public bool PostEvaluate { get; private set; }
        public int Priority { get; private set; }
        public int Value { get => m_Value; set { Value = value; OnValueChanged?.Invoke(); } }
        private int m_Value;

        public DynamicInt(DynamicIntEffect dynamicIntEffect, Effect effect, int value = 0)
        {
            StatusName = effect.StatusName;
            ValueModifier = effect.ValueModifier;
            PostEvaluate = dynamicIntEffect.PostEvaluate;
            Priority = effect.Priority;
            m_Value = value;
        }
    }
}
