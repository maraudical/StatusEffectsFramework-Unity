using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Damage Over Time Module", menuName = "Status Effects Framework/Modules/Damage Over Time", order = 1)]
    [AttachModuleInstance(typeof(DamageOverTimeInstance))]
    public class DamageOverTimeModule : Module
    {
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
    }
}