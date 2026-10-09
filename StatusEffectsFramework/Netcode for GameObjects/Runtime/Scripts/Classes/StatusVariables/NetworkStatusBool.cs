using System;

namespace StatusEffectsFramework.Netcode
{
    [Serializable]
    public class NetworkStatusBool : NetworkStatusVariable<StatusBool, StatusBoolState>
    {
        public event Action<bool, bool> OnValueChanged { add => m_Inner.OnValueChanged += value; remove => m_Inner.OnValueChanged -= value; }
        public event Action<bool, bool> OnPreEvaluationValueChanged { add => m_Inner.OnPreEvaluationValueChanged += value; remove => m_Inner.OnPreEvaluationValueChanged -= value; }
        public event Action<bool, bool> OnBaseValueChanged { add => m_Inner.OnBaseValueChanged += value; remove => m_Inner.OnBaseValueChanged -= value; }

        public StatusNameBool StatusName => m_Inner.StatusName;
        public bool BaseValue { get => m_Inner.BaseValue; set { if (CanWrite()) m_Inner.BaseValue = value; } }
        public bool Value => m_Inner.Value;
        public bool PreEvaluationValue => m_Inner.PreEvaluationValue;
        public bool PostEvaluationValue => m_Inner.PostEvaluationValue;

        public NetworkStatusBool(bool baseValue, StatusWritePermission writePermission = StatusWritePermission.ServerAndOwner)
            : base(new StatusBool(baseValue), writePermission) { }

        public NetworkStatusBool(bool baseValue, StatusNameBool statusName, StatusWritePermission writePermission = StatusWritePermission.ServerAndOwner)
            : base(new StatusBool(baseValue, statusName), writePermission) { }

        public static implicit operator bool(NetworkStatusBool statusBool) => statusBool.Value;
    }
}
