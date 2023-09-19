using StatusEffects;
using System.Collections.Generic;
using UnityEngine;

public class Entity : MonoBehaviour, IStatus
{
    [SerializeField] private StatusFloat _maxHealth;
    [SerializeField] private StatusFloat _speed;
    [SerializeField] private StatusBool _stunned;
    public float health;
    public float maxHealth => _maxHealth.value;
    [Header("Debug")]
    [SerializeField] private StatusEffectData _statusEffectData;
    [SerializeField] private float time;
    [SerializeField, GroupString] private string group;
    [field: Space]
    [field: ContextMenuItem("Add Effect", "DebugAddStatusEffect")]
    [field: ContextMenuItem("Remove Effect", "DebugRemoveStatusEffect")]
    [field: ContextMenuItem("Remove Effect Group", "DebugRemoveStatusEffectGroup")]
    [field: SerializeField] public List<StatusEffect> effects { get; set; }

    private void Start()
    {
        health = _maxHealth.value;
    }

    public void OnStatusEffect(StatusEffect statusEffect, bool active) { }

    void DebugAddStatusEffect()
    {
        this.AddStatusEffect(_statusEffectData, time);
    }
    
    public void DebugRemoveStatusEffect()
    {
        this.RemoveStatusEffect(_statusEffectData);
    }
    
    public void DebugRemoveStatusEffectGroup()
    {
        this.RemoveStatusEffects(group);
    }
}
