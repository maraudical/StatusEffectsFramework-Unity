#if UNITASK
using Cysharp.Threading.Tasks;
#endif
using System.Threading;
using UnityEngine;
using StatusEffects.Example;
#if ENTITIES
using StatusEffects.Entities;
using Unity.Entities;
#endif

namespace StatusEffects.Modules
{
#if NETCODE_ENTITIES
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
#endif
    [CreateAssetMenu(fileName = "Damage Over Time Module", menuName = "Status Effect Framework/Modules/Damage Over Time", order = 1)]
    [AttachModuleInstance(typeof(DamageOverTimeInstance))]
    public class DamageOverTimeModule : Module
#if ENTITIES
        , IEntityModule
    {
        public void ModifyCommandBuffer(ref EntityCommandBuffer commandBuffer, in Entity entity, ModuleInstance moduleInstance)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            commandBuffer.AddComponent(entity, new DamageOverTimeEntityModule()
            {
                InvervalSeconds = damageOverTimeInstance.IntervalSeconds,
                CurrentSeconds = 0
            });
        }

        public struct DamageOverTimeEntityModule : IComponentData
        {
            public float InvervalSeconds;
            public float CurrentSeconds; // For use in the ISystem
        }
#else
    {
#endif
#if UNITASK
        public override async UniTaskVoid EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            if (manager.TryGetComponent(out IExamplePlayer player))
                while (!token.IsCancellationRequested)
                {
                    // Reduce health based on the Statu Effect base value
                    player.Health -= statusEffect.Data.BaseValue * statusEffect.Stacks;
                    // Wait for the interval before applying the damage again
                    await UniTask.WaitForSeconds(damageOverTimeInstance.IntervalSeconds);
                }
        }
#else
        public override async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            if (manager.TryGetComponent(out IExamplePlayer player))
                while (!token.IsCancellationRequested)
                {
                    // Reduce health based on the Statu Effect base value
                    player.Health -= statusEffect.Data.BaseValue * statusEffect.Stacks;
                    // Wait for the interval before applying the damage again
                    await Awaitable.WaitForSecondsAsync(damageOverTimeInstance.IntervalSeconds);
                }
        }
#endif
    }
}
