using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    public partial class DamageOverTimeModule : Module
    {
        public override async UniTaskVoid EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

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