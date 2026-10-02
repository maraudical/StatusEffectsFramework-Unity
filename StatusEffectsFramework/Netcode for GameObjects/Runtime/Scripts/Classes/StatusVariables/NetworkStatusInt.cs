#if NETCODE
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace StatusEffectsFramework.NetCode
{
    [Serializable]
    public class NetworkStatusInt : NetworkStatusVariable
    {
        public event Action<int, int> OnValueChanged;
        public event Action<int, int> OnPreEvaluationValueChanged;
        public event Action<int, int> OnBaseValueChanged;
        public event Action<bool, bool> OnSignProtectedChanged;

        public StatusNameInt StatusName => m_StatusName;
        public int BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; BaseValueChanged(); } }
        public bool SignProtected { get { return m_SignProtected; } set { m_SignProtected = value; SignProtectedChanged(); } }
        public int Value => Instance != null ? PostEvaluationValue : m_BaseValue;
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

        protected NetworkStatusManager NetworkStatusManager;

        public NetworkStatusInt(int baseValue, bool signProtected = true) : base(NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server)
        {
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;
            m_PreviousBaseValue = baseValue;
            m_PreviousSignProtected = signProtected;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public NetworkStatusInt(int baseValue, StatusNameInt statusName, bool signProtected = true) : base(NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;
            m_PreviousBaseValue = baseValue;
            m_PreviousSignProtected = signProtected;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public static implicit operator int(NetworkStatusInt statusInt) => statusInt.Value;

        public override void SetManager(IStatusManager instance)
        {
            if (instance is NetworkStatusManager networkStatusManager)
                NetworkStatusManager = networkStatusManager;
            else
                Debug.LogError("Make sure that the Status Manager is set to a Network Status Manager when calling SetManager() on a Network Status Variable!");

            if (Instance != null)
                foreach (StatusEffect statusEffect in Instance.StatusEffects)
                    foreach (var dynamicInt in statusEffect.DynamicInts)
                        if (dynamicInt.StatusName == m_StatusName)
                        {
                            if (dynamicInt.PostEvaluate)
                                dynamicInt.ValueChanged -= UpdatePostEvaluationValue;
                            else
                                dynamicInt.ValueChanged -= UpdatePreEvaluationValue;
                        }

            base.SetManager(instance);

            foreach (StatusEffect statusEffect in Instance.StatusEffects)
                foreach (var dynamicInt in statusEffect.DynamicInts)
                    if (dynamicInt.StatusName == m_StatusName)
                    {
                        if (dynamicInt.PostEvaluate)
                            dynamicInt.ValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicInt.ValueChanged += UpdatePreEvaluationValue;
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
                foreach (var dynamicInt in statusEffect.DynamicInts)
                    if (dynamicInt.StatusName == m_StatusName)
                        if (dynamicInt.PostEvaluate)
                            dynamicInt.ValueChanged += UpdatePostEvaluationValue;
                        else
                            dynamicInt.ValueChanged += UpdatePreEvaluationValue;
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Any(effect => effect.StatusName == m_StatusName))
                UpdatePreEvaluationValue(true);
        }

        protected int GetPreEvaluationValue()
        {
            if (Instance == null)
                return m_BaseValue;

            var statusIntValue = new StatusIntValue(m_BaseValue, m_SignProtected);

            int effectValue = default;

            foreach (StatusEffect statusEffect in Instance.StatusEffects)
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
            if (Instance == null)
                return PreEvaluationValue;

            var statusIntValue = new StatusIntValue(PreEvaluationValue, m_SignProtected);

            foreach (StatusEffect statusEffect in Instance.StatusEffects)
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
