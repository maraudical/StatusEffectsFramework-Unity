using UnityEngine;
using UnityEngine.Events;

namespace StatusEffects.Example
{
    // Require the StatusEffectsInstance so that StatusVariables can be setup.
    [RequireComponent(typeof(StatusManager))]
    public class ExampleEntity : MonoBehaviour
    {
        [HideInInspector] public StatusManager statusManager;
        // Example variables
        [SerializeField] private StatusFloat _maxHealth;
        [SerializeField] private StatusFloat _speed;
        [SerializeField] private StatusInt _coinMultiplier;
        [SerializeField] private StatusBool _stunned;
        // Note that health would not be a StatusVariable because its
        // value shouldn't dynamically update with effects.
        public float health;
        // To make it easier for other scripts to access StatusVariables
        // you can publicize them like this.
        public float maxHealth => _maxHealth.value;
        public float speed => _speed.value;
        public int coinMultiplier => _coinMultiplier.value;
        public bool stunned => _stunned.value;
        // The following is for debugging how effects are added/removed.
        [Header("Debug Variables")]
        public StatusEffectData statusEffectData;
        [SerializeField] private float _duration = 10;
        [SerializeField] private StatusEffectGroup _group;
        [SerializeField] private int _stack = 1;
        // See the DebugAddStatusEffectPredicate method for how a predicate
        // can be used to stop an effect.
        [SerializeField] private bool _predicateBool;
        // See the DebugAddStatusEffectTimedEvent method for how events can
        // be used to update the duration of an effect.
        private UnityEvent _event;

        private void Awake()
        {
            // Obtain the StatusEffectsInstance, this doesn't need to be on the
            // same GameObject but you need to get the reference for whatever you
            // want to store this Entity's Status Effects.
            statusManager = GetComponent<StatusManager>();
            _maxHealth.SetInstance(statusManager);
            _speed.SetInstance(statusManager);
            _coinMultiplier.SetInstance(statusManager);
            _stunned.SetInstance(statusManager);
        }
        // I subscribe to the onStatusEffect event just to debug
        private void OnEnable()
        {
            statusManager.onStatusEffect += OnStatusEffect;
        }

        private void OnDisable()
        {
            statusManager.onStatusEffect -= OnStatusEffect;
        }

        private void OnStatusEffect(StatusEffect statusEffect, bool added, int stacks)
        {
            Debug.Log($"{(added ? "Added" : "Removed")} {stacks} stacks of the effect \"{statusEffect.data.name}\"!");
        }

        private void Start()
        {
            health = _maxHealth.value;
            _event = new();
            _predicateBool = false;
        }
        // Default adding of status effect is infinite.
        public void DebugAddStatusEffect() { statusManager.AddStatusEffect(statusEffectData, _stack); }
        // But you can set an effect duration.
        public void DebugAddStatusEffectTimed() { statusManager.AddStatusEffect(statusEffectData, _duration, _stack); }
        // Additionally you can have the duration update of System.Action
        // events where each invoke reduces duration by 1. This could be used
        // for games that are more round based or don't work in realtime.
        public void DebugAddStatusEffectTimedEvent() { statusManager.AddStatusEffect(statusEffectData, _duration, _event, 1, _stack); }
        // Just calls the example action.
        public void InvokeEvent() { _event?.Invoke(); }
        // Set a predicate that when true disables the effect. In this example
        // if _predicateBool is set to true the effect is removed.
        public void DebugAddStatusEffectPredicate() { statusManager.AddStatusEffect(statusEffectData, () => _predicateBool, _stack); }
        // Default removing of a status effect. There are multiple overrides
        // such as using status effect string names or the StatusEffect
        // reference itself instead.
        public void DebugRemoveStatusEffect() { statusManager.RemoveStatusEffect(statusEffectData, _stack); }
        // Removes all effects that fall under a specific group. Additionally
        // you can remove the group parameter and just remove all effects.
        public void DebugRemoveStatusEffectGroup() { statusManager.RemoveAllStatusEffects(_group); }
    }
}
