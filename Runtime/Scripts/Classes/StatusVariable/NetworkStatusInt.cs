#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
using System;
using Unity.Netcode;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    [GenerateSerializationForType(typeof(int))]
    public class NetworkStatusInt : StatusInt
    {
        protected NetworkVariable<int> NetworkBaseValue;
        protected NetworkVariable<bool> NetworkSignProtected;
        protected NetworkStatusManager NetworkStatusManager;

        public NetworkStatusInt(int baseValue, bool signProtected = true) : base(baseValue, signProtected) { }
        public NetworkStatusInt(int baseValue, StatusNameInt statusName, bool signProtected = true) : base(baseValue, statusName, signProtected) { }

        public override void SetManager(IStatusManager instance)
        {
            if (instance is NetworkStatusManager networkStatusManager)
            {
                NetworkStatusManager = networkStatusManager;
                NetworkBaseValue = new NetworkVariable<int>(m_BaseValue);
                NetworkSignProtected = new NetworkVariable<bool>(m_SignProtected);

                NetworkBaseValue.Initialize(NetworkStatusManager);
                NetworkSignProtected.Initialize(NetworkStatusManager);

                NetworkBaseValue.OnValueChanged += OnBaseValueChanged;
                NetworkSignProtected.OnValueChanged += OnSignProtectedChanged;
            }
            else
                Debug.LogError("Make sure that the Status Manager is set to a Network Status Manager when calling SetManager() on a Network Status Variable!");

            base.SetManager(instance);

            void OnBaseValueChanged(int previousValue, int newValue)
            {
                m_BaseValue = newValue;
                base.BaseValueChanged();
            }

            void OnSignProtectedChanged(bool previousValue, bool newValue)
            {
                m_SignProtected = newValue;
                base.SignProtectedChanged();
            }
        }

        protected override void BaseValueChanged()
        {
            if (!NetworkStatusManager.NetworkManager.IsServer)
            {
                m_BaseValue = NetworkBaseValue.Value;
                return;
            }

            NetworkBaseValue.Value = m_BaseValue;

            base.BaseValueChanged();
        }

        protected override void SignProtectedChanged()
        {
            if (!NetworkStatusManager.NetworkManager.IsServer)
            {
                m_SignProtected = NetworkSignProtected.Value;
                return;
            }

            NetworkSignProtected.Value = m_SignProtected;

            base.SignProtectedChanged();
        }
#if UNITY_EDITOR

        protected override void BaseValueUpdate()
        {
            if (!NetworkStatusManager.NetworkManager.IsServer)
            {
                m_BaseValue = NetworkBaseValue.Value;
                return;
            }

            NetworkBaseValue.Value = m_BaseValue;

            base.BaseValueUpdate();
        }

        protected override void SignProtectedUpdate()
        {
            if (!NetworkStatusManager.NetworkManager.IsServer)
            {
                m_SignProtected = NetworkSignProtected.Value;
                return;
            }

            NetworkSignProtected.Value = m_SignProtected;

            base.SignProtectedUpdate();
        }
#endif
    }
}
#endif