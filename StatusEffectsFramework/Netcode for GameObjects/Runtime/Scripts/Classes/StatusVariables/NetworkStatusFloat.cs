using System;

namespace StatusEffectsFramework.Netcode
{
    [Serializable]
    public class NetworkStatusFloat : NetworkStatusVariable<StatusFloat, StatusFloatState>
    {
        public event Action<float, float> OnValueChanged { add => m_Inner.OnValueChanged += value; remove => m_Inner.OnValueChanged -= value; }
        public event Action<float, float> OnPreEvaluationValueChanged { add => m_Inner.OnPreEvaluationValueChanged += value; remove => m_Inner.OnPreEvaluationValueChanged -= value; }
        public event Action<float, float> OnBaseValueChanged { add => m_Inner.OnBaseValueChanged += value; remove => m_Inner.OnBaseValueChanged -= value; }
        public event Action<bool, bool> OnSignProtectedChanged { add => m_Inner.OnSignProtectedChanged += value; remove => m_Inner.OnSignProtectedChanged -= value; }

        public StatusNameFloat StatusName => m_Inner.StatusName;
        public float BaseValue { get => m_Inner.BaseValue; set { if (CanWrite()) m_Inner.BaseValue = value; } }
        public bool SignProtected { get => m_Inner.SignProtected; set { if (CanWrite()) m_Inner.SignProtected = value; } }
        public float Value => m_Inner.Value;
        public float PreEvaluationValue => m_Inner.PreEvaluationValue;
        public float PostEvaluationValue => m_Inner.PostEvaluationValue;

        public NetworkStatusFloat(float baseValue, bool signProtected = true, StatusWritePermission writePermission = StatusWritePermission.ServerAndOwner)
            : base(new StatusFloat(baseValue, signProtected), writePermission) { }

        public NetworkStatusFloat(float baseValue, StatusNameFloat statusName, bool signProtected = true, StatusWritePermission writePermission = StatusWritePermission.ServerAndOwner)
            : base(new StatusFloat(baseValue, statusName, signProtected), writePermission) { }

        public static implicit operator float(NetworkStatusFloat statusFloat) => statusFloat.Value;
    }
}
