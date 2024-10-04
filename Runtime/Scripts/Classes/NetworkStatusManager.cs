#if NETCODE && ADDRESSABLES && (UNITY_2023_1_OR_NEWER || UNITASK)
#if UNITASK
using Cysharp.Threading.Tasks;
#endif
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.Events;
using UnityEngine.AddressableAssets;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Linq;
using EventType = Unity.Netcode.NetworkListEvent<StatusEffects.NetworkStatusEffect>.EventType;

namespace StatusEffects
{
    /// <summary>
    /// A component for a network synced StatusManager.
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(StatusManager))]
    [AddComponentMenu("Netcode/Network Status Manager")]
    public class NetworkStatusManager : NetworkBehaviour, IStatusManager
    {
        [HideInInspector] public event System.Action<StatusEffect, StatusEffectAction, int> OnStatusEffect
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
        /// <summary>
        /// Note that the <see cref="int"/> hash of the NetworkStatusEffect is actually just the hash of the <see cref="NetworkStatusEffect.AssetGuid"/>.
        /// </summary>
        private Dictionary<int, AsyncOperationHandle<StatusEffectData>> m_LoadedDataResourceHandles;
        private Dictionary<StatusEffectData, string> m_DataToGuid;
        
        private NetworkList<NetworkStatusEffect> m_NetworkEffects;
        private NetworkStatusEffect m_NetworkEffect;
        private StatusEffect m_StatusEffect;

        [SerializeField, HideInInspector] private StatusManager m_StatusManager;
        int m_Index;

        private const HideFlags k_HideFlags = HideFlags.HideInInspector | HideFlags.HideInHierarchy;
        private const float k_FlushEverySeconds = 300;

        private void OnValidate()
        {
            if (!m_StatusManager)
                if (!TryGetComponent(out m_StatusManager))
                    m_StatusManager = gameObject.AddComponent<StatusManager>();
            
            if (m_StatusManager.hideFlags != k_HideFlags)
                m_StatusManager.hideFlags = k_HideFlags;
        }

        private void Awake()
        {
            m_NetworkEffects = new();
            m_LoadedDataResourceHandles = new();
            m_DataToGuid = new();
            m_NetworkEffect = new();

#if UNITASK
            FlushUnusedStatusEffectDatas(destroyCancellationToken).Forget();
#else
            _ = FlushUnusedStatusEffectDatas(destroyCancellationToken);
#endif
        }

        public override void OnDestroy()
        {
            base.OnDestroy();
            
            UnloadAndReleaseAllStatusEffectDatas();
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
        /// <inheritdoc cref="StatusManager.GetStatusEffects"/>
#nullable enable
        public IEnumerable<StatusEffect> GetStatusEffects(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null) => m_StatusManager.GetStatusEffects(group, name, data);
#nullable disable
        /// <inheritdoc cref="StatusManager.GetFirstStatusEffect"/>
#nullable enable
        public StatusEffect GetFirstStatusEffect(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null) => m_StatusManager.GetFirstStatusEffect(group, name, data);
#nullable disable
        /// <inheritdoc cref="StatusManager.AddStatusEffect(StatusEffectData, int)"/>
        public async
#if UNITASK
            UniTask<StatusEffect> 
#else
            Awaitable<StatusEffect>
#endif
            AddStatusEffect(AssetReferenceT<StatusEffectData> statusEffectDataReference, int stack = 1)
        {
            if (!CheckForServer())
                return null;
            
            StatusEffect statusEffect = m_StatusManager.AddStatusEffect(await LoadStatusEffectData(statusEffectDataReference.AssetGUID), stack);

            if (statusEffect == null)
                return null;

            return statusEffect;
        }
        /// <inheritdoc cref="StatusManager.AddStatusEffect(StatusEffectData, float, int)"/>
        public async
#if UNITASK
            UniTask<StatusEffect> 
#else
            Awaitable<StatusEffect>
#endif
            AddStatusEffect(AssetReferenceT<StatusEffectData> statusEffectDataReference, float duration, int stack = 1)
        {
            if (!CheckForServer())
                return null;
            
            StatusEffect statusEffect = m_StatusManager.AddStatusEffect(await LoadStatusEffectData(statusEffectDataReference.AssetGUID), duration, stack);
            
            if (statusEffect == null)
                return null;

            return statusEffect;
        }
        /// <inheritdoc cref="StatusManager.AddStatusEffect(StatusEffectData, float, UnityEvent, float, int)"/>
        public async
#if UNITASK
            UniTask<StatusEffect> 
#else
            Awaitable<StatusEffect>
#endif
            AddStatusEffect(AssetReferenceT<StatusEffectData> statusEffectDataReference, float duration, UnityEvent unityEvent, float interval = 1, int stack = 1)
        {
            if (!CheckForServer())
                return null;

            StatusEffect statusEffect = m_StatusManager.AddStatusEffect(await LoadStatusEffectData(statusEffectDataReference.AssetGUID), duration, unityEvent, interval, stack);

            if (statusEffect == null)
                return null;

            statusEffect.OnDurationUpdate += (duration) => OnDurationUpdate(statusEffect.GetInstanceID(), duration);

            return statusEffect;
        }
        /// <inheritdoc cref="StatusManager.AddStatusEffect(StatusEffectData, System.Func{bool}, int)"/>
        public async
#if UNITASK
            UniTask<StatusEffect> 
#else
            Awaitable<StatusEffect>
#endif
            AddStatusEffect(AssetReferenceT<StatusEffectData> statusEffectDataReference, System.Func<bool> predicate, int stack = 1)
        {
            if (!CheckForServer())
                return null;

            StatusEffect statusEffect = m_StatusManager.AddStatusEffect(await LoadStatusEffectData(statusEffectDataReference.AssetGUID), predicate, stack);

            if (statusEffect == null)
                return null;

            return statusEffect;
        }
        /// <inheritdoc cref="StatusManager.RemoveStatusEffect(StatusEffect)"/>
        public async
#if UNITASK
            UniTask 
#else
            Awaitable
#endif
            RemoveStatusEffect(AssetReferenceT<StatusEffectData> statusEffectDataReference)
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveStatusEffect(await LoadStatusEffectData(statusEffectDataReference.AssetGUID));
        }
        /// <inheritdoc cref="StatusManager.RemoveStatusEffect(StatusEffectData, int?)"/>
        public async
#if UNITASK
            UniTask
#else
            Awaitable
#endif
#nullable enable
            RemoveStatusEffect(AssetReferenceT<StatusEffectData> statusEffectDataReference, int? stack = null)
#nullable disable
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveStatusEffect(await LoadStatusEffectData(statusEffectDataReference.AssetGUID), stack);
        }
        /// <inheritdoc cref="StatusManager.RemoveStatusEffect(StatusEffect)"/>
        public void RemoveStatusEffect(StatusEffect statusEffect)
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveStatusEffect(statusEffect);
        }
        /// <inheritdoc cref="StatusManager.RemoveStatusEffect(StatusEffectData, int?)"/>
