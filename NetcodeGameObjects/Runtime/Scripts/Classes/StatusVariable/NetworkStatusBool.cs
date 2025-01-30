#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace StatusEffects.NetCode.GameObjects
{
    [Serializable]
    public class NetworkStatusBool : NetworkStatusVariable
    {
        [SerializeField] public event Action<bool, bool> OnValueChanged;
        [SerializeField] public event Action<bool, bool> OnBaseValueChanged;

        public StatusNameBool StatusName => m_StatusName;
        public bool BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; BaseValueChanged(); } }
        public bool Value => Instance != null ? m_Value : m_BaseValue;

        [SerializeField] protected StatusNameBool m_StatusName;
        [SerializeField] protected bool m_BaseValue;
        protected bool m_PreviousBaseValue;
        [SerializeField] protected bool m_Value;
        protected bool m_PreviousValue;

        protected NetworkStatusManager NetworkStatusManager;
        protected bool m_IsDisposed;

        public NetworkStatusBool(bool baseValue) : base(NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server)
        {
            m_BaseValue = baseValue;

            if (Instance != null)
            {
                UpdateBaseValue();
                m_PreviousBaseValue = baseValue;
            }
        }

        public NetworkStatusBool(bool baseValue, StatusNameBool statusName) : base(NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;

            if (Instance != null)
            {
                UpdateBaseValue();
                m_PreviousBaseValue = baseValue;
            }
            UpdateValue();
        }

        public static implicit operator bool(NetworkStatusBool statusBool) => statusBool.Value;

        public override void SetManager(IStatusManager instance)
        {
            if (instance is NetworkStatusManager networkStatusManager)
                NetworkStatusManager = networkStatusManager;
            else
                Debug.LogError("Make sure that the Status Manager is set to a Network Status Manager when calling SetManager() on a Network Status Variable!");

            base.SetManager(instance);
            m_PreviousBaseValue = m_BaseValue;
            m_Value = m_BaseValue;
            UpdateValue();
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

                if (NetworkStatusManager)
                    SetDirty(true);
            }

            UpdateBaseValue();
            m_PreviousBaseValue = m_BaseValue;
            UpdateValue();
        }

        protected override void InstanceUpdate(StatusEffect statusEffect)
        {
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Select(e => e.StatusName).Contains(m_StatusName))
                UpdateValue();
        }

        protected bool GetValue()
        {
            if (Instance == null)
                return m_BaseValue;

            bool value = m_BaseValue;
            int priority = -1;

            bool effectValue;

            foreach (StatusEffect statusEffect in Instance.Effects)
            {
                foreach (Effect effect in statusEffect.Data.Effects)
                {
                    if (effect.StatusName != m_StatusName)
                        continue;

                    effectValue = effect.UseBaseValue ? Convert.ToBoolean(statusEffect.Data.BaseValue) : effect.BoolValue;

                    if (priority < effect.Priority)
                    {
                        priority = effect.Priority;
                        value = effectValue;
                    }
                }
            }

            return value;
        }

        protected void UpdateValue()
        {
            m_PreviousValue = m_Value;
            m_Value = GetValue();
            if (m_Value != m_PreviousValue)
                OnValueChanged?.Invoke(m_PreviousValue, m_Value);
        }

        protected void UpdateBaseValue()
        {
            if (m_BaseValue != m_PreviousBaseValue)
                OnBaseValueChanged?.Invoke(m_PreviousBaseValue, m_BaseValue);
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
            m_PreviousBaseValue = m_BaseValue;
            reader.ReadValueSafe(out m_BaseValue);

            if (m_BaseValue != m_PreviousBaseValue)
            {
                OnBaseValueChanged?.Invoke(m_PreviousBaseValue, m_BaseValue);
                UpdateValue();
            }
        }

        public override void WriteDelta(FastBufferWriter writer)
        {
            writer.WriteValueSafe(m_BaseValue);
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            m_PreviousBaseValue = m_BaseValue;
            reader.ReadValueSafe(out m_BaseValue);

            if (m_BaseValue != m_PreviousBaseValue)
            {
                OnBaseValueChanged?.Invoke(m_PreviousBaseValue, m_BaseValue);
                UpdateValue();
            }
        }

        public override void ResetDirty()
        {
            if (IsDirty())
            {
                m_PreviousBaseValue = m_BaseValue;
                m_PreviousValue = m_Value;
            }
            base.ResetDirty();
        }

        public override void Dispose()
        {
            if (m_IsDisposed)
                return;

            m_IsDisposed = true;

            m_BaseValue = default;
            m_PreviousBaseValue = default;
            m_Value = default;
            m_PreviousValue = default;

            base.Dispose();
        }

        ~NetworkStatusBool()
        {
            Dispose();
        }
#if UNITY_EDITOR

        protected async virtual void BaseValueUpdate()
        {
            await Task.Yield();

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

                if (NetworkStatusManager)
                    SetDirty(true);
            }

            UpdateBaseValue();
            m_PreviousBaseValue = m_BaseValue;
            UpdateValue();
        }
#endif
    }
}
#endif