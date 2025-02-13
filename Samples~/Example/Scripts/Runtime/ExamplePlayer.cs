#if ENTITIES
using StatusEffects.Entities;
using Unity.Entities;
using Hash128 = Unity.Entities.Hash128;
#endif
using System;
using UnityEngine;
using UnityEngine.Events;

namespace StatusEffects.Example
{
    // Require the StatusEffectsInstance so that StatusVariables can be setup.
    [RequireComponent(typeof(StatusManager))]
    public class ExamplePlayer : MonoBehaviour, IExamplePlayer
    // The following block is for strictly when using the ECS framework.
#if ENTITIES
            , IEntityStatus
    {
        public Hash128 ComponentId => m_ComponentId;
        private Hash128 m_ComponentId = new Hash128(Guid.NewGuid().ToString("N"));

        public void OnBake(Entity entity, StatusManagerBaker baker)
        {
            baker.DependsOn(this);
            baker.DependsOn(StatusMaxHealth.StatusName);
            baker.DependsOn(StatusSpeed.StatusName);
            baker.DependsOn(StatusCoinMultiplier.StatusName);
            baker.DependsOn(StatusStunned.StatusName);
            baker.AppendToBuffer(entity, new StatusFloats(ComponentId, StatusMaxHealth));
            baker.AppendToBuffer(entity, new StatusFloats(ComponentId, StatusSpeed));
            baker.AppendToBuffer(entity, new StatusInts(ComponentId, StatusCoinMultiplier));
            baker.AppendToBuffer(entity, new StatusBools(ComponentId, StatusStunned));
        }
#else
    {
#endif
        [NonSerialized] public StatusManager StatusManager;
        // Example variables
        public StatusFloat StatusMaxHealth = new StatusFloat(100, true);
        public StatusFloat StatusSpeed = new StatusFloat(5, true);
        public StatusInt StatusCoinMultiplier = new StatusInt(1, true);
        public StatusBool StatusStunned = new StatusBool(false);
        // Note that health would not be a StatusVariable because its
        // value shouldn't dynamically update with effects.
        public float Health { get => m_Health; set => m_Health = value; }
        [SerializeField] private float m_Health;
        // To make it easier for other scripts to access StatusVariables
        // you can publicize them like this.
        public virtual float MaxHealth => StatusMaxHealth.Value;
        public virtual float Speed => StatusSpeed.Value;
        public virtual int CoinMultiplier => StatusCoinMultiplier.Value;
        public virtual bool Stunned => StatusStunned.Value;

        // The following is for debugging how effects are added/removed.
        [Header("Debug Variables")]
        public StatusEffectData StatusEffectData;
        [SerializeField] private float Duration = 10;
        [SerializeField] private StatusEffectGroup Group;
        [SerializeField] private int Stack = 1;
        // See the DebugAddStatusEffectPredicate method for how a predicate
        // can be used to stop an effect.
        [SerializeField] private bool PredicateBool;
        // See the DebugAddStatusEffectTimedEvent method for how events can
        // be used to update the duration of an effect.
        private UnityEvent Event;

        private void Awake()
        {
            // Obtain the StatusManager, this doesn't need to be on the
            // same GameObject but you need to get the reference for whatever
            // you want to store this Entity's status effects.
            StatusManager = GetComponent<StatusManager>();
            StatusMaxHealth.SetManager(StatusManager);
            StatusSpeed.SetManager(StatusManager);
            StatusCoinMultiplier.SetManager(StatusManager);
            StatusStunned.SetManager(StatusManager);
        }
        // I subscribe to the onStatusEffect event just to debug
        private void OnEnable()
        {
            StatusManager.OnStatusEffect += OnStatusEffect;
        }

        private void OnDisable()
        {
            StatusManager.OnStatusEffect -= OnStatusEffect;
        }

        private void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int stacks)
        {
            Debug.Log($"{(action is StatusEffectAction.AddedStatusEffect or StatusEffectAction.AddedStacks ? "Added" : "Removed")} {stacks} stacks of the effect \"{statusEffect.Data.name}\"!");
        }

        private void Start()
        {
            Health = StatusMaxHealth.Value;
            PredicateBool = false;
        }
        // Default adding of status effect is infinite.
        public void DebugAddStatusEffect() { StatusManager.AddStatusEffect(StatusEffectData, Stack); }
        // But you can set an effect duration.
        public void DebugAddStatusEffectTimed() { StatusManager.AddStatusEffect(StatusEffectData, Duration, Stack); }
        // Additionally you can have the duration update of System.Action
        // events where each invoke reduces duration by 1. This could be used
        // for games that are more round based or don't work in realtime.
        public void DebugAddStatusEffectTimedEvent() { StatusManager.AddStatusEffect(StatusEffectData, Duration, Event, 1, Stack); }
        // Just calls the example action.
        public void InvokeEvent() { Event?.Invoke(); }
        // Set a predicate that when true disables the effect. In this example
        // if _predicateBool is set to true the effect is removed.
        public void DebugAddStatusEffectPredicate() { StatusManager.AddStatusEffect(StatusEffectData, () => PredicateBool, Stack); }
        // Default removing of a status effect. There are multiple overrides
        // such as using status effect string names or the StatusEffect
        // reference itself instead.
        public void DebugRemoveStatusEffect() { StatusManager.RemoveStatusEffect(StatusEffectData, Stack); }
        // Removes all effects that fall under a specific group. Additionally
        // you can remove the group parameter and just remove all effects.
        public void DebugRemoveStatusEffectGroup() { StatusManager.RemoveStatusEffect(Group); }
    }
}
