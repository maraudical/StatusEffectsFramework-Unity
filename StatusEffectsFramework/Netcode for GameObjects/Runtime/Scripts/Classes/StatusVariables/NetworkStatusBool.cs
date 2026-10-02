#if NETCODE
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace StatusEffectsFramework.NetCode
{
    [Serializable]
    public class NetworkStatusBool : NetworkStatusVariable
    {
        public event Action<bool, bool> OnValueChanged;
        public event Action<bool, bool> OnPreEvaluationValueChanged;
        public event Action<bool, bool> OnBaseValueChanged;

        public StatusNameBool StatusName => m_StatusName;
        public bool BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; BaseValueChanged(); } }
        public bool Value => Instance != null ? PostEvaluationValue : m_BaseValue;
        public bool PreEvaluationValue { get; protected set; }
#if UNITY_EDITOR
        [field: SerializeField]
#endif
        public bool PostEvaluationValue { get; protected set; }

        [SerializeField] protected StatusNameBool m_StatusName;
        [SerializeField] protected bool m_BaseValue;

        protected bool m_PreviousBaseValue;
        protected bool m_PreviousPreEvaluationValue;
        protected bool m_PreviousPostEvaluationValue;

        protected NetworkStatusManager NetworkStatusManager;

        public NetworkStatusBool(bool baseValue) : base(NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server)
        {
            m_BaseValue = baseValue;
            m_PreviousBaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public NetworkStatusBool(bool baseValue, StatusNameBool statusName) : base(NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;
            m_PreviousBaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public static implicit operator bool(NetworkStatusBool statusBool) => statusBool.Value;

        public override void SetManager(IStatusManager instance)
        {
            if (instance is NetworkStatusManager networkStatusManager)
                NetworkStatusManager = networkStatusManager;
            else
                Debug.LogError("Make sure that the Status Manager is set to a Network Status Manager when calling SetManager() on a Network Status Variable!");

            if (Instance != null)
                foreach (StatusEffect statusEffect in Instance.StatusEffects)
                    foreach (var dynamicBool in statusEffect.DynamicBools)
                        if (dynamicBool.StatusName == m_StatusName)
                        {
                            if (dynamicBool.PostEvaluate)
                                dynamicBool.ValueChanged -= UpdatePostEvaluationValue;
                            else
                                dynamicBool.ValueChanged -= UpdatePreEvaluationValue;
                        }

            base.SetManager(instance);

            foreach (StatusEffect statusEffect in Instance.StatusEffects)
                foreach (var dynamicBool in statusEffect.DynamicBools)
                    if (dynamicBool.StatusName == m_StatusName)
                    {
                        if (dynamicBool.PostEvaluate)
                            dynamicBool.ValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicBool.ValueChanged += UpdatePreEvaluationValue;
                    }

            m_PreviousBaseValue = m_BaseValue;
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

        protected override void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int previousStacks, int currentStacks)
        {
            if (action is StatusEffectAction.AddedStatusEffect)
                foreach (var dynamicBool in statusEffect.DynamicBools)
                    if (dynamicBool.StatusName == m_StatusName)
                        if (dynamicBool.PostEvaluate)
                            dynamicBool.ValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicBool.ValueChanged += UpdatePreEvaluationValue;
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Any(effect => effect.StatusName == m_StatusName))
                UpdatePreEvaluationValue(true);
        }

        protected bool GetPreEvaluationValue()
        {
            if (Instance == null)
                return m_BaseValue;

            var statusBoolValue = new StatusBoolValue(m_BaseValue);

            bool effectValue = default;

            foreach (StatusEffect statusEffect in Instance.StatusEffects)
            {
                foreach (Effect effect in statusEffect.Data.Effects)
                {
                    if (effect.StatusName != m_StatusName)
                        continue;

                    switch (effect.ValueSource)
                    {
                        case ValueSource.ExplicitValue:
                            effectValue = effect.BoolValue;
                            break;
                        case ValueSource.BaseValue:
                            effectValue = statusEffect.Data.BaseValue != 0;
                            break;
                        case ValueSource.DynamicValue:
                            continue;
                    }

                    statusBoolValue.ApplyEffect(effectValue, effect.Priority);
                }

                foreach (var dynamicBool in statusEffect.DynamicBools)
                    if (!dynamicBool.PostEvaluate && dynamicBool.StatusName == m_StatusName)
                        statusBoolValue.ApplyEffect(dynamicBool.Value, dynamicBool.Priority);
            }

            return statusBoolValue.GetValue();
        }

        protected bool GetPostEvaluationValue()
        {
            if (Instance == null)
                return PreEvaluationValue;

            var statusBoolValue = new StatusBoolValue(PreEvaluationValue);

            foreach (StatusEffect statusEffect in Instance.StatusEffects)
                foreach (var dynamicBool in statusEffect.DynamicBools)
                    if (dynamicBool.PostEvaluate && dynamicBool.StatusName == m_StatusName)
                        statusBoolValue.ApplyEffect(dynamicBool.Value, dynamicBool.Priority);

            return statusBoolValue.GetValue();
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

        public override void OnInitialize()
        {
            base.OnInitialize();

            m_PreviousBaseValue = m_BaseValue;
        }

        public override void WriteField(FastBufferWriter writer)
        {
            writer.WriteValueSafe(m_BaseValue);
        }

        public override void ReadField(FastBufferReader reader)
        {
            reader.ReadValueSafe(out m_BaseValue);

            UpdateBaseValue();
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
#endif
    }
}
#endif
