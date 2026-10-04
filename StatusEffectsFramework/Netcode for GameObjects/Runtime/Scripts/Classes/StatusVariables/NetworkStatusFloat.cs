#if NETCODE
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace StatusEffectsFramework.Netcode
{
    [Serializable]
    public class NetworkStatusFloat : NetworkStatusVariable
    {
        public event Action<float, float> OnValueChanged;
        public event Action<float, float> OnPreEvaluationValueChanged;
        public event Action<float, float> OnBaseValueChanged;
        public event Action<bool, bool> OnSignProtectedChanged;

        public StatusNameFloat StatusName => m_StatusName;
        public float BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; BaseValueChanged(); } }
        public bool SignProtected { get { return m_SignProtected; } set { m_SignProtected = value; SignProtectedChanged(); } }
        public float Value => Instance != null ? PostEvaluationValue : m_BaseValue;
        public float PreEvaluationValue { get; protected set; }
#if UNITY_EDITOR
        [field: SerializeField]
#endif
        public float PostEvaluationValue { get; protected set; }

        [SerializeField] protected StatusNameFloat m_StatusName;
        [SerializeField] protected float m_BaseValue;
        [SerializeField] protected bool m_SignProtected;

        protected float m_PreviousBaseValue;
        protected bool m_PreviousSignProtected;
        protected float m_PreviousPreEvaluationValue;
        protected float m_PreviousPostEvaluationValue;

        protected NetworkStatusManager NetworkStatusManager;

        public NetworkStatusFloat(float baseValue, bool signProtected = true) : base(NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server)
        {
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;
            m_PreviousBaseValue = baseValue;
            m_PreviousSignProtected = signProtected;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public NetworkStatusFloat(float baseValue, StatusNameFloat statusName, bool signProtected = true) : base(NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;
            m_PreviousBaseValue = baseValue;
            m_PreviousSignProtected = signProtected;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public static implicit operator float(NetworkStatusFloat statusFloat) => statusFloat.Value;

        public override void SetManager(IStatusManager instance)
        {
            if (instance is NetworkStatusManager networkStatusManager)
                NetworkStatusManager = networkStatusManager;
            else
                Debug.LogError("Make sure that the Status Manager is set to a Network Status Manager when calling SetManager() on a Network Status Variable!");

            if (Instance != null)
                foreach (StatusEffect statusEffect in Instance.StatusEffects)
                    foreach (var dynamicFloat in statusEffect.DynamicFloats)
                        if (dynamicFloat.StatusName == m_StatusName)
                        {
                            if (dynamicFloat.PostEvaluate)
                                dynamicFloat.ValueChanged -= UpdatePostEvaluationValue;
                            else
                                dynamicFloat.ValueChanged -= UpdatePreEvaluationValue;
                        }

            base.SetManager(instance);

            foreach (StatusEffect statusEffect in Instance.StatusEffects)
                foreach (var dynamicFloat in statusEffect.DynamicFloats)
                    if (dynamicFloat.StatusName == m_StatusName)
                    {
                        if (dynamicFloat.PostEvaluate)
                            dynamicFloat.ValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicFloat.ValueChanged += UpdatePreEvaluationValue;
                    }

            m_PreviousBaseValue = m_BaseValue;
            m_PreviousSignProtected = m_SignProtected;
            UpdatePreEvaluationValue(true);
        }

        protected virtual void BaseValueChanged()
        {
            if (NetworkStatusManager)
            {
                if (!CanClientWrite(NetworkStatusManager.NetworkManager.LocalClientId))
                {
                    m_BaseValue = m_PreviousBaseValue;
                    LogWritePermissionError(NetworkStatusManager);
                    return;
                }

                if (m_BaseValue == m_PreviousBaseValue)
                    return;

                SetDirty(true);
            }

            UpdateBaseValue();
        }

        protected virtual void SignProtectedChanged()
        {
            if (NetworkStatusManager)
            {
                if (!CanClientWrite(NetworkStatusManager.NetworkManager.LocalClientId))
                {
                    m_SignProtected = m_PreviousSignProtected;
                    LogWritePermissionError(NetworkStatusManager);
                    return;
                }

                if (m_SignProtected == m_PreviousSignProtected)
                    return;

                SetDirty(true);
            }

            UpdateSignProtected();
        }

        protected override void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int previousStacks, int currentStacks)
        {
            if (action is StatusEffectAction.AddedStatusEffect)
                foreach (var dynamicFloat in statusEffect.DynamicFloats)
                    if (dynamicFloat.StatusName == m_StatusName)
                        if (dynamicFloat.PostEvaluate)
                            dynamicFloat.ValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicFloat.ValueChanged += UpdatePreEvaluationValue;
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Any(effect => effect.StatusName == m_StatusName))
                UpdatePreEvaluationValue(true);
        }

        protected float GetPreEvaluationValue()
        {
            if (Instance == null)
                return m_BaseValue;

            var statusFloatValue = new StatusFloatValue(m_BaseValue, m_SignProtected);

            float effectValue = default;

            foreach (StatusEffect statusEffect in Instance.StatusEffects)
            {
                foreach (Effect effect in statusEffect.Data.Effects)
                {
                    if (effect.StatusName != m_StatusName)
                        continue;

                    switch (effect.ValueSource)
                    {
                        case ValueSource.ExplicitValue:
                            effectValue = statusEffect.Stacks * effect.FloatValue;
                            break;
                        case ValueSource.BaseValue:
                            effectValue = statusEffect.Stacks * statusEffect.Data.BaseValue;
                            break;
                        case ValueSource.DynamicValue:
                            continue;
                    }

                    statusFloatValue.ApplyEffect(effect.ValueModifier, effectValue, effect.Priority);
                }

                foreach (var dynamicFloat in statusEffect.DynamicFloats)
                    if (!dynamicFloat.PostEvaluate && dynamicFloat.StatusName == m_StatusName)
                        statusFloatValue.ApplyEffect(dynamicFloat.ValueModifier, statusEffect.Stacks * dynamicFloat.Value, dynamicFloat.Priority);
            }

            return statusFloatValue.GetValue();
        }

        protected float GetPostEvaluationValue()
        {
            if (Instance == null)
                return PreEvaluationValue;

            var statusFloatValue = new StatusFloatValue(PreEvaluationValue, m_SignProtected);

            foreach (StatusEffect statusEffect in Instance.StatusEffects)
                foreach (var dynamicFloat in statusEffect.DynamicFloats)
                    if (dynamicFloat.PostEvaluate && dynamicFloat.StatusName == m_StatusName)
                        statusFloatValue.ApplyEffect(dynamicFloat.ValueModifier, statusEffect.Stacks * dynamicFloat.Value, dynamicFloat.Priority);

            return statusFloatValue.GetValue();
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
            if (m_BaseValue == m_PreviousBaseValue)
                return;

            OnBaseValueChanged?.Invoke(m_PreviousBaseValue, m_BaseValue);
            m_PreviousBaseValue = m_BaseValue;
            UpdatePreEvaluationValue();
        }

        protected void UpdateSignProtected()
        {
            if (m_SignProtected == m_PreviousSignProtected)
                return;

            OnSignProtectedChanged?.Invoke(m_PreviousSignProtected, m_SignProtected);
            m_PreviousSignProtected = m_SignProtected;
            UpdatePreEvaluationValue();
        }

        public override void OnInitialize()
        {
            base.OnInitialize();

            m_PreviousBaseValue = m_BaseValue;
            m_PreviousSignProtected = m_SignProtected;
        }

        public override void WriteField(FastBufferWriter writer)
        {
            writer.WriteValueSafe(m_BaseValue);
            writer.WriteValueSafe(m_SignProtected);
        }

        public override void ReadField(FastBufferReader reader)
        {
            reader.ReadValueSafe(out m_BaseValue);
            reader.ReadValueSafe(out m_SignProtected);

            UpdateBaseValue();
            UpdateSignProtected();
        }

        public override void WriteDelta(FastBufferWriter writer)
        {
            WriteField(writer);
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            ReadField(reader);
        }
#if UNITY_EDITOR

        protected async virtual void BaseValueUpdate()
        {
            await Task.Yield();

            BaseValueChanged();
        }

        protected async virtual void SignProtectedUpdate()
        {
            await Task.Yield();

            SignProtectedChanged();
        }
#endif
    }
}
#endif
