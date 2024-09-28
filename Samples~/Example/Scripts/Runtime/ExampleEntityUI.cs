using System;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffects.Example.UI
{
    public class ExampleEntityUI : MonoBehaviour
    {
        [SerializeField] private StatusManager m_StatusManager;
        [SerializeField] private Text m_Health;
        [SerializeField] private Text m_MaxHealth;
        [SerializeField] private Text m_Speed;
        [SerializeField] private Text m_CoinMultiplier;
        [SerializeField] private Text m_Stunned;

        private float m_BaseHealth;
        private float m_BaseMaxHealth;
        private float m_BaseSpeed;
        private int m_BaseCoinMultiplier;
        private bool m_BaseStunned;

        private IExampleEntity m_ExampleEntity;

        private void Start()
        {
            m_ExampleEntity = m_StatusManager.GetComponent<IExampleEntity>();
            m_BaseHealth = m_ExampleEntity.MaxHealth;
            m_BaseMaxHealth = m_ExampleEntity.MaxHealth;
            m_BaseSpeed = m_ExampleEntity.Speed;
            m_BaseCoinMultiplier = m_ExampleEntity.CoinMultiplier;
            m_BaseStunned = m_ExampleEntity.Stunned;
        }

        private void Update()
        {
            // Alternatively you can subscribe to each StatusVariable's OnValueUpdate event
            m_Health.text = m_ExampleEntity.Health.ToString("0.0");
            m_Health.color = GetColor(m_BaseHealth, m_ExampleEntity.Health);

            m_MaxHealth.text = m_ExampleEntity.MaxHealth.ToString("0.0");
            m_MaxHealth.color = GetColor(m_BaseMaxHealth, m_ExampleEntity.MaxHealth);

            m_Speed.text = m_ExampleEntity.Speed.ToString("0.0");
            m_Speed.color = GetColor(m_BaseSpeed, m_ExampleEntity.Speed);

            m_CoinMultiplier.text = m_ExampleEntity.CoinMultiplier.ToString();
            m_CoinMultiplier.color = GetColor(m_BaseCoinMultiplier, m_ExampleEntity.CoinMultiplier);

            m_Stunned.text = m_ExampleEntity.Stunned.ToString();
            m_Stunned.color = GetColor(m_BaseStunned, m_ExampleEntity.Stunned);
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
