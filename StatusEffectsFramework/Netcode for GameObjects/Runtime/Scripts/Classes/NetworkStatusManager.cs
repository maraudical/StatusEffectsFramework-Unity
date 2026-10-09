using System;
using System.Collections.Generic;
using System.Linq;
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
    public class NetworkStatusManager : NetworkBehaviour, INetworkStatusManager
    {
        public event Action<StatusEffect, StatusEffectAction, int, int> StatusEffectAction
        {
            add => m_StatusManager.StatusEffectAction += value;
            remove => m_StatusManager.StatusEffectAction -= value;
        }

        public IEnumerable<StatusEffect> StatusEffects => m_StatusManager.StatusEffects;

        private StatusRegistry m_Registry;
        private NetworkList<NetworkStatusEffect> m_NetworkEffects;

        [SerializeField, HideInInspector] private StatusManager m_StatusManager;

        private const string k_SyncError = "The Status Registry is not synced with the server! The Status Effect with id {0} could not be found, so it has failed to get added.";

        // Caching the reference in the editor means instances don't need to get the component when they are created.
        private void OnValidate()
        {
            if (!m_StatusManager)
                TryGetComponent(out m_StatusManager);
        }

        private void Awake()
        {
            m_NetworkEffects = new();

            // Only needed when the reference wasn't serialized, such as when this component is added at runtime.
            if (!m_StatusManager)
                m_StatusManager = GetComponent<StatusManager>();
        }

        public override void OnNetworkSpawn()
        {
            m_Registry = StatusRegistry.Instance;

            if (IsServer)
            {
                // Status effects can only exist while spawned. Any that were added to the Status Manager directly
                // are removed before subscribing so they aren't synced. Clients remove theirs in SyncAllForClient.
                if (m_StatusManager.StatusEffects.Any())
                {
                    Debug.LogWarning("Status Effects were added to the Status Manager before it was spawned, so they have been removed. Only add Status Effects through the Network Status Manager while it is spawned.", this);
                    m_StatusManager.RemoveAllStatusEffects();
                }

                m_StatusManager.StatusEffectAction += OnStatusEffectForServer;
            }
            else
            {
                SyncAllForClient();
                m_NetworkEffects.OnListChanged += OnListChangedForClient;
            }

            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            // Status effects can only exist while spawned. The server removes them before unsubscribing so the
            // network list is emptied too, otherwise pooled and in-scene objects would keep it when respawned.
            m_StatusManager.RemoveAllStatusEffects();

            m_StatusManager.StatusEffectAction -= OnStatusEffectForServer;
            m_NetworkEffects.OnListChanged -= OnListChangedForClient;
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
            if (IsSpawned && IsServer)
                return true;

            Debug.LogError(IsSpawned
                ? "Please do not try to add or remove Status Effects from clients. Only the Server/Host can change them."
                : "Status Effects can only be added or removed while this object is spawned on the network.");
            return false;
        }

        private int IndexOfInstance(uint instanceId)
        {
            for (int i = 0; i < m_NetworkEffects.Count; i++)
                if (m_NetworkEffects[i].InstanceId == instanceId)
                    return i;

            return -1;
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
            int index = IndexOfInstance(instanceId);
            if (index < 0)
                return;

            var networkEffect = m_NetworkEffects[index];
            networkEffect.Duration = duration;
            m_NetworkEffects[index] = networkEffect;
        }
        #endregion

        #region Client
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
