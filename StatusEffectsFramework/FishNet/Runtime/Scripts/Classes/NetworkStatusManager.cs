using System;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using UnityEngine;

namespace StatusEffectsFramework.FishNet
{
    /// <summary>
    /// A component for a FishNet synced StatusManager. The server runs all of the status effect
    /// logic and clients mirror the resulting status effects.
    /// </summary>
    /// <remarks>
    /// Status effects are synced by the ids assigned in the <see cref="StatusRegistry"/>, so the server and
    /// clients must have the same registered assets. A client logs an error when its registry differs. Status
    /// variables are matched between the server and clients by the order they call SetManager(), so call it in
    /// the same order everywhere.
    /// </remarks>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StatusManager))]
    [AddComponentMenu("FishNet/Network Status Manager")]
    public class NetworkStatusManager : NetworkBehaviour, INetworkStatusManager
    {
        public event Action<StatusEffect, StatusEffectAction, int, int> StatusEffectAction
        {
            add => StatusManager.StatusEffectAction += value;
            remove => StatusManager.StatusEffectAction -= value;
        }

        public IEnumerable<StatusEffect> StatusEffects => StatusManager.StatusEffects;

        bool INetworkStatusManager.IsSpawned => IsSpawned;
        bool INetworkStatusManager.IsServer => IsServerStarted;
        bool INetworkStatusManager.IsOwner => IsOwner;

        private StatusManager StatusManager => m_StatusManager ? m_StatusManager : (m_StatusManager = GetComponent<StatusManager>());

        private StatusRegistry m_Registry;
        private StatusManager m_StatusManager;
        private readonly SyncList<NetworkStatusEffect> m_NetworkEffects = new SyncList<NetworkStatusEffect>();
        private readonly List<SyncStatusVariable> m_Variables = new List<SyncStatusVariable>();

        private const string k_SyncError = "The Status Registry is not synced with the server! The Status Effect with id {0} could not be found, so it has failed to get added.";

        /// <summary>
        /// The time on the network clock, in seconds. Clients get the time the server is at.
        /// </summary>
        private double NetworkTime => TimeManager.Tick * TimeManager.TickDelta;

        #region Network Callbacks
        public override void OnStartServer()
        {
            m_Registry = StatusRegistry.Instance;

            // Effects that were added before spawning still need to be synced.
            foreach (var statusEffect in StatusManager.StatusEffects)
                AddNetworkEffect(statusEffect);

            StatusManager.StatusEffectAction += OnStatusEffectForServer;
        }

        public override void OnStopServer()
        {
            StatusManager.StatusEffectAction -= OnStatusEffectForServer;
        }

        public override void OnStartClient()
        {
            m_Registry = StatusRegistry.Instance;

            m_NetworkEffects.OnChange += OnListChangedForClient;

            if (!IsServerStarted)
                SyncAllForClient();
        }

        public override void OnStopClient()
        {
            m_NetworkEffects.OnChange -= OnListChangedForClient;
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

        #region Status Variables
        /// <summary>
        /// Adds a status variable so an owner can submit changes to it. Returns its index.
        /// </summary>
        internal int RegisterVariable(SyncStatusVariable variable)
        {
            int index = m_Variables.IndexOf(variable);
            if (index >= 0)
                return index;

            m_Variables.Add(variable);
            return m_Variables.Count - 1;
        }

        internal void SubmitFloat(int index, StatusFloatState state) => SubmitFloatServerRpc(index, state);
        internal void SubmitInt(int index, StatusIntState state) => SubmitIntServerRpc(index, state);
        internal void SubmitBool(int index, StatusBoolState state) => SubmitBoolServerRpc(index, state);

        [ServerRpc(RequireOwnership = true)]
        private void SubmitFloatServerRpc(int index, StatusFloatState state) => ApplyOwnerState<StatusFloat, StatusFloatState>(index, state);

        [ServerRpc(RequireOwnership = true)]
        private void SubmitIntServerRpc(int index, StatusIntState state) => ApplyOwnerState<StatusInt, StatusIntState>(index, state);

        [ServerRpc(RequireOwnership = true)]
        private void SubmitBoolServerRpc(int index, StatusBoolState state) => ApplyOwnerState<StatusBool, StatusBoolState>(index, state);

        private void ApplyOwnerState<TVar, TState>(int index, in TState state)
            where TVar : StatusVariable<TState>
            where TState : struct, IStatusState<TState>
        {
            if (index < 0 || index >= m_Variables.Count || m_Variables[index] is not SyncStatusVariable<TVar, TState> variable)
            {
                Debug.LogWarning($"A client submitted a value for the Status Variable at index {index}, but it doesn't match a registered {typeof(TVar).Name}. Make sure Status Variables are given their Status Manager in the same order on the server and clients.");
                return;
            }

            if (!variable.OwnerCanWrite)
            {
                Debug.LogWarning($"A client submitted a value for the Status Variable at index {index}, but its write permission only allows the server.");
                return;
            }

            variable.ReceiveFromOwner(state);
        }
        #endregion

        #region Private Methods
        private bool CheckForServer()
        {
            if (!IsSpawned)
                return true;
            if (!IsServerStarted)
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
            double serverTimeAdded = NetworkTime - (Time.timeAsDouble - statusEffect.TimeAdded);

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
            if (!IsSpawned || !IsServerStarted)
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
        /// <summary>
        /// Called as the sync list changes. Comparing against the whole list once a batch of changes is
        /// complete keeps this correct no matter which operations were applied.
        /// </summary>
        private void OnListChangedForClient(SyncListOperation operation, int index, NetworkStatusEffect oldItem, NetworkStatusEffect newItem, bool asServer)
        {
            if (asServer || IsServerStarted)
                return;

            if (operation == SyncListOperation.Complete)
                SyncAllForClient();
        }

        /// <summary>
        /// Makes the local status effects match the sync list, keeping the ones that already exist.
        /// </summary>
        private void SyncAllForClient()
        {
            var networkIds = new HashSet<uint>();
            foreach (var networkEffect in m_NetworkEffects)
                networkIds.Add(networkEffect.InstanceId);

            foreach (var statusEffect in StatusManager.StatusEffects.Where(effect => !networkIds.Contains(effect.Id)).ToList())
                StatusManager.RemoveStatusEffect(statusEffect);

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
            double elapsedTime = Math.Max(0d, NetworkTime - networkEffect.ServerTimeAdded);
            StatusManager.ForceAddStatusEffect(networkEffect.InstanceId, statusEffectData, networkEffect.Timing, Time.timeAsDouble - elapsedTime, networkEffect.Duration, networkEffect.Stacks);
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
