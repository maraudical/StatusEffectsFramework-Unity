#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
#elif UNITY_2023_1_OR_NEWER
using System.Threading;
#else
using System.Collections;
#endif
using UnityEngine;
using StatusEffects.Example;

namespace StatusEffects.Modules
{
    [CreateAssetMenu(fileName = "Damage Over Time Module", menuName = "Status Effect Framework/Modules/Damage Over Time", order = 1)]
    [AttachModuleInstance(typeof(DamageOverTimeInstance))]
    public class DamageOverTimeModule : Module
    {
#if UNITASK
        public override async UniTask EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            if (manager.TryGetComponent(out IExampleEntity entity))
                while (!token.IsCancellationRequested)
                {
                    // Reduce health based on the Statu Effect base value
                    entity.Health -= statusEffect.Data.BaseValue * statusEffect.Stack;
                    // Wait for the interval before applying the damage again
                    await UniTask.WaitForSeconds(damageOverTimeInstance.IntervalSeconds);
                }
        }
#elif UNITY_2023_1_OR_NEWER
        public override async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            if (manager.TryGetComponent(out IExampleEntity entity))
                while (!token.IsCancellationRequested)
                {
                    // Reduce health based on the Statu Effect base value
                    entity.Health -= statusEffect.Data.BaseValue * statusEffect.Stack;
                    // Wait for the interval before applying the damage again
                    await Awaitable.WaitForSecondsAsync(damageOverTimeInstance.IntervalSeconds);
                }
        }
#else
        public override IEnumerator EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            if (manager.TryGetComponent(out IExampleEntity entity))
                for (; ; )
                {
                    // Reduce health based on the Statu Effect base value
                    entity.Health -= statusEffect.Data.BaseValue * statusEffect.Stack;
                    // Wait for the interval before applying the damage again
                    yield return new WaitForSeconds(damageOverTimeInstance.IntervalSeconds);
                }
        }

        public override void DisableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance) { }
#endif
    }
}
