#if Entities
using Unity.Entities;
using StatusEffectsFramework.Entities;
#endif
#if UniTask
using Cysharp.Threading.Tasks;
using System.Threading;
#elif Default
using System.Threading;
#endif
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Damage Over Time Module", menuName = "Status Effects Framework/Modules/Damage Over Time", order = 1)]
    [AttachModuleInstance(typeof(DamageOverTimeInstance))]
#if Entities
    public class DamageOverTimeModule : Module, IEntityModule
#else
    public class DamageOverTimeModule : Module
#endif
    {
#if Entities
        public void CreateModuleInfo(ModuleInstance moduleInstance, ref ModuleInfo info, ref BlobBuilder builder)
        {
            var instance = moduleInstance as DamageOverTimeInstance;
            var moduleStruct = new DamageOverTimeModuleStruct
            {
                IntervalSeconds = instance.IntervalSeconds,
            };
            ModuleInfo.AllocateModule(moduleStruct, ref info, ref builder);
        }
#endif
#if UniTask
        public override void EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            if (!manager.TryGetComponent(out IExamplePlayer player))
                return;

            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            var source = CancellationTokenSource.CreateLinkedTokenSource(manager.GetCancellationTokenOnDestroy());
            Task(player, statusEffect, damageOverTimeInstance, source.Token).Forget();

            statusEffect.Stopped += source.Cancel;

            async UniTaskVoid Task(IExamplePlayer player, StatusEffect statusEffect, DamageOverTimeInstance damageOverTimeInstance, CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    // Reduce DamageOverTimeth based on the Statu Effect base value
                    player.Health -= statusEffect.Data.BaseValue * statusEffect.Stacks;
                    // Wait for the interval before applying the damage again
                    await UniTask.WaitForSeconds(damageOverTimeInstance.IntervalSeconds);
                }
            }
        }
#elif Default
        public override void EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            if (!manager.TryGetComponent(out IExamplePlayer player))
                return;

            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            var source = CancellationTokenSource.CreateLinkedTokenSource(manager.GetCancellationTokenOnDestroy());
            _ = Task(player, statusEffect, damageOverTimeInstance, source.Token);
            statusEffect.Stopped += source.Cancel;

            async Awaitable Task(IExamplePlayer player, StatusEffect statusEffect, DamageOverTimeInstance damageOverTimeInstance, CancellationToken token)
            {
                while (!token.IsCancellationRequested)
                {
                    // Reduce DamageOverTimeth based on the Statu Effect base value
                    player.Health -= statusEffect.Data.BaseValue * statusEffect.Stacks;
                    // Wait for the interval before applying the damage again
                    await Awaitable.WaitForSecondsAsync(damageOverTimeInstance.IntervalSeconds);
                }
            }
        }
#endif
    }
}