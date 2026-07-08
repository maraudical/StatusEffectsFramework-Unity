using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
#if BURST
using Unity.Burst;
using Unity.Mathematics;
#endif


namespace StatusEffectsFramework
{
    [Serializable]
    public class StatusInt : StatusVariable
    {
        public event Action<int, int> OnValueChanged;
        public event Action<int, int> OnPreEvaluationValueChanged;
        public event Action<int, int> OnBaseValueChanged;
        public event Action<bool, bool> OnSignProtectedChanged;

        public StatusNameInt StatusName => m_StatusName;
        public int BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; UpdateBaseValue(); } }
        public bool SignProtected { get { return m_SignProtected; } set { m_SignProtected = value; UpdateSignProtected(); } }
        public int Value => Manager != null ? PostEvaluationValue : m_BaseValue;
        public int PreEvaluationValue { get; protected set; }
#if UNITY_EDITOR
        [field: SerializeField]
#endif
        public int PostEvaluationValue { get; protected set; }

        [SerializeField] protected StatusNameInt m_StatusName;
        [SerializeField] protected int m_BaseValue;
        [SerializeField] protected bool m_SignProtected;

        protected int m_PreviousBaseValue;
        protected bool m_PreviousSignProtected;
        protected int m_PreviousPreEvaluationValue;
        protected int m_PreviousPostEvaluationValue;

        public StatusInt(int baseValue, bool signProtected = true)
        {
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;
            m_PreviousBaseValue = baseValue;
            m_PreviousSignProtected = signProtected;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusInt(int baseValue, StatusNameInt statusName, bool signProtected = true)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;
            m_PreviousBaseValue = baseValue;
            m_PreviousSignProtected = signProtected;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public static implicit operator int(StatusInt statusInt) => statusInt.Value;

        public override void SetManager(IStatusManager instance)
        {
            if (Manager != null)
                foreach (StatusEffect statusEffect in Manager.StatusEffects)
                    foreach (var dynamicInt in statusEffect.DynamicInts)
                        if (dynamicInt.StatusName == m_StatusName)
                        {
                            if (dynamicInt.PostEvaluate)
                                dynamicInt.OnValueChanged -= UpdatePostEvaluationValue;
                            else
                                dynamicInt.OnValueChanged -= UpdatePreEvaluationValue;
                        }

            base.SetManager(instance);

            foreach (StatusEffect statusEffect in Manager.StatusEffects)
                foreach (var dynamicInt in statusEffect.DynamicInts)
                    if (dynamicInt.StatusName == m_StatusName)
                    {
                        if (dynamicInt.PostEvaluate)
                            dynamicInt.OnValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicInt.OnValueChanged += UpdatePreEvaluationValue;
                    }

            UpdatePreEvaluationValue(true);
        }

        protected override void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int previousStacks, int currentStacks)
        {
            if (action is StatusEffectAction.AddedStatusEffect)
                foreach (var dynamicInt in statusEffect.DynamicInts)
                    if (dynamicInt.StatusName == m_StatusName)
                        if (dynamicInt.PostEvaluate)
                            dynamicInt.OnValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicInt.OnValueChanged += UpdatePreEvaluationValue;
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Any(effect => effect.StatusName == m_StatusName))
                UpdatePreEvaluationValue(true);
        }

        protected int GetPreEvaluationValue()
        {
            if (Manager == null)
                return m_BaseValue;

            var statusIntValue = new StatusIntValue(m_BaseValue, m_SignProtected);

            int effectValue = default;

            foreach (StatusEffect statusEffect in Manager.StatusEffects)
            {
                foreach (Effect effect in statusEffect.Data.Effects)
                {
                    if (effect.StatusName != m_StatusName)
                        continue;

                    switch (effect.ValueSource)
                    {
                        case ValueSource.ExplicitValue:
                            effectValue = statusEffect.Stacks * effect.IntValue;
                            break;
                        case ValueSource.BaseValue:
                            effectValue = statusEffect.Stacks * (int)statusEffect.Data.BaseValue;
                            break;
                        case ValueSource.DynamicValue:
                            continue;
                    }

                    statusIntValue.ApplyEffect(effect.ValueModifier, effectValue, effect.Priority);
                }

                foreach (var dynamicInt in statusEffect.DynamicInts)
                    if (!dynamicInt.PostEvaluate && dynamicInt.StatusName == m_StatusName)
                        statusIntValue.ApplyEffect(dynamicInt.ValueModifier, statusEffect.Stacks * dynamicInt.Value, dynamicInt.Priority);
            }

            return statusIntValue.GetValue();
        }

        protected int GetPostEvaluationValue()
        {
            if (Manager == null)
                return PreEvaluationValue;

            var statusIntValue = new StatusIntValue(PreEvaluationValue, m_SignProtected);

            foreach (StatusEffect statusEffect in Manager.StatusEffects)
                foreach (var dynamicInt in statusEffect.DynamicInts)
                    if (dynamicInt.PostEvaluate && dynamicInt.StatusName == m_StatusName)
                        statusIntValue.ApplyEffect(dynamicInt.ValueModifier, statusEffect.Stacks * dynamicInt.Value, dynamicInt.Priority);

            return statusIntValue.GetValue();
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
                UpdatePreEvaluationValue();
            }
        }

        protected void UpdateSignProtected()
        {
            if (m_SignProtected != m_PreviousSignProtected)
            {
                OnSignProtectedChanged?.Invoke(m_PreviousSignProtected, m_SignProtected);
                m_PreviousSignProtected = m_SignProtected;
                UpdatePreEvaluationValue();
            }
        }
#if UNITY_EDITOR

        protected virtual async void BaseValueUpdate()
        {
            await Task.Yield();

            UpdateBaseValue();
        }

        protected virtual async void SignProtectedUpdate()
        {
            await Task.Yield();

            UpdateSignProtected();
        }
#endif
    }

#if BURST
    [BurstCompile]
#endif
    internal struct StatusIntValue
    {
        public int BaseValue;

        public int AdditiveValue;
        public int MultiplicativeValue;
        public int PostAdditiveValue;
        public int MinimumPriority;
        public int MinimumValue;
        public int MaximumPriority;
        public int MaximumValue;
        public int OverwritePriority;
        public int OverwriteValue;

        public StatusIntValue(int baseValue, bool signProtected)
        {
            BaseValue = baseValue;
            AdditiveValue = 0;
            MultiplicativeValue = 1;
            PostAdditiveValue = 0;
            MinimumPriority = -1;
            MinimumValue = int.MinValue;
            MaximumPriority = -1;
            MaximumValue = int.MaxValue;
            OverwritePriority = -1;
            OverwriteValue = 0;
            if (signProtected)
            {
                if (
#if BURST
                    math.sign
#else
                    Mathf.Sign
#endif
                    (baseValue) >= 0)
                    MinimumValue = 0;
                else
                    MaximumValue = 0;
            }
        }

#if BURST
        [BurstCompile]
#endif
        public void ApplyEffect(ValueModifier valueModifier, int value, int priority)
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
                        MinimumValue =
#if BURST
                        math.max
#else
                        Mathf.Max
#endif
                        (MinimumValue, value);
                    break;
                case ValueModifier.Maximum:
                    if (MaximumPriority < priority)
                    {
                        MaximumPriority = priority;
                        MaximumValue = value;
                    }
                    else if (MaximumPriority == priority)
                        MaximumValue =
#if BURST
                        math.min
#else
                        Mathf.Min
#endif
                        (MaximumValue, value);
                    break;
                case ValueModifier.Overwrite:
                    if (OverwritePriority <= priority)
                    {
                        OverwritePriority = priority;
                        OverwriteValue = value;
                    }
                    break;
            }
        }

#if BURST
        [BurstCompile]
#endif
        public int GetValue()
        {
            if (OverwritePriority >= 0)
                return
#if BURST
                    math.clamp
#else
                    Mathf.Clamp
#endif
                    (OverwriteValue, OverwritePriority <= MinimumPriority ? MinimumValue : int.MinValue, OverwritePriority <= MaximumPriority ? MaximumValue : int.MaxValue);
            else
                return
#if BURST
                    math.clamp
#else
                    Mathf.Clamp
#endif
                    ((BaseValue + AdditiveValue) * MultiplicativeValue + PostAdditiveValue, MinimumValue, MaximumValue);
        }
    }
}