#nullable enable
        public void RemoveStatusEffect(StatusEffectData statusEffectData, int? stack = null)
#nullable disable
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveStatusEffect(statusEffectData, stack);
        }
        /// <inheritdoc cref="StatusManager.RemoveStatusEffect(ComparableName, int?)"/>
        public void RemoveStatusEffect(ComparableName name, int? stack = null)
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveStatusEffect(name, stack);
        }
        /// <inheritdoc cref="StatusManager.RemoveAllStatusEffects(StatusEffectGroup)"/>
        public void RemoveStatusEffect(StatusEffectGroup group)
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveStatusEffect(group);
        }
        /// <inheritdoc cref="StatusManager.RemoveAllStatusEffects"/>
        public void RemoveAllStatusEffects()
        {
            if (!CheckForServer())
                return;

            m_StatusManager.RemoveAllStatusEffects();
        }
#endregion

        #region Public Methods
        public bool TryGetLoadedDataFromNetworkStatusEffect(in NetworkStatusEffect hash, out AsyncOperationHandle<StatusEffectData> loadedData) => TryGetLoadedDataFromHash(hash.AssetGuid.ToString().GetHashCode(), out loadedData);
        
        public bool TryGetLoadedDataFromHash(int hash, out AsyncOperationHandle<StatusEffectData> loadedData) => m_LoadedDataResourceHandles.TryGetValue(hash, out loadedData);

        public async
