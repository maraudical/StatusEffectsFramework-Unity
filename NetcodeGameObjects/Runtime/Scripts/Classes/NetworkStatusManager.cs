#if NETCODE && COLLECTIONS
#if UNITASK
using Cysharp.Threading.Tasks;
#endif
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

using EventType = Unity.Netcode.NetworkListEvent<StatusEffects.NetCode.GameObjects.NetworkStatusEffect>.EventType;

namespace StatusEffects.NetCode.GameObjects
{
    /// <summary>
    /// A component for a network synced StatusManager.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StatusManager))]
    [AddComponentMenu("Netcode/Network Status Manager")]
    public class NetworkStatusManager : NetworkBehaviour, IStatusManager
    {
        [HideInInspector] public event System.Action<StatusEffect, StatusEffectAction, int, int> OnStatusEffect
        {
            add => m_StatusManager.OnStatusEffect += value;
            remove => m_StatusManager.OnStatusEffect -= value;
        }
        event System.Action<StatusEffect> IStatusManager.ValueUpdate
        {
            add => m_StatusManager.ValueUpdate += value;
            remove => m_StatusManager.ValueUpdate -= value;
        }

        public IEnumerable<StatusEffect> Effects => m_StatusManager.Effects;

        private StatusEffectDatabase m_Database;
        private NetworkList<NetworkStatusEffect> m_NetworkEffects;
        private NetworkStatusEffect m_NetworkEffect;
        private StatusEffect m_StatusEffect;
        private StatusEffectData m_StatusEffectData;

        [SerializeField, HideInInspector] private StatusManager m_StatusManager;
        int m_Index;

        private const HideFlags k_HideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        private const string k_SyncError = "Status Effect Database is not synced with the server! Status Effect has failed to get added.";

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
            m_StatusManager.hideFlags = k_HideFlags;
        }

        private void Awake()
        {
            m_Database = StatusEffectDatabase.Get();
            m_NetworkEffects = new();
            
            if (m_StatusManager.hideFlags != k_HideFlags)
                m_StatusManager.hideFlags = k_HideFlags;
        }

        public override void OnNetworkSpawn()
        {
            m_StatusManager.TimerOverride = CreateTimer;
            
            if (IsServer)
                m_StatusManager.OnStatusEffect += OnStatusEffectForServer;
            else
            {
                var listEvent = new NetworkListEvent<NetworkStatusEffect>();
                listEvent.Type = EventType.Full;
                OnListChangedForClient(listEvent);
                m_NetworkEffects.OnListChanged += OnListChangedForClient;
            }

            base.OnNetworkSpawn();
        }

        public override void OnNetworkDespawn()
        {
            base.OnNetworkDespawn();

            m_StatusManager.OnStatusEffect -= OnStatusEffectForServer;
            m_NetworkEffects.OnListChanged -= OnListChangedForClient;
        }

        #region Status Manager Methods
        public bool GetStatusEffect(Hash128 instanceId, out StatusEffect statusEffect) => m_StatusManager.GetStatusEffect(instanceId, out statusEffect);

#nullable enable
        public IEnumerable<StatusEffect> GetStatusEffects(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null) => m_StatusManager.GetStatusEffects(group, name, data);
        
