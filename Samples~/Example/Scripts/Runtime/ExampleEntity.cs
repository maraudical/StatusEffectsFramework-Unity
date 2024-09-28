using System;
using UnityEngine;
using UnityEngine.Events;

namespace StatusEffects.Example
{
    // Require the StatusEffectsInstance so that StatusVariables can be setup.
    [RequireComponent(typeof(StatusManager))]
    public class ExampleEntity : MonoBehaviour, IExampleEntity
    {
        [NonSerialized] public StatusManager StatusManager;
        // Example variables
        [SerializeField] private StatusFloat m_MaxHealth = new StatusFloat(100, true);
        [SerializeField] private StatusFloat m_Speed = new StatusFloat(5, true);
        [SerializeField] private StatusInt m_CoinMultiplier = new StatusInt(1, true);
        [SerializeField] private StatusBool m_Stunned = new StatusBool(false);
        // Note that health would not be a StatusVariable because its
        // value shouldn't dynamically update with effects.
        [property: SerializeField] public float Health { get; set; }
        // To make it easier for other scripts to access StatusVariables
        // you can publicize them like this.
        public virtual float MaxHealth => m_MaxHealth.Value;
        public virtual float Speed => m_Speed.Value;
        public virtual int CoinMultiplier => m_CoinMultiplier.Value;
        public virtual bool Stunned => m_Stunned.Value;
        // The following is for debugging how effects are added/removed.
        [Header("Debug Variables")]
        public StatusEffectData StatusEffectData;
        [SerializeField] protected float Duration = 10;
        [SerializeField] protected StatusEffectGroup Group;
        [SerializeField] protected int Stack = 1;
        // See the DebugAddStatusEffectPredicate method for how a predicate
        // can be used to stop an effect.
        [SerializeField] protected bool PredicateBool;
        // See the DebugAddStatusEffectTimedEvent method for how events can
        // be used to update the duration of an effect.
        protected UnityEvent Event;

        private void Awake()
        {
            // Obtain the StatusManager, this doesn't need to be on the
            // same GameObject but you need to get the reference for whatever
            // you want to store this Entity's status effects.
            StatusManager = GetComponent<StatusManager>();
            m_MaxHealth.SetManager(StatusManager);
            m_Speed.SetManager(StatusManager);
            m_CoinMultiplier.SetManager(StatusManager);
            m_Stunned.SetManager(StatusManager);
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
            Health = m_MaxHealth.Value;
            Event = new();
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
        public void DebugRemoveStatusEffectGroup() { StatusManager.RemoveAllStatusEffects(Group); }
    }
}
