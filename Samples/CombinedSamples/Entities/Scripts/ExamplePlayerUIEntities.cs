using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffectsFramework.Entities.Samples
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

        private TypeIndex m_TypeIndex;
        private EntityManager m_Manager;
        private EntityQuery m_PlayerQuery;
        private DynamicBuffer<StatusFloats> m_StatusFloatBuffer;
        private DynamicBuffer<StatusInts> m_StatusIntBuffer;
        private DynamicBuffer<StatusBools> m_StatusBoolBuffer;

        protected virtual void Start()
        {
            m_TypeIndex = TypeManager.GetTypeIndex<ExamplePlayerComponent>();

            m_Manager = World.DefaultGameObjectInjectionWorld.EntityManager;

            m_PlayerQuery = m_Manager.CreateEntityQuery(typeof(ExamplePlayerComponent));
        }

        private void Update()
        {
            using var array = m_PlayerQuery.ToEntityArray(Allocator.Temp);

            if (array.Length <= 0)
                return;

            var entity = array[0];
            
            var player = m_Manager.GetComponentData<ExamplePlayerComponent>(entity);

            m_StatusFloatBuffer = m_Manager.GetBuffer<StatusFloats>(entity);
            m_StatusIntBuffer = m_Manager.GetBuffer<StatusInts>(entity);
            m_StatusBoolBuffer = m_Manager.GetBuffer<StatusBools>(entity);

            // May want to check for structural changes to the Status Buffers but in this example it is assumed there aren't any.
            if (!player.MaxHealth.TryGetElement(m_TypeIndex, m_StatusFloatBuffer, out var maxHealth)
                || !player.Speed.TryGetElement(m_TypeIndex, m_StatusFloatBuffer, out var speed)
                || !player.CoinMultiplier.TryGetElement(m_TypeIndex, m_StatusIntBuffer, out var coinMultiplier)
                || !player.Stunned.TryGetElement(m_TypeIndex, m_StatusBoolBuffer, out var stunned))
                return;

            m_Health.text = player.Health.ToString("0.0");
            m_Health.color = GetColor(maxHealth.Value, player.Health);

            m_MaxHealth.text = maxHealth.Value.ToString("0.0");
            m_MaxHealth.color = GetColor(maxHealth.BaseValue, maxHealth.Value);

            m_Speed.text = speed.Value.ToString("0.0");
            m_Speed.color = GetColor(speed.BaseValue, speed.Value);

            m_CoinMultiplier.text = coinMultiplier.Value.ToString();
            m_CoinMultiplier.color = GetColor(coinMultiplier.BaseValue, coinMultiplier.Value);

            m_Stunned.text = stunned.Value.ToString();
            m_Stunned.color = GetColor(stunned.BaseValue, stunned.Value);
        }

        private Color GetColor(float original, float current)
        {
            // Fix floating point errors.
            float roundedOrigional = Mathf.Round(original * 10f) / 10f;
            float roundedCurrent = Mathf.Round(current * 10f) / 10f;
            return roundedCurrent > roundedOrigional ? Color.green : roundedCurrent < roundedOrigional ? Color.red : Color.white;
        }

        private Color GetColor(bool original, bool current)
        {
            return Convert.ToInt32(current) > Convert.ToInt32(original) ? Color.red : Color.white;
        }
    }
}