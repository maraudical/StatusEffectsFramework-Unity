#if ENTITIES
using System;
using System.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;
#if NETCODE_ENTITIES
using Unity.Collections;
using Unity.NetCode;
#endif

namespace StatusEffects.Entities.Example.UI
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

        private int m_MaxHealthIndex;
        private int m_SpeedIndex;
        private int m_CoinMultiplierIndex;
        private int m_StunnedIndex;
        
        private float m_BaseMaxHealth;
        private float m_BaseSpeed;
        private int m_BaseCoinMultiplier;
        private bool m_BaseStunned;

        private EntityManager m_Manager;
        private EntityQuery m_PlayerQuery;
        private DynamicBuffer<StatusFloats> m_StatusFloatBuffer;
        private DynamicBuffer<StatusInts> m_StatusIntBuffer;
        private DynamicBuffer<StatusBools> m_StatusBoolBuffer;
#if NETCODE_ENTITIES

        private bool m_Initialized;
#endif

        private IEnumerator Start()
        {
#if NETCODE_ENTITIES
            yield return new WaitUntil(() => ClientServerBootstrap.HasClientWorlds || ClientServerBootstrap.HasServerWorld);

            if (ClientServerBootstrap.HasServerWorld)
                m_Manager = ClientServerBootstrap.ServerWorld.EntityManager;
            else
                m_Manager = ClientServerBootstrap.ClientWorld.EntityManager;

            m_Initialized = true;
#else
            m_Manager = World.DefaultGameObjectInjectionWorld.EntityManager;
#endif
            m_PlayerQuery = m_Manager.CreateEntityQuery(typeof(ExamplePlayer));

            yield return new WaitUntil(() => m_PlayerQuery.TryGetSingletonEntity<ExamplePlayer>(out var entity));
            
            var entity = m_PlayerQuery.GetSingletonEntity();
            var player = m_Manager.GetComponentData<ExamplePlayer>(entity);

            m_StatusFloatBuffer = m_Manager.GetBuffer<StatusFloats>(entity);
            m_StatusIntBuffer = m_Manager.GetBuffer<StatusInts>(entity);
            m_StatusBoolBuffer = m_Manager.GetBuffer<StatusBools>(entity);

            m_MaxHealthIndex = player.MaxHealth.GetBufferIndex(player.ComponentId, m_StatusFloatBuffer);
            m_SpeedIndex = player.Speed.GetBufferIndex(player.ComponentId, m_StatusFloatBuffer);
            m_CoinMultiplierIndex = player.CoinMultiplier.GetBufferIndex(player.ComponentId, m_StatusIntBuffer);
            m_StunnedIndex = player.Stunned.GetBufferIndex(player.ComponentId, m_StatusBoolBuffer);
            
            m_BaseMaxHealth = m_StatusFloatBuffer[m_MaxHealthIndex].BaseValue;
            m_BaseSpeed = m_StatusFloatBuffer[m_SpeedIndex].BaseValue;
            m_BaseCoinMultiplier = m_StatusIntBuffer[m_CoinMultiplierIndex].BaseValue;
            m_BaseStunned = m_StatusBoolBuffer[m_StunnedIndex].BaseValue;
        }

        private void Update()
        {
#if NETCODE_ENTITIES
            if (!m_Initialized)
                return;

#endif
            if (!m_PlayerQuery.TryGetSingletonEntity<ExamplePlayer>(out var entity))
                return;
            
            var player = m_Manager.GetComponentData<ExamplePlayer>(entity);

            m_StatusFloatBuffer = m_Manager.GetBuffer<StatusFloats>(entity);
            m_StatusIntBuffer = m_Manager.GetBuffer<StatusInts>(entity);
            m_StatusBoolBuffer = m_Manager.GetBuffer<StatusBools>(entity);

            float maxHealth = m_StatusFloatBuffer[m_MaxHealthIndex].Value;
            float speed = m_StatusFloatBuffer[m_SpeedIndex].Value;
            int coinMultiplier = m_StatusIntBuffer[m_CoinMultiplierIndex].Value;
            bool stunned = m_StatusBoolBuffer[m_StunnedIndex].Value;

            m_Health.text = player.Health.ToString("0.0");
            m_Health.color = GetColor(m_BaseMaxHealth, player.Health);

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
            // Fix floating point errors.
            float roundedOrigional = Mathf.Round(origional * 10f) / 10f;
            float roundedCurrent = Mathf.Round(current * 10f) / 10f;
            return roundedCurrent > roundedOrigional ? Color.green : roundedCurrent < roundedOrigional ? Color.red : Color.white;
        }

        private Color GetColor(bool origional, bool current)
        {
            return Convert.ToInt32(current) > Convert.ToInt32(origional) ? Color.red : Color.white;
        }
    }
}
#endif