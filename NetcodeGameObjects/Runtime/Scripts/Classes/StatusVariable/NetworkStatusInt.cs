#if NETCODE
using System;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

namespace StatusEffects.NetCode.GameObjects
{
    [Serializable]
    public class NetworkStatusInt : NetworkStatusVariable
    {
        public event Action<int, int> OnValueChanged;
        public event Action<int, int> OnBaseValueChanged;
        public event Action<bool, bool> OnSignProtectedChanged;

        public StatusNameInt StatusName => m_StatusName;
        public int BaseValue { get { return m_BaseValue; } set { m_BaseValue = value; BaseValueChanged(); } }
        public bool SignProtected { get { return m_SignProtected; } set { m_SignProtected = value; SignProtectedChanged(); } }
        public int Value => Instance != null ? m_Value : m_BaseValue;

        [SerializeField] protected StatusNameInt m_StatusName;
        [SerializeField] protected int m_BaseValue;
        protected int m_PreviousBaseValue;
        [SerializeField] protected bool m_SignProtected;
        protected bool m_PreviousSignProtected;
        [SerializeField] protected int m_Value;
        protected int m_PreviousValue;

        protected NetworkStatusManager NetworkStatusManager;
        protected bool m_IsDisposed;

        public NetworkStatusInt(int baseValue, bool signProtected = true) : base(NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server)
        {
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;

            if (Instance != null)
            {
                UpdateBaseValue();
                m_PreviousBaseValue = baseValue;
                UpdateSignProtected();
                m_PreviousSignProtected = signProtected;
            }
        }

        public NetworkStatusInt(int baseValue, StatusNameInt statusName, bool signProtected = true) : base(NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server)
        {
            m_StatusName = statusName;
            m_BaseValue = baseValue;
            m_SignProtected = signProtected;

            if (Instance != null)
            {
                UpdateBaseValue();
                m_PreviousBaseValue = baseValue;
                UpdateSignProtected();
                m_PreviousSignProtected = signProtected;
            }
            UpdateValue();
        }

        public static implicit operator int(NetworkStatusInt statusInt) => statusInt.Value;

        public override void SetManager(IStatusManager instance)
        {
            if (instance is NetworkStatusManager networkStatusManager)
                NetworkStatusManager = networkStatusManager;
            else
                Debug.LogError("Make sure that the Status Manager is set to a Network Status Manager when calling SetManager() on a Network Status Variable!");

            base.SetManager(instance);
            m_PreviousBaseValue = m_BaseValue;
            m_PreviousSignProtected = m_SignProtected;
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

                if (NetworkStatusManager)
                    SetDirty(true);
            }

            UpdateSignProtected();
            m_PreviousSignProtected = m_SignProtected;
            UpdateValue();
        }

        protected override void InstanceUpdate(StatusEffect statusEffect)
        {
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.Data.Effects.Select(e => e.StatusName).Contains(m_StatusName))
                UpdateValue();
        }

        protected virtual int GetValue()
        {
            if (Instance == null)
                return m_BaseValue;

            bool positive = Mathf.Sign(m_BaseValue) > 0;
            int additiveValue = 0;
            int multiplicativeValue = 1;
            int postAdditiveValue = 0;

            int effectValue;

            foreach (StatusEffect statusEffect in Instance.Effects)
            {
                foreach (Effect effect in statusEffect.Data.Effects)
                {
                    if (effect.StatusName != m_StatusName)
                        continue;

                    effectValue = statusEffect.Stacks * (effect.UseBaseValue ? (int)statusEffect.Data.BaseValue : effect.IntValue);

                    switch (effect.ValueModifier)
                    {
                        case ValueModifier.Additive:
                            additiveValue += effectValue;
                            break;
                        case ValueModifier.Multiplicative:
                            multiplicativeValue += effectValue;
                            break;
                        case ValueModifier.PostAdditive:
                            postAdditiveValue += effectValue;
                            break;

                    }
                }
            }

            if (m_SignProtected)
                return Mathf.Clamp((m_BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue, positive ? 0 : int.MinValue, positive ? int.MaxValue : 0);
            else
                return (m_BaseValue + additiveValue) * multiplicativeValue + postAdditiveValue;
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

        protected void UpdateSignProtected()
        {
            if (m_SignProtected != m_PreviousSignProtected)
                OnSignProtectedChanged?.Invoke(m_PreviousSignProtected, m_SignProtected);
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
            m_PreviousBaseValue = m_BaseValue;
            m_PreviousSignProtected = m_SignProtected;
            reader.ReadValueSafe(out m_BaseValue);
            reader.ReadValueSafe(out m_SignProtected);

            UpdateBaseValue();
            UpdateSignProtected();
            if (m_BaseValue != m_PreviousBaseValue || m_SignProtected != m_PreviousSignProtected)
                UpdateValue();
        }

        public override void WriteDelta(FastBufferWriter writer)
        {
            writer.WriteValueSafe(m_BaseValue);
            writer.WriteValueSafe(m_SignProtected);
        }

        public override void ReadDelta(FastBufferReader reader, bool keepDirtyDelta)
        {
            m_PreviousBaseValue = m_BaseValue;
            m_PreviousSignProtected = m_SignProtected;
            reader.ReadValueSafe(out m_BaseValue);
            reader.ReadValueSafe(out m_SignProtected);

            UpdateBaseValue();
            UpdateSignProtected();
            if (m_BaseValue != m_PreviousBaseValue || m_SignProtected != m_PreviousSignProtected)
                UpdateValue();
        }

        public override void ResetDirty()
        {
            if (IsDirty())
            {
                m_PreviousBaseValue = m_BaseValue;
                m_PreviousSignProtected = m_SignProtected;
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
            m_SignProtected = default;
            m_PreviousSignProtected = default;
            m_Value = default;
            m_PreviousValue = default;

            base.Dispose();
        }

        ~NetworkStatusInt()
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

        protected async virtual void SignProtectedUpdate()
        {
            await Task.Yield();

            if (NetworkStatusManager)
            {
                if (!CanClientWrite(NetworkStatusManager.NetworkManager.LocalClientId))
                {
                    m_SignProtected = m_PreviousSignProtected;
                    return;
                }

                if (m_SignProtected == m_PreviousSignProtected)
                    return;

                if (NetworkStatusManager)
                    SetDirty(true);
            }

            UpdateSignProtected();
            m_PreviousSignProtected = m_SignProtected;
            UpdateValue();
        }
#endif
    }
}
#endif