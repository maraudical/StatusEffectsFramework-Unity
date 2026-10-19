using StatusEffectsFramework.Entities;
using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffectsFramework.Samples
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

        private ulong m_StableTypeHash;

        protected virtual void Start()
        {
            m_StableTypeHash = TypeManager.GetTypeInfo<ExamplePlayerComponent>().StableTypeHash;
        }

        private void Update()
        {
            // The world, EntityManager, queries and buffers are looked up every frame instead of being
            // stored, since they become invalid when the world is disposed, such as on a domain reload.
            var world = World.DefaultGameObjectInjectionWorld;

            if (world == null || !world.IsCreated)
                return;

            var manager = world.EntityManager;

            using var registryQuery = manager.CreateEntityQuery(typeof(UnmanagedStatusRegistry));

            if (!registryQuery.TryGetSingleton(out UnmanagedStatusRegistry registry))
                return;

            using var playerQuery = manager.CreateEntityQuery(typeof(ExamplePlayerComponent));
            using var array = playerQuery.ToEntityArray(Allocator.Temp);

            if (array.Length <= 0)
                return;

            var entity = array[0];

            var player = manager.GetComponentData<ExamplePlayerComponent>(entity);

            var statusFloatBuffer = manager.GetBuffer<StatusFloats>(entity, true);
            var statusIntBuffer = manager.GetBuffer<StatusInts>(entity, true);
            var statusBoolBuffer = manager.GetBuffer<StatusBools>(entity, true);

            if (!player.MaxHealth.TryGetElement(m_StableTypeHash, registry, statusFloatBuffer, out var maxHealth)
                || !player.Speed.TryGetElement(m_StableTypeHash, registry, statusFloatBuffer, out var speed)
                || !player.CoinMultiplier.TryGetElement(m_StableTypeHash, registry, statusIntBuffer, out var coinMultiplier)
                || !player.Stunned.TryGetElement(m_StableTypeHash, registry, statusBoolBuffer, out var stunned))
                return;
            
            m_Health.text = player.Health.ToString("0.0");
            m_Health.color = GetColor(maxHealth, player.Health);

            m_MaxHealth.text = maxHealth.Value.ToString("0.0");
            m_MaxHealth.color = GetColor(maxHealth.BaseValue, maxHealth);

            m_Speed.text = speed.Value.ToString("0.0");
            m_Speed.color = GetColor(speed.BaseValue, speed);

            m_CoinMultiplier.text = coinMultiplier.Value.ToString();
            m_CoinMultiplier.color = GetColor(coinMultiplier.BaseValue, coinMultiplier);

            m_Stunned.text = stunned.Value.ToString();
            m_Stunned.color = GetColor(stunned.BaseValue, stunned);
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
