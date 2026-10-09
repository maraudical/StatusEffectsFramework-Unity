using System;
using System.Collections.Generic;
using System.Linq;
using PurrNet;
using UnityEngine;

namespace StatusEffectsFramework.PurrNet
{
    /// <summary>
    /// A component for a PurrNet synced StatusManager. The server runs all of the status effect
    /// logic and clients mirror the resulting status effects.
    /// </summary>
    /// <remarks>
    /// Status effects are synced by the ids assigned in the <see cref="StatusRegistry"/>, so the server and
    /// clients must have the same registered assets. A client logs an error when its registry differs. The
    /// server sends the full list of status effects to everyone whenever one changes, and clients compare
    /// it against what they have. Times are sent as seconds elapsed, so a client is off by about the latency.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StatusManager))]
    [AddComponentMenu("PurrNet/Network Status Manager")]
    public class NetworkStatusManager : NetworkBehaviour, INetworkStatusManager
    {
        public event Action<StatusEffect, StatusEffectAction, int, int> StatusEffectAction
        {
            add => StatusManager.StatusEffectAction += value;
            remove => StatusManager.StatusEffectAction -= value;
        }

        public IEnumerable<StatusEffect> StatusEffects => StatusManager.StatusEffects;

        bool INetworkStatusManager.IsSpawned => isSpawned;
        bool INetworkStatusManager.IsServer => isServer;
        bool INetworkStatusManager.IsOwner => isOwner;

        private StatusManager StatusManager => m_StatusManager ? m_StatusManager : (m_StatusManager = GetComponent<StatusManager>());

        private StatusRegistry m_Registry;
        private StatusManager m_StatusManager;
        private readonly HashSet<uint> m_ListenedEffects = new HashSet<uint>();

        private const string k_SyncError = "The Status Registry is not synced with the server! The Status Effect with id {0} could not be found, so it has failed to get added.";

        #region Network Callbacks
        protected override void OnSpawned(bool asServer)
        {
            m_Registry = StatusRegistry.Instance;

            if (asServer)
            {
                StatusManager.StatusEffectAction += OnStatusEffectForServer;

                // Effects that were added before spawning still need to be synced.
                foreach (var statusEffect in StatusManager.StatusEffects)
                    ListenForDuration(statusEffect);

                SyncEffectsForObservers();
            }
            else if (!isServer)
            {
                // Ask the server for the current list, since the ones sent earlier have old elapsed times.
                RequestEffectsRpc();
            }
        }

        protected override void OnDespawned(bool asServer)
        {
            if (asServer)
                StatusManager.StatusEffectAction -= OnStatusEffectForServer;
        }
        #endregion

        #region Status Manager Methods
        public bool GetStatusEffect(uint instanceId, out StatusEffect statusEffect) => StatusManager.GetStatusEffect(instanceId, out statusEffect);

#nullable enable
        public IEnumerable<StatusEffect> GetStatusEffects(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null, bool matchAllGroups = true) => StatusManager.GetStatusEffects(group, name, data, matchAllGroups);

        public StatusEffect GetFirstStatusEffect(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null, bool matchAllGroups = true) => StatusManager.GetFirstStatusEffect(group, name, data, matchAllGroups);
#nullable restore

        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, int stacks = 1)
        {
            if (!CheckForServer())
                return null;

            return StatusManager.AddStatusEffect(statusEffectData, stacks);
        }

        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, int stacks = 1)
        {
            if (!CheckForServer())
                return null;

            return StatusManager.AddStatusEffect(statusEffectData, duration, stacks);
        }

        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, StatusEvent statusEvent, int stacks = 1)
        {
            if (!CheckForServer())
                return null;

            return StatusManager.AddStatusEffect(statusEffectData, duration, statusEvent, stacks);
        }

        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, Func<bool> predicate, int stacks = 1)
        {
            if (!CheckForServer())
                return null;

            return StatusManager.AddStatusEffect(statusEffectData, predicate, stacks);
        }

        public void RemoveStatusEffect(StatusEffect statusEffect)
        {
            if (!CheckForServer())
                return;

            StatusManager.RemoveStatusEffect(statusEffect);
        }

#nullable enable
        public void RemoveStatusEffect(StatusEffectData statusEffectData, int? stacks = null)
#nullable disable
        {
            if (!CheckForServer())
                return;

            StatusManager.RemoveStatusEffect(statusEffectData, stacks);
        }

        public void RemoveStatusEffect(ComparableName name, int? stacks = null)
        {
            if (!CheckForServer())
                return;

            StatusManager.RemoveStatusEffect(name, stacks);
        }

        public void RemoveStatusEffect(StatusEffectGroup group, int? stacks = null, bool matchAllGroups = true)
        {
            if (!CheckForServer())
                return;

            StatusManager.RemoveStatusEffect(group, stacks, matchAllGroups);
        }

        public void RemoveAllStatusEffects()
        {
            if (!CheckForServer())
                return;

            StatusManager.RemoveAllStatusEffects();
        }
        #endregion

        #region Private Methods
        private bool CheckForServer()
        {
            if (!isSpawned)
                return true;
            if (!isServer)
            {
                Debug.LogError("Please do not try to add or remove Status Effects from non-servers. If this is a Host/Server, double check that this GameObject has a NetworkIdentity component!");
                return false;
            }
            return true;
        }
        #endregion

        #region RPCs
        [ServerRpc(requireOwnership: false)]
        private void RequestEffectsRpc() => SyncEffectsForObservers();

        [ObserversRpc]
        private void SyncEffectsRpc(NetworkStatusEffect[] effects)
        {
            if (!isServer)
                SyncAllForClient(effects);
        }
        #endregion

        #region Server
        private void OnStatusEffectForServer(StatusEffect statusEffect, StatusEffectsFramework.StatusEffectAction action, int previousStacks, int currentStacks)
        {
            if (action is StatusEffectsFramework.StatusEffectAction.AddedStatusEffect)
                ListenForDuration(statusEffect);

            SyncEffectsForObservers();
        }

        private void ListenForDuration(StatusEffect statusEffect)
        {
            // Event timed status effects count down when their event is invoked.
            if (m_ListenedEffects.Add(statusEffect.Id))
                statusEffect.DurationUpdate += (duration) => { if (isSpawned && isServer) SyncEffectsForObservers(); };
        }

        private void SyncEffectsForObservers()
        {
            if (!isSpawned || !isServer)
                return;

            var effects = new List<NetworkStatusEffect>();
            double now = Time.timeAsDouble;

            foreach (var statusEffect in StatusManager.StatusEffects)
            {
                if (!m_Registry.TryGetId(statusEffect.Data, out var dataId))
                {
                    Debug.LogError($"The Status Effect Data \"{statusEffect.Data.name}\" is not in the Status Registry, so it can't be synced to clients.");
                    continue;
                }

                effects.Add(new NetworkStatusEffect(dataId, statusEffect.Timing, statusEffect.Duration, statusEffect.Stacks, statusEffect.Id, (float)(now - statusEffect.TimeAdded)));
            }

            SyncEffectsRpc(effects.ToArray());
        }
        #endregion

        #region Client
        /// <summary>
        /// Makes the local status effects match the given list, keeping the ones that already exist.
        /// </summary>
        private void SyncAllForClient(NetworkStatusEffect[] networkEffects)
        {
            var networkIds = new HashSet<uint>();
            foreach (var networkEffect in networkEffects)
                networkIds.Add(networkEffect.InstanceId);

            foreach (var statusEffect in StatusManager.StatusEffects.Where(effect => !networkIds.Contains(effect.Id)).ToList())
                StatusManager.RemoveStatusEffect(statusEffect);

            foreach (var networkEffect in networkEffects)
                UpdateForClient(networkEffect);
        }

        private void AddForClient(NetworkStatusEffect networkEffect)
        {
            if (m_Registry == null)
                m_Registry = StatusRegistry.Instance;

            if (!m_Registry || !m_Registry.IdToStatusEffectData.TryGetValue(networkEffect.Id, out var statusEffectData))
            {
                Debug.LogError(string.Format(k_SyncError, networkEffect.Id));
                return;
            }

            // Work out when the status effect was added in local time.
            StatusManager.ForceAddStatusEffect(networkEffect.InstanceId, statusEffectData, networkEffect.Timing, Time.timeAsDouble - Math.Max(0f, networkEffect.Elapsed), networkEffect.Duration, networkEffect.Stacks);
        }

        private void UpdateForClient(NetworkStatusEffect networkEffect)
        {
            if (!StatusManager.GetStatusEffect(networkEffect.InstanceId, out var statusEffect))
            {
                AddForClient(networkEffect);
                return;
            }

            if (statusEffect.Duration != networkEffect.Duration)
                statusEffect.Duration = networkEffect.Duration;

            StatusManager.SetStatusEffectStacks(statusEffect, networkEffect.Stacks);
        }
        #endregion
    }
}
