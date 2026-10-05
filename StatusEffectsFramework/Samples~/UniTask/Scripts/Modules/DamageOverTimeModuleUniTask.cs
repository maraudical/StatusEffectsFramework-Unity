using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    public partial class DamageOverTimeModule : Module
    {
        public override void EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            var source = CancellationTokenSource.CreateLinkedTokenSource(manager.GetCancellationTokenOnDestroy());
            Task(manager, statusEffect, damageOverTimeInstance, source.Token).Forget();

            statusEffect.Stopped += source.Cancel;
        }

        async UniTaskVoid Task(StatusManager manager, StatusEffect statusEffect, DamageOverTimeInstance damageOverTimeInstance, CancellationToken token)
        {
            if (manager.TryGetComponent(out IExamplePlayer player))
                while (!token.IsCancellationRequested)
                {
                    // Reduce DamageOverTimeth based on the Statu Effect base value
                    player.Health -= statusEffect.Data.BaseValue * statusEffect.Stacks;
                    // Wait for the interval before applying the damage again
                    await UniTask.WaitForSeconds(damageOverTimeInstance.IntervalSeconds);
                }
        }
    }
}