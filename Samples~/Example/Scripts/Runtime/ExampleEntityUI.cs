using System;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffects.Example.UI
{
    public class ExampleEntityUI : MonoBehaviour
    {
        [SerializeField] private ExampleEntity entity;
        [SerializeField] private Text _health;
        [SerializeField] private Text _maxHealth;
        [SerializeField] private Text _speed;
        [SerializeField] private Text _coinMultiplier;
        [SerializeField] private Text _stunned;

        private float _baseHealth;
        private float _baseMaxHealth;
        private float _baseSpeed;
        private int _baseCoinMultiplier;
        private bool _baseStunned;

        private void Start()
        {
            _baseHealth = entity.maxHealth;
            _baseMaxHealth = entity.maxHealth;
            _baseSpeed = entity.speed;
            _baseCoinMultiplier = entity.coinMultiplier;
            _baseStunned = entity.stunned;
        }

        private void OnEnable()
        {
            entity.onStatusEffect += OnStatusEffect;
        }

        private void OnDisable()
        {
            entity.onStatusEffect -= OnStatusEffect;
        }

        public void OnStatusEffect(StatusEffect statusEffect, bool added, int stacks)
        {
            _maxHealth.text = entity.maxHealth.ToString("0.0");
            _maxHealth.color = GetColor(_baseMaxHealth, entity.maxHealth);

            _speed.text = entity.speed.ToString("0.0");
            _speed.color = GetColor(_baseSpeed, entity.speed);

            _coinMultiplier.text = entity.coinMultiplier.ToString();
            _coinMultiplier.color = GetColor(_baseCoinMultiplier, entity.coinMultiplier);

            _stunned.text = entity.stunned.ToString();
            _stunned.color = GetColor(_baseStunned, entity.stunned);
        }

        private void Update()
        {
            _health.text = entity.health.ToString("0.0");
            _health.color = GetColor(_baseHealth, entity.health);
        }

        private Color GetColor(float origional, float current)
        {
            return current > origional ? Color.green : current < origional ? Color.red : Color.white;
        }

        private Color GetColor(bool origional, bool current)
        {
            return Convert.ToInt32(current) > Convert.ToInt32(origional) ? Color.red : Color.white;
        }
    }
}
