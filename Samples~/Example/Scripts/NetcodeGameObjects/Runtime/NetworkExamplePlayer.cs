#if NETCODE_GAMEOBJECTS && COLLECTIONS
using StatusEffects.Example;
using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

namespace StatusEffects.NetCode.GameObjects.Example
{
    [RequireComponent(typeof(NetworkStatusManager))]
    public class NetworkExamplePlayer : NetworkBehaviour, IExamplePlayer
    {
        [NonSerialized] public NetworkStatusManager StatusManager;
        // Note that instead of regular StatusFloats, StatusInts, etc. we use
        // network versions of them.
        public NetworkStatusFloat StatusMaxHealth = new NetworkStatusFloat(100, true);
        public NetworkStatusFloat StatusSpeed = new NetworkStatusFloat(5, true);
        public NetworkStatusInt StatusCoinMultiplier = new NetworkStatusInt(1, true);
        public NetworkStatusBool StatusStunned = new NetworkStatusBool(false);
        
        public float Health { get => m_Health; set => m_Health = value; }
        [SerializeField] private float m_Health;
        
        public virtual float MaxHealth => StatusMaxHealth.Value;
        public virtual float Speed => StatusSpeed.Value;
        public virtual int CoinMultiplier => StatusCoinMultiplier.Value;
        public virtual bool Stunned => StatusStunned.Value;
        
        [Header("Debug Variables")]
        // Instead of directly adding the StatusEffectData, it needs to be an
        // Addressable Asset Reference. This is so the network only needs to
        // sync the StatusEffectData scriptable object reference asset GUID.
        public StatusEffectData StatusEffectData
        {
            get => m_StatusEffectData;
            set => m_StatusEffectData = value;
        }
        [Header("Debug Variables")]
        [SerializeField] private StatusEffectData m_StatusEffectData;
        [SerializeField] private float Duration = 10;
        [SerializeField] private StatusEffectGroup Group;
        [SerializeField] private int Stack = 1;
        [SerializeField] private bool PredicateBool;

        private UnityEvent Event;

        private void Awake()
        {
            // Obtain the NetworkStatusManager
            StatusManager = GetComponent<NetworkStatusManager>();
            StatusMaxHealth.SetManager(StatusManager);
            StatusSpeed.SetManager(StatusManager);
            StatusCoinMultiplier.SetManager(StatusManager);
            StatusStunned.SetManager(StatusManager);
        }
        
        private void OnEnable()
        {
            StatusManager.OnStatusEffect += OnStatusEffect;
        }

        private void OnDisable()
        {
            StatusManager.OnStatusEffect -= OnStatusEffect;
        }

        private void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int previousStacks, int currentStacks)
        {
            Debug.Log($"{(action is StatusEffectAction.AddedStatusEffect or StatusEffectAction.AddedStacks ? "Added" : "Removed")} {Mathf.Abs(currentStacks - previousStacks)} stacks of the effect \"{statusEffect.Data.name}\"!");
        }

        private void Start()
        {
            Health = StatusMaxHealth.Value;
            Event = new();
            PredicateBool = false;
        }
        // Notice how many of the adding and removing methods are awaitable.
        // This is because it needs to make sure that it loads the addressable
        // asset before handling the adding logic. In addition to that, the
        // manipulating of status effects is completely SERVER AUTHORITATIVE.
        // So invoking these methods will throw an error if not done from the
        // server.
        public void DebugAddStatusEffect() { StatusManager?.AddStatusEffect(StatusEffectData, Stack); }
        public void DebugAddStatusEffectTimed() { StatusManager?.AddStatusEffect(StatusEffectData, Duration, Stack); }
        public void DebugAddStatusEffectTimedEvent() { StatusManager?.AddStatusEffect(StatusEffectData, Duration, Event, 1, Stack); }
        public void InvokeEvent() { Event?.Invoke(); }
        public void DebugAddStatusEffectPredicate() { StatusManager?.AddStatusEffect(StatusEffectData, () => PredicateBool, Stack); }
        public void DebugRemoveStatusEffect() { StatusManager?.RemoveStatusEffect(StatusEffectData, Stack); }
        public void DebugRemoveStatusEffectGroup() { StatusManager?.RemoveStatusEffect(Group); }
    }
}
#endif