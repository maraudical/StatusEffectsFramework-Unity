#if NETCODE
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

using EventType = Unity.Netcode.NetworkListEvent<StatusEffectsFramework.Netcode.NetworkStatusEffect>.EventType;

namespace StatusEffectsFramework.Netcode
{
    /// <summary>
    /// A component for a network synced StatusManager. The server runs all of the status effect
    /// logic and clients mirror the resulting status effects.
    /// </summary>
    /// <remarks>
    /// Status effects are synced by the ids assigned in the <see cref="StatusRegistry"/>, so the server and
    /// clients must have the same registered assets. A client logs an error when its registry differs.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StatusManager))]
    [AddComponentMenu("Netcode/Network Status Manager")]
    public class NetworkStatusManager : NetworkBehaviour, IStatusManager
    {
        public event Action<StatusEffect, StatusEffectAction, int, int> StatusEffectAction
        {
            add => m_StatusManager.StatusEffectAction += value;
            remove => m_StatusManager.StatusEffectAction -= value;
        }

        public IEnumerable<StatusEffect> StatusEffects => m_StatusManager.StatusEffects;

        private StatusRegistry m_Registry;
        private NetworkList<NetworkStatusEffect> m_NetworkEffects;
        private readonly NetworkVariable<FixedString64Bytes> m_RegistryHash = new();

        [SerializeField, HideInInspector] private StatusManager m_StatusManager;

        private const HideFlags k_HideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        private const string k_SyncError = "The Status Registry is not synced with the server! The Status Effect with id {0} could not be found, so it has failed to get added.";

        private void OnValidate()
        {
            if (!m_StatusManager)
                if (!TryGetComponent(out m_StatusManager))
                    m_StatusManager = gameObject.AddComponent<StatusManager>();

            if (m_StatusManager.hideFlags != k_HideFlags)
                _ = NextFrameHideFlags();
        }

        private async Task NextFrameHideFlags()
        {
            await Task.Yield();

            if (m_StatusManager)
                m_StatusManager.hideFlags = k_HideFlags;
        }

        private void Awake()
        {
            m_NetworkEffects = new();

            if (m_StatusManager.hideFlags != k_HideFlags)
                m_StatusManager.hideFlags = k_HideFlags;
        }

        public override void OnNetworkSpawn()
        {
            m_Registry = StatusRegistry.Get();

            if (IsServer)
            {
                m_RegistryHash.Value = m_Registry ? m_Registry.RegistryHashString : string.Empty;
                // Effects that were added before spawning still need to be synced.
                foreach (var statusEffect in m_StatusManager.StatusEffects)
                    AddNetworkEffect(statusEffect);

                m_StatusManager.StatusEffectAction += OnStatusEffectForServer;
            }
            else
            {
                CheckRegistryHash();
                m_RegistryHash.OnValueChanged += OnRegistryHashChanged;
                SyncAllForClient();
                m_NetworkEffects.OnListChanged += OnListChangedForClient;
            }

            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            m_StatusManager.StatusEffectAction -= OnStatusEffectForServer;
            m_NetworkEffects.OnListChanged -= OnListChangedForClient;
            m_RegistryHash.OnValueChanged -= OnRegistryHashChanged;
        }

        #region Status Manager Methods
        public bool GetStatusEffect(uint instanceId, out StatusEffect statusEffect) => m_StatusManager.GetStatusEffect(instanceId, out statusEffect);

#nullable enable
        public IEnumerable<StatusEffect> GetStatusEffects(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null, bool matchAllGroups = true) => m_StatusManager.GetStatusEffects(group, name, data, matchAllGroups);

        public StatusEffect GetFirstStatusEffect(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null, bool matchAllGroups = true) => m_StatusManager.GetFirstStatusEffect(group, name, data, matchAllGroups);
#nullable restore

        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, int stacks = 1)
        {
            if (!CheckForServer())
                return null;

            return m_StatusManager.AddStatusEffect(statusEffectData, stacks);
        }

        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, int stacks = 1)
        {
            if (!CheckForServer())
                return null;

            return m_StatusManager.AddStatusEffect(statusEffectData, duration, stacks);
        }

        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, StatusEvent statusEvent, int stacks = 1)
        {
            if (!CheckForServer())
                return null;

            return m_StatusManager.AddStatusEffect(statusEffectData, duration, statusEvent, stacks);
        }

        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, Func<bool> predicate, int stacks = 1)
        {
            if (!CheckForServer())
                return null;

            return m_StatusManager.AddStatusEffect(statusEffectData, predicate, stacks);
        }

        public void RemoveStatusEffect(StatusEffect statusEffect)
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveStatusEffect(statusEffect);
        }

#nullable enable
        public void RemoveStatusEffect(StatusEffectData statusEffectData, int? stacks = null)
#nullable disable
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveStatusEffect(statusEffectData, stacks);
        }

        public void RemoveStatusEffect(ComparableName name, int? stacks = null)
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveStatusEffect(name, stacks);
        }

        public void RemoveStatusEffect(StatusEffectGroup group, int? stacks = null, bool matchAllGroups = true)
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveStatusEffect(group, stacks, matchAllGroups);
        }

        public void RemoveAllStatusEffects()
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveAllStatusEffects();
        }
        #endregion

        #region Private Methods
        private bool CheckForServer()
        {
            if (!NetworkManager || !NetworkManager.IsListening)
                return true;
            if (!IsServer)
            {
                Debug.LogError("Please do not try to add or remove Status Effects from non-servers. If this is a Host/Server, double check that this GameObject has a NetworkObject component!");
                return false;
            }
            return true;
        }

        private int IndexOfInstance(uint instanceId)
        {
            for (int i = 0; i < m_NetworkEffects.Count; i++)
                if (m_NetworkEffects[i].InstanceId == instanceId)
                    return i;

            return -1;
        }

        private void CheckRegistryHash()
        {
            if (!m_Registry || m_RegistryHash.Value.IsEmpty)
                return;

            if (m_RegistryHash.Value != m_Registry.RegistryHashString)
                Debug.LogError($"|Client-{NetworkManager.LocalClientId}|{name}| The Status Registry doesn't match the server's, so status effects will refer to the wrong data. Make sure the server and clients have the same Status Effect Datas, Status Names, Comparable Names and Status Events registered.");
        }
        #endregion

        #region Server
        private void OnStatusEffectForServer(StatusEffect statusEffect, StatusEffectsFramework.StatusEffectAction action, int previousStacks, int currentStacks)
        {
            switch (action)
            {
                case StatusEffectsFramework.StatusEffectAction.AddedStatusEffect:
                    AddNetworkEffect(statusEffect);
                    break;
                case StatusEffectsFramework.StatusEffectAction.RemovedStatusEffect:
                    int removeIndex = IndexOfInstance(statusEffect.Id);
                    if (removeIndex >= 0)
                        m_NetworkEffects.RemoveAt(removeIndex);
                    break;
                default:
                    int index = IndexOfInstance(statusEffect.Id);
                    if (index < 0)
                        break;
                    var networkEffect = m_NetworkEffects[index];
                    networkEffect.Stacks = statusEffect.Stacks;
                    m_NetworkEffects[index] = networkEffect;
                    break;
            }
        }

        private void AddNetworkEffect(StatusEffect statusEffect)
        {
            if (IndexOfInstance(statusEffect.Id) >= 0)
                return;

            // The elapsed time is measured on this machine, but clients need the time on the network clock.
            double serverTimeAdded = NetworkManager.ServerTime.Time - (Time.timeAsDouble - statusEffect.TimeAdded);

            if (!m_Registry.TryGetId(statusEffect.Data, out var dataId))
            {
                Debug.LogError($"The Status Effect Data \"{statusEffect.Data.name}\" is not in the Status Registry, so it can't be synced to clients.");
                return;
            }

            m_NetworkEffects.Add(new NetworkStatusEffect(dataId, statusEffect.Timing, statusEffect.Duration, statusEffect.Stacks, statusEffect.Id, serverTimeAdded));
            // Event timed status effects count down when their event is invoked.
            statusEffect.DurationUpdate += (duration) => OnDurationUpdate(statusEffect.Id, duration);
        }

        private void OnDurationUpdate(uint instanceId, float duration)
        {
            if (!IsSpawned || !IsServer)
                return;

            int index = IndexOfInstance(instanceId);
            if (index < 0)
                return;

            var networkEffect = m_NetworkEffects[index];
            networkEffect.Duration = duration;
            m_NetworkEffects[index] = networkEffect;
        }
        #endregion

        #region Client
        private void OnRegistryHashChanged(FixedString64Bytes previous, FixedString64Bytes current)
        {
            CheckRegistryHash();
        }

        private void OnListChangedForClient(NetworkListEvent<NetworkStatusEffect> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case EventType.Add:
                case EventType.Insert:
                    AddForClient(changeEvent.Value);
                    break;
                case EventType.Remove:
                case EventType.RemoveAt:
                    if (m_StatusManager.GetStatusEffect(changeEvent.Value.InstanceId, out var statusEffect))
                        m_StatusManager.RemoveStatusEffect(statusEffect);
                    else
                        SyncAllForClient();
                    break;
                case EventType.Clear:
                    m_StatusManager.RemoveAllStatusEffects();
                    break;
                case EventType.Full:
                    SyncAllForClient();
                    break;
                case EventType.Value:
                    UpdateForClient(changeEvent.Value);
                    break;
                default:
                    Debug.LogError($"NetworkList change event {changeEvent.Type} not implemented!");
                    return;
            }
        }

        /// <summary>
        /// Makes the local status effects match the network list, keeping the ones that already exist.
        /// </summary>
        private void SyncAllForClient()
        {
            var networkIds = new HashSet<uint>();
            foreach (var networkEffect in m_NetworkEffects)
                networkIds.Add(networkEffect.InstanceId);

            foreach (var statusEffect in m_StatusManager.StatusEffects.Where(effect => !networkIds.Contains(effect.Id)).ToList())
                m_StatusManager.RemoveStatusEffect(statusEffect);

            foreach (var networkEffect in m_NetworkEffects)
                UpdateForClient(networkEffect);
        }

        private void AddForClient(NetworkStatusEffect networkEffect)
        {
            if (!m_Registry || !m_Registry.IdToStatusEffectData.TryGetValue(networkEffect.Id, out var statusEffectData))
            {
                Debug.LogError(string.Format(k_SyncError, networkEffect.Id));
                return;
            }

            // Work out when the status effect was added in local time.
            double elapsedTime = Math.Max(0d, NetworkManager.ServerTime.Time - networkEffect.ServerTimeAdded);
            m_StatusManager.ForceAddStatusEffect(networkEffect.InstanceId, statusEffectData, networkEffect.Timing, Time.timeAsDouble - elapsedTime, networkEffect.Duration, networkEffect.Stacks);
        }

        private void UpdateForClient(NetworkStatusEffect networkEffect)
        {
            if (!m_StatusManager.GetStatusEffect(networkEffect.InstanceId, out var statusEffect))
            {
                AddForClient(networkEffect);
                return;
            }

            if (statusEffect.Duration != networkEffect.Duration)
                statusEffect.Duration = networkEffect.Duration;

            m_StatusManager.SetStatusEffectStacks(statusEffect, networkEffect.Stacks);
        }
        #endregion
    }
}
#endif
