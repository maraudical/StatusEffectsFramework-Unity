#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
using System;
using Unity.Netcode;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    [GenerateSerializationForType(typeof(bool))]
    public class NetworkStatusBool : StatusBool
    {
        protected NetworkVariable<bool> NetworkBaseValue;
        protected NetworkStatusManager NetworkStatusManager;

        public NetworkStatusBool(bool baseValue) : base(baseValue) { }
        public NetworkStatusBool(bool baseValue, StatusNameBool statusName) : base(baseValue, statusName) { }

        public override void SetManager(IStatusManager instance)
        {
            if (instance is NetworkStatusManager networkStatusManager)
            {
                NetworkStatusManager = networkStatusManager;
                NetworkBaseValue = new NetworkVariable<bool>(m_BaseValue);
                NetworkBaseValue.Initialize(NetworkStatusManager);

                NetworkBaseValue.OnValueChanged += OnBaseValueChanged;
            }
            else
                Debug.LogError("Make sure that the Status Manager is set to a Network Status Manager when calling SetManager() on a Network Status Variable!");

            base.SetManager(instance);

            void OnBaseValueChanged(bool previousValue, bool newValue)
            {
                m_BaseValue = newValue;
                base.BaseValueChanged();
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
#endif
    }
}
#endif