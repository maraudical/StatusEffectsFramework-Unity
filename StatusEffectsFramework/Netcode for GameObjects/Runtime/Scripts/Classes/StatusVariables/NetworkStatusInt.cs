using System;

namespace StatusEffectsFramework.Netcode
{
    [Serializable]
    public class NetworkStatusInt : NetworkStatusVariable<StatusInt, StatusIntState>
    {
        public event Action<int, int> OnValueChanged { add => m_Inner.OnValueChanged += value; remove => m_Inner.OnValueChanged -= value; }
        public event Action<int, int> OnPreEvaluationValueChanged { add => m_Inner.OnPreEvaluationValueChanged += value; remove => m_Inner.OnPreEvaluationValueChanged -= value; }
        public event Action<int, int> OnBaseValueChanged { add => m_Inner.OnBaseValueChanged += value; remove => m_Inner.OnBaseValueChanged -= value; }
        public event Action<bool, bool> OnSignProtectedChanged { add => m_Inner.OnSignProtectedChanged += value; remove => m_Inner.OnSignProtectedChanged -= value; }

        public StatusNameInt StatusName => m_Inner.StatusName;
        public int BaseValue { get => m_Inner.BaseValue; set { if (CanWrite()) m_Inner.BaseValue = value; } }
        public bool SignProtected { get => m_Inner.SignProtected; set { if (CanWrite()) m_Inner.SignProtected = value; } }
        public int Value => m_Inner.Value;
        public int PreEvaluationValue => m_Inner.PreEvaluationValue;
        public int PostEvaluationValue => m_Inner.PostEvaluationValue;

        public NetworkStatusInt(int baseValue, bool signProtected = true, StatusWritePermission writePermission = StatusWritePermission.ServerAndOwner)
            : base(new StatusInt(baseValue, signProtected), writePermission) { }

        public NetworkStatusInt(int baseValue, StatusNameInt statusName, bool signProtected = true, StatusWritePermission writePermission = StatusWritePermission.ServerAndOwner)
            : base(new StatusInt(baseValue, statusName, signProtected), writePermission) { }

        public static implicit operator int(NetworkStatusInt statusInt) => statusInt.Value;
    }
}
