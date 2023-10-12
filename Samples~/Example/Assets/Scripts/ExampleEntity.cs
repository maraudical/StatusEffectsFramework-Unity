using StatusEffects;
using System.Collections.Generic;
using UnityEngine;

public class ExampleEntity : MonoBehaviour, IStatus
{
    [StatusEffects.Inspector.InfoBox(messageType = 1), SerializeField]
#pragma warning disable CS0414
    private string info = "Please view the code in this script for example implementation!";
#pragma warning restore CS0414
    // Example variables
    [SerializeField] private StatusFloat _maxHealth;
    [SerializeField] private StatusFloat _speed;
    [SerializeField] private StatusBool _stunned;
    [SerializeField] private StatusBool _onFire;
    // Note that health would not be a StatusVariable because its
    // value shouldn't dynamically update with effects.
    public float health;
    // To make it easier for other scripts to access StatusVariables
    // you can publicize them like this.
    public float maxHealth => _maxHealth.value;
    // The following is for debugging how effects are added/removed.
    [Header("Debug")]
    [SerializeField] private StatusEffectData _statusEffectData;
    [SerializeField] private float duration;
    [SerializeField] private StatusEffectGroup group;
    [field: Space]
    // Right click the "effect" list from the inspector to access the
    // context menu methods.
    [field: ContextMenuItem("Add Effect", "DebugAddStatusEffect")]
    [field: ContextMenuItem("Add Effect Timed", "DebugAddStatusEffectTimed")]
    [field: ContextMenuItem("Add Effect Timed Action", "DebugAddStatusEffectTimedAction")]
    [field: ContextMenuItem("Invoke Action", "InvokeAction")]
    [field: ContextMenuItem("Add Effect Predicate", "DebugAddStatusEffectPredicate")]
    [field: ContextMenuItem("Remove Effect", "DebugRemoveStatusEffect")]
    [field: ContextMenuItem("Remove Effect Group", "DebugRemoveStatusEffectGroup")]
    // This is apart of the IStatus interface which is required by all
    // MonoBehaviours which will interact with status effects. It will
    // hold the list of currently active effects and update automatically.
    // DO NOT edit the list directly, see the below extensions for adding/
    // removing effects.
    [field: SerializeField] public List<StatusEffect> effects { get; set; }
    // See the DebugAddStatusEffectTimedAction method for how events can
    // be used to update the duration of an effect.
    private System.Action _action;

    private void Start()
    {
        health = _maxHealth.value;
    }
    // This is also apart of the IStatus interface. It will be invoked
    // whenever status effects are started or ended.
    public void OnStatusEffect(StatusEffect statusEffect, bool started) 
    { 
        Debug.Log($"The effect {statusEffect.data.name} was {(started ? "enabled" : "disabled")}!"); 
    }
    // Default adding of status effect is infinite.
    void DebugAddStatusEffect() { this.AddStatusEffect(_statusEffectData); }
    // But you can set an effect duration.
    public void DebugAddStatusEffectTimed() { this.AddStatusEffect(_statusEffectData, duration); }
    // Additionally you can have the duration update of System.Action
    // events where each invoke reduces duration by 1. This could be used
    // for games that are more round based or don't work in realtime.
    void DebugAddStatusEffectTimedAction() { this.AddStatusEffect(_statusEffectData, duration, ref _action); }
    // Just calls the example action.
    void InvokeAction() { _action?.Invoke(); }
    // Set a predicate that when true disables the effect. In this example
    // if health is set to 5 the effect is removed.
    void DebugAddStatusEffectPredicate() { this.AddStatusEffect(_statusEffectData, () => health == 5); }
    // Default removing of a status effect. There are multiple overrides
    // such as using status effect string names or the StatusEffect
    // reference itself instead.
    void DebugRemoveStatusEffect() { this.RemoveStatusEffect(_statusEffectData); }
    // Removes all effects that fall under a specific group. Additionally
    // you can remove the group parameter and just remove all effects.
    void DebugRemoveStatusEffectGroup() { this.RemoveAllStatusEffects(group); }
}
