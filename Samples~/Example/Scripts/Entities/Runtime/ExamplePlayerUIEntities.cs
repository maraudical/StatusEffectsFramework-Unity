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

            player.MaxHealth.Get(player.ComponentId, m_StatusFloatBuffer, out var maxHealth);
            player.Speed.Get(player.ComponentId, m_StatusFloatBuffer, out var speed);
            player.CoinMultiplier.Get(player.ComponentId, m_StatusIntBuffer, out var coinMultiplier);
            player.Stunned.Get(player.ComponentId, m_StatusBoolBuffer, out var stunned);
            
            m_BaseMaxHealth = maxHealth.BaseValue;
            m_BaseSpeed = speed.BaseValue;
            m_BaseCoinMultiplier = coinMultiplier.BaseValue;
            m_BaseStunned = stunned.BaseValue;
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

            // May want to check for structural changes to the Status Buffers but in this example it is assumed there aren't any.
            player.MaxHealth.GetValue(player.ComponentId, m_StatusFloatBuffer, out var maxHealth);
            player.Speed.GetValue(player.ComponentId, m_StatusFloatBuffer, out var speed);
            player.CoinMultiplier.GetValue(player.ComponentId, m_StatusIntBuffer, out var coinMultiplier);
            player.Stunned.GetValue(player.ComponentId, m_StatusBoolBuffer, out var stunned);

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