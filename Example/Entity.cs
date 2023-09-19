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

    public List<StatusEffect> effects { get; set; }

    private void Start()
    {
        health = _maxHealth.GetValue();
    }

    public void OnStatusEffect(StatusEffect statusEffect, bool active) { }
    
    public void DebugAddStatusEffect(StatusEffectData data, float time)
    {
        this.AddStatusEffect(data, time);
    }
    
    public void DebugRemoveStatusEffect(StatusEffectData data)
    {
        this.RemoveStatusEffect(data);
    }
    
    public void DebugRemoveStatusEffectGroup([GroupString] string group)
    {
        this.RemoveStatusEffects(group);
    }
}
