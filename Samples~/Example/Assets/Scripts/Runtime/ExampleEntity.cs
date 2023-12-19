using StatusEffects;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class ExampleEntity : MonoBehaviour, IStatus
{
    // This is an action to subscribe an effect UI to.
    [HideInInspector] public event Action<StatusEffect, bool> onStatusEffect;

    [StatusEffects.Inspector.InfoBox(messageType = 1), SerializeField]
#pragma warning disable CS0414
    private string _info = "Please view the code in this script for example implementation!";
#pragma warning restore CS0414
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
    [field: Space]
    // This is apart of the IStatus interface which is required by all
    // MonoBehaviours which will interact with status effects. It will
    // hold the list of currently active effects and update automatically.
    // DO NOT edit the list directly, see the below extensions for adding/
    // removing effects.
    [field: SerializeField] public List<StatusEffect> effects { get; set; }
    // The following is for debugging how effects are added/removed.
    [Header("Debug Variables")]
    [SerializeField] private StatusEffectData _statusEffectData;
    [SerializeField] private float _duration;
    [SerializeField] private StatusEffectGroup _group;
    // See the DebugAddStatusEffectPredicate method for how a predicate
    // can be used to stop an effect.
    [SerializeField] private bool _predicateBool;
    // See the DebugAddStatusEffectTimedEvent method for how events can
    // be used to update the duration of an effect.
    private UnityEvent _event;

    private void Start()
    {
        health = _maxHealth.value;
        _event = new();
        _predicateBool = false;
    }
    // This is also apart of the IStatus interface. It will be invoked
    // whenever status effects are started or ended.
    public void OnStatusEffect(StatusEffect statusEffect, bool started) 
    { 
        Debug.Log($"The effect {statusEffect.data.name} was {(started ? "enabled" : "disabled")}!"); 
        onStatusEffect?.Invoke(statusEffect, started);
    }
    // Default adding of status effect is infinite.
    public void DebugAddStatusEffect() { this.AddStatusEffect(_statusEffectData); }
    // But you can set an effect duration.
    public void DebugAddStatusEffectTimed() { this.AddStatusEffect(_statusEffectData, _duration); }
    // Additionally you can have the duration update of System.Action
    // events where each invoke reduces duration by 1. This could be used
    // for games that are more round based or don't work in realtime.
    public void DebugAddStatusEffectTimedEvent() { this.AddStatusEffect(_statusEffectData, _duration, _event); }
    // Just calls the example action.
    public void InvokeEvent() { _event?.Invoke(); }
    // Set a predicate that when true disables the effect. In this example
    // if _predicateBool is set to true the effect is removed.
    public void DebugAddStatusEffectPredicate() { this.AddStatusEffect(_statusEffectData, () => _predicateBool); }
    // Default removing of a status effect. There are multiple overrides
    // such as using status effect string names or the StatusEffect
    // reference itself instead.
    public void DebugRemoveStatusEffect() { this.RemoveStatusEffect(_statusEffectData); }
    // Removes all effects that fall under a specific group. Additionally
    // you can remove the group parameter and just remove all effects.
    public void DebugRemoveStatusEffectGroup() { this.RemoveAllStatusEffects(_group); }
}