#if UNITASK
            UniTask<StatusEffectData> 
#else
            Awaitable<StatusEffectData>
#endif
            LoadStatusEffectData(string assedGuid)
        {
            AsyncOperationHandle<StatusEffectData> handle;
            int hash = assedGuid.GetHashCode();

            if (TryGetLoadedDataFromHash(hash, out handle))
            {
                if (!handle.IsDone)
                    await handle.Task;
            }
            else
            {
                handle = Addressables.LoadAssetAsync<StatusEffectData>(assedGuid);
                m_LoadedDataResourceHandles.Add(hash, handle);
                await handle.Task;
                StatusEffectData data = handle.Result;
                if (m_DataToGuid.TryAdd(data, assedGuid))
                    foreach (var dependency in data.Dependencies)
                        await LoadStatusEffectData(dependency.AssetGUID);
            }

            return handle.Result;
        }

        public void UnloadAndReleaseAllStatusEffectDatas()
        {
            if (m_LoadedDataResourceHandles == null)
                return;

            foreach (var handle in m_LoadedDataResourceHandles.Values)
                handle.Release();

            m_LoadedDataResourceHandles.Clear();
            m_DataToGuid.Clear();
        }
        #endregion

        #region Private Methods
        private bool CheckForServer()
        {
            if (!NetworkManager.IsListening)
            {
                Debug.LogError("Network has not been started. Cannot add or remove Status Effects.");
                return false;
            }
            if (!IsServer)
            {
                Debug.LogError("Please do not try to add or remove Status Effects from non-servers.");
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
#else
            statusEffect.TimedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _ = TimedEffect(statusEffect.TimedTokenSource.Token);
#endif
            // Timer method
#if UNITASK
            async UniTask TimedEffect(CancellationToken token)
#else
            async Awaitable TimedEffect(CancellationToken token)
#endif
            {
                float startTime = NetworkManager.ServerTime.TimeAsFloat;
                float startDuration = statusEffect.Duration;
                // Basic decreasing timer.
                while (statusEffect.Duration > 0 && !token.IsCancellationRequested)
                {
#if UNITASK
                    await UniTask.NextFrame(token);
#else
                    await Awaitable.NextFrameAsync(token);
#endif
                    statusEffect.Duration = startDuration - NetworkManager.ServerTime.TimeAsFloat + startTime;
                }

                if (!IsServer)
                    return;
                
                // Once it has ended remove the given effect.
                if (!token.IsCancellationRequested && remove)
                    RemoveStatusEffect(statusEffect);
            }
        }

        private async
#if UNITASK
            UniTaskVoid
#else
            Awaitable
#endif
            FlushUnusedStatusEffectDatas(CancellationToken token)
        {
            bool found;
            int count;
            KeyValuePair<int, AsyncOperationHandle<StatusEffectData>> kvp;

            for (; ; )
            {
#if UNITASK
                await UniTask.WaitForSeconds(k_FlushEverySeconds, ignoreTimeScale: true, cancellationToken: token);
#else
                await Awaitable.WaitForSecondsAsync(k_FlushEverySeconds, token);
#endif
                if (token.IsCancellationRequested)
                    return;

                count = m_LoadedDataResourceHandles.Count;
                for (int i = count - 1; i >=0; i--)
                {
                    kvp = m_LoadedDataResourceHandles.ElementAt(i);

                    if (!kvp.Value.IsDone)
                        continue;
                    
                    found = false;
                    // Check if loaded handle is still being used
                    foreach (var networkEffect in m_NetworkEffects)
                    {
                        if (networkEffect.AssetGuid.ToString().GetHashCode() == kvp.Key)
                        {
                            found = true;
                            break;
                        }
                    }

                    if (!found)
                    {
                        m_LoadedDataResourceHandles.Remove(kvp.Key);
                        m_DataToGuid.Remove(kvp.Value.Result);
                        kvp.Value.Release();
                    }    

#if UNITASK
                    await UniTask.NextFrame(token);
#else
                    await Awaitable.NextFrameAsync(token);
#endif

                    if (token.IsCancellationRequested)
                        return;
                }
            }
        }
        #endregion

        #region Subscriptions
        private void OnListChangedForClient(NetworkListEvent<NetworkStatusEffect> changeEvent)
        {
#if UNITASK
            OnListChangedForClientAsync(destroyCancellationToken).Forget();
#else
            _ = OnListChangedForClientAsync(destroyCancellationToken);
#endif
#if UNITASK
            async UniTaskVoid OnListChangedForClientAsync(CancellationToken token)
#else
            async Awaitable OnListChangedForClientAsync(CancellationToken token)
#endif
            {
                switch (changeEvent.Type)
                {
                    case EventType.Add:
                        m_NetworkEffect = changeEvent.Value;
                        m_StatusManager.ForceAddStatusEffect(m_NetworkEffect.InstanceId, await LoadStatusEffectData(m_NetworkEffect.AssetGuid.ToString()), m_NetworkEffect.Timing, m_NetworkEffect.Duration, m_NetworkEffect.Stack);
                        break;
                    case EventType.Remove:
                        if (m_StatusManager.GetStatusEffect(changeEvent.Value.InstanceId, out m_StatusEffect))
                            m_StatusManager.RemoveStatusEffect(m_StatusEffect);
                        break;
                    case EventType.Clear:
                        m_StatusManager.RemoveAllStatusEffects();
                        break;
                    case EventType.Full:
                        m_StatusManager.RemoveAllStatusEffects();
                        // Re-add all status effects
                        foreach (var effect in m_NetworkEffects)
                            m_StatusManager.ForceAddStatusEffect(effect.InstanceId, await LoadStatusEffectData(effect.AssetGuid.ToString()), effect.Timing, effect.Duration, effect.Stack);
                        break;
                    case EventType.Value:
                        m_NetworkEffect = changeEvent.Value;
                        if (m_StatusManager.GetStatusEffect(m_NetworkEffect.InstanceId, out m_StatusEffect))
                        {
                            int stacks = m_NetworkEffect.Stack - m_StatusEffect.Stack;
                            m_StatusEffect.Duration = m_NetworkEffect.Duration;
                            m_StatusEffect.Stack = m_NetworkEffect.Stack;
                            m_StatusManager.InvokeValueUpdate(m_StatusEffect);
                            m_StatusEffect.InvokeStackUpdate();
                            m_StatusManager.InvokeOnStatusEffect(m_StatusEffect, stacks >= 0 ? StatusEffectAction.AddedStacks : StatusEffectAction.RemovedStacks, Mathf.Abs(stacks));
                        }
                        break;
                    default:
                        Debug.LogError($"NetworkList change event {changeEvent.Type} not implemented!");
                        return;
                }
            }
        }

        private void OnStatusEffectForServer(StatusEffect statusEffect, StatusEffectAction action, int stack)
        {
            switch (action)
            {
                case StatusEffectAction.AddedStatusEffect:
                    if (m_DataToGuid.TryGetValue(statusEffect.Data, out string assetGuid))
                        m_NetworkEffects.Add(new NetworkStatusEffect(assetGuid, statusEffect.Timing, statusEffect.Duration, statusEffect.Stack, statusEffect.GetInstanceID()));
                    else
                        Debug.LogError($"Missing dependency! Make sure {statusEffect.Data.name} is added to all conditionals that reference it!");
                    break;
                case StatusEffectAction.RemovedStatusEffect:
                    m_NetworkEffect.InstanceId = statusEffect.GetInstanceID();
                    m_NetworkEffects.Remove(m_NetworkEffect);
                    break;
                default:
                    m_NetworkEffect.InstanceId = statusEffect.GetInstanceID();
                    m_Index = m_NetworkEffects.IndexOf(m_NetworkEffect);
                    m_NetworkEffect = m_NetworkEffects[m_Index];
                    m_NetworkEffect.Stack = statusEffect.Stack;
                    m_NetworkEffects[m_Index] = m_NetworkEffect;
                    break;
            }
        }

        private void OnDurationUpdate(int instanceId, float duration)
        {
            m_NetworkEffect.InstanceId = instanceId;
            m_Index = m_NetworkEffects.IndexOf(m_NetworkEffect);
            m_NetworkEffect = m_NetworkEffects[m_Index];
            m_NetworkEffect.Duration = duration;
            m_NetworkEffects[m_Index] = m_NetworkEffect;
        }
#endregion
    }
}
#endif