using System;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffects.Example.UI
{
    public class ExampleEntityUI : MonoBehaviour
    {
        [SerializeField] private StatusManager _statusManager;
        [SerializeField] private ExampleEntity _exampleEntity;
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
            _baseHealth = _exampleEntity.maxHealth;
            _baseMaxHealth = _exampleEntity.maxHealth;
            _baseSpeed = _exampleEntity.speed;
            _baseCoinMultiplier = _exampleEntity.coinMultiplier;
            _baseStunned = _exampleEntity.stunned;
        }

        private void OnEnable()
        {
            _statusManager.onStatusEffect += OnStatusEffect;
        }

        private void OnDisable()
        {
            _statusManager.onStatusEffect -= OnStatusEffect;
        }

        public void OnStatusEffect(StatusEffect statusEffect, bool added, int stacks)
        {
            _maxHealth.text = _exampleEntity.maxHealth.ToString("0.0");
            _maxHealth.color = GetColor(_baseMaxHealth, _exampleEntity.maxHealth);

            _speed.text = _exampleEntity.speed.ToString("0.0");
            _speed.color = GetColor(_baseSpeed, _exampleEntity.speed);

            _coinMultiplier.text = _exampleEntity.coinMultiplier.ToString();
            _coinMultiplier.color = GetColor(_baseCoinMultiplier, _exampleEntity.coinMultiplier);

            _stunned.text = _exampleEntity.stunned.ToString();
            _stunned.color = GetColor(_baseStunned, _exampleEntity.stunned);
        }

        private void Update()
        {
            _health.text = _exampleEntity.health.ToString("0.0");
            _health.color = GetColor(_baseHealth, _exampleEntity.health);
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