        public StatusEffect GetFirstStatusEffect(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null) => m_StatusManager.GetFirstStatusEffect(group, name, data);
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

        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, UnityEvent unityEvent, float interval = 1, int stacks = 1)
        {
            if (!CheckForServer())
                return null;

            StatusEffect statusEffect = m_StatusManager.AddStatusEffect(statusEffectData, duration, unityEvent, interval, stacks);

            if (statusEffect == null)
                return null;

            statusEffect.OnDurationUpdate += (duration) => OnDurationUpdate(statusEffect.GetInstanceID(), duration);

            return statusEffect;
        }

        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, System.Func<bool> predicate, int stacks = 1)
        {
            if (!CheckForServer())
                return null;

            return m_StatusManager.AddStatusEffect(statusEffectData, predicate, stacks);
        }
        
        public void RemoveStatusEffect(StatusEffectData statusEffectData)
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveStatusEffect(statusEffectData);
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

        public void RemoveStatusEffect(StatusEffectGroup group, int? stacks = null)
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveStatusEffect(group, stacks);
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
            if (!NetworkManager.IsListening)
                return true;
            if (!IsServer)
            {
                Debug.LogError("Please do not try to add or remove Status Effects from non-servers. If this is a Host/Server, double check that this GameObject has a NetworkObject component!");
                return false;
            }
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateTimer(StatusEffect statusEffect, bool remove = true)
        {
#if UNITASK
            statusEffect.TimedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            TimedEffect(statusEffect.TimedTokenSource.Token).Forget();
#elif UNITY_2023_1_OR_NEWER
            statusEffect.TimedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _ = TimedEffect(statusEffect.TimedTokenSource.Token);
#else
            statusEffect.TimedCoroutine = StartCoroutine(TimedEffect());
#endif
            // Timer method
#if UNITASK
            async UniTask TimedEffect(CancellationToken token)
#elif UNITY_2023_1_OR_NEWER
            async Awaitable TimedEffect(CancellationToken token)
#else
            IEnumerator TimedEffect()
#endif
            {
                float startTime = NetworkManager.ServerTime.TimeAsFloat;
                float startDuration = statusEffect.Duration;
                // Basic decreasing timer.
                while (statusEffect.Duration > 0
#if UNITASK || UNITY_2023_1_OR_NEWER
                   && !token.IsCancellationRequested
#endif
                   )
                {
#if UNITASK
                    await UniTask.NextFrame(token);
#elif UNITY_2023_1_OR_NEWER
                    await Awaitable.NextFrameAsync(token);
#else
                    yield return null;
#endif
                    statusEffect.Duration = startDuration - NetworkManager.ServerTime.TimeAsFloat + startTime;
                }

                if (!IsServer)
                    return;

#if UNITASK || UNITY_2023_1_OR_NEWER
                if (!token.IsCancellationRequested)
#endif
                // Once it has ended remove the given effect.
                if (remove)
                    RemoveStatusEffect(statusEffect);
            }
        }
        #endregion

        #region Subscriptions
        private void OnListChangedForClient(NetworkListEvent<NetworkStatusEffect> changeEvent)
        {
            switch (changeEvent.Type)
            {
                case EventType.Add:
                    m_NetworkEffect = changeEvent.Value;
                    if (!m_Database.Values.TryGetValue(Hash128.Parse(m_NetworkEffect.Id.ToString()), out m_StatusEffectData))
                        Debug.LogError(k_SyncError);
                    m_StatusManager.ForceAddStatusEffect(Hash128.Parse(m_NetworkEffect.InstanceId.ToString()), m_StatusEffectData, m_NetworkEffect.Timing, m_NetworkEffect.Duration, m_NetworkEffect.Stacks);
                    break;
                case EventType.Remove:
                    if (m_StatusManager.GetStatusEffect(Hash128.Parse(changeEvent.Value.InstanceId.ToString()), out m_StatusEffect))
                        m_StatusManager.RemoveStatusEffect(m_StatusEffect);
                    break;
                case EventType.Clear:
                    m_StatusManager.RemoveAllStatusEffects();
                    break;
                case EventType.Full:
                    m_StatusManager.RemoveAllStatusEffects();
                    // Re-add all status effects
                    foreach (var effect in m_NetworkEffects)
                    {
                        if (!m_Database.Values.TryGetValue(Hash128.Parse(effect.Id.ToString()), out m_StatusEffectData))
                            Debug.LogError(k_SyncError);
                        m_StatusManager.ForceAddStatusEffect(Hash128.Parse(effect.InstanceId.ToString()), m_StatusEffectData, effect.Timing, effect.Duration, effect.Stacks);
                    }
                    break;
                case EventType.Value:
                    m_NetworkEffect = changeEvent.Value;
                    if (m_StatusManager.GetStatusEffect(Hash128.Parse(m_NetworkEffect.InstanceId.ToString()), out m_StatusEffect))
                    {
                        int stacks = m_NetworkEffect.Stacks - m_StatusEffect.Stacks;
                        m_StatusEffect.Duration = m_NetworkEffect.Duration;
                        if (stacks != 0)
                        {
                            m_StatusEffect.Stacks = m_NetworkEffect.Stacks;
                            m_StatusManager.InvokeValueUpdate(m_StatusEffect);
                            m_StatusEffect.InvokeStackUpdate();
                            m_StatusManager.InvokeOnStatusEffect(m_StatusEffect, stacks >= 0 ? StatusEffectAction.AddedStacks : StatusEffectAction.RemovedStacks, m_StatusEffect.Stacks - stacks, m_StatusEffect.Stacks);
                        }
                    }
                    break;
                default:
                    Debug.LogError($"NetworkList change event {changeEvent.Type} not implemented!");
                    return;
            }
        }

        private void OnStatusEffectForServer(StatusEffect statusEffect, StatusEffectAction action, int previousStacks, int currentStacks)
        {
            switch (action)
            {
                case StatusEffectAction.AddedStatusEffect:
                    m_NetworkEffects.Add(new NetworkStatusEffect(statusEffect.Data.Id, statusEffect.Timing, statusEffect.Duration, statusEffect.Stacks, statusEffect.GetInstanceID()));
                    break;
                case StatusEffectAction.RemovedStatusEffect:
                    m_NetworkEffect.InstanceId = statusEffect.GetInstanceID().ToString();
                    m_NetworkEffects.Remove(m_NetworkEffect);
                    break;
                default:
                    m_NetworkEffect.InstanceId = statusEffect.GetInstanceID().ToString();
                    m_Index = m_NetworkEffects.IndexOf(m_NetworkEffect);
                    m_NetworkEffect = m_NetworkEffects[m_Index];
                    m_NetworkEffect.Stacks = statusEffect.Stacks;
                    m_NetworkEffects[m_Index] = m_NetworkEffect;
                    break;
            }
        }

        private void OnDurationUpdate(Hash128 instanceId, float duration)
        {
            m_NetworkEffect.InstanceId = instanceId.ToString();
            m_Index = m_NetworkEffects.IndexOf(m_NetworkEffect);
            m_NetworkEffect = m_NetworkEffects[m_Index];
            m_NetworkEffect.Duration = duration;
            m_NetworkEffects[m_Index] = m_NetworkEffect;
        }
        #endregion
    }
}
#endif