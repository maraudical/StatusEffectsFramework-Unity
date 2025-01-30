#if ENTITIES && ADDRESSABLES
using System;
using System.Collections;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffects.NetCode.Entities.Example.UI
{
    // This would be more optimized and scalable from a SystemBase.
    // For simplicity everything is done in this MonoBehaviour.
    public class ExamplePlayerUIEntities : MonoBehaviour
    {
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

        private EntityManager m_Manager;
        private EntityQuery m_PlayerQuery;
        private DynamicBuffer<StatusFloats> m_StatusFloatBuffer;
        private DynamicBuffer<StatusInts> m_StatusIntBuffer;
        private DynamicBuffer<StatusBools> m_StatusBoolBuffer;

        private IEnumerator Start()
        {
            m_Manager = World.DefaultGameObjectInjectionWorld.EntityManager;
            m_PlayerQuery = m_Manager.CreateEntityQuery(typeof(ExamplePlayer));

            yield return new WaitUntil(() => m_PlayerQuery.TryGetSingletonEntity<ExamplePlayer>(out var entity));
            
            var entity = m_PlayerQuery.GetSingletonEntity();
            var player = m_Manager.GetComponentData<ExamplePlayer>(entity);

            m_StatusFloatBuffer = m_Manager.GetBuffer<StatusFloats>(entity);
            m_StatusIntBuffer = m_Manager.GetBuffer<StatusInts>(entity);
            m_StatusBoolBuffer = m_Manager.GetBuffer<StatusBools>(entity);

            m_BaseHealth = player.Health;

            GetStatusFloat(ref m_BaseMaxHealth, player.ComponentId, player.MaxHealth);
            GetStatusFloat(ref m_BaseSpeed, player.ComponentId, player.Speed);
            GetStatusInt(ref m_BaseCoinMultiplier, player.ComponentId, player.CoinMultiplier);
            GetStatusBool(ref m_BaseStunned, player.ComponentId, player.Stunned);
        }

        private void Update()
        {
            if (!m_PlayerQuery.TryGetSingletonEntity<ExamplePlayer>(out var entity))
                return;
            
            var player = m_Manager.GetComponentData<ExamplePlayer>(entity);

            m_StatusFloatBuffer = m_Manager.GetBuffer<StatusFloats>(entity);
            m_StatusIntBuffer = m_Manager.GetBuffer<StatusInts>(entity);
            m_StatusBoolBuffer = m_Manager.GetBuffer<StatusBools>(entity);

            float maxHealth = default;
            float speed = default;
            int coinMultiplier = default;
            bool stunned = default;

            GetStatusFloat(ref maxHealth, player.ComponentId, player.MaxHealth);
            GetStatusFloat(ref speed, player.ComponentId, player.Speed);
            GetStatusInt(ref coinMultiplier, player.ComponentId, player.CoinMultiplier);
            GetStatusBool(ref stunned, player.ComponentId, player.Stunned);

            m_Health.text = player.Health.ToString("0.0");
            m_Health.color = GetColor(m_BaseHealth, player.Health);

            m_MaxHealth.text = maxHealth.ToString("0.0");
            m_MaxHealth.color = GetColor(m_BaseMaxHealth, maxHealth);

            m_Speed.text = speed.ToString("0.0");
            m_Speed.color = GetColor(m_BaseSpeed, speed);

            m_CoinMultiplier.text = coinMultiplier.ToString();
            m_CoinMultiplier.color = GetColor(m_BaseCoinMultiplier, coinMultiplier);

            m_Stunned.text = stunned.ToString();
            m_Stunned.color = GetColor(m_BaseStunned, stunned);
        }

        private Color GetColor(float origional, float current)
        {
            return current > origional ? Color.green : current < origional ? Color.red : Color.white;
        }

        private Color GetColor(bool origional, bool current)
        {
            return Convert.ToInt32(current) > Convert.ToInt32(origional) ? Color.red : Color.white;
        }

        private void GetStatusFloat(ref float value, in FixedString64Bytes componentId, in FixedString64Bytes id)
        {
            for (int i = 0; i < m_StatusFloatBuffer.Length; i++)
            {
                var statusFloat = m_StatusFloatBuffer[i];
                if (statusFloat.ComponentId == componentId && statusFloat.Id == id)
                {
                    value = statusFloat.Value;
                    break;
                }
            }
        }

        private void GetStatusInt(ref int value, in FixedString64Bytes componentId, in FixedString64Bytes id)
        {
            for (int i = 0; i < m_StatusFloatBuffer.Length; i++)
            {
                var statusInt = m_StatusIntBuffer[i];
                if (statusInt.ComponentId == componentId && statusInt.Id == id)
                {
                    value = statusInt.Value;
                    break;
                }
            }
        }

        private void GetStatusBool(ref bool value, in FixedString64Bytes componentId, in FixedString64Bytes id)
        {
            for (int i = 0; i < m_StatusFloatBuffer.Length; i++)
            {
                var statusBool = m_StatusBoolBuffer[i];
                if (statusBool.ComponentId == componentId && statusBool.Id == id)
                {
                    value = statusBool.Value;
                    break;
                }
            }
        }
    }
}
#endif