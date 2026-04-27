#if UNITY_2023_1_OR_NEWER
using System.Threading;
#else
using System.Collections;
#endif
using UnityEngine;

namespace StatusEffectFramework.Samples
{
    public partial class DamageOverTimeModule : Module
    {
#if UNITY_2023_1_OR_NEWER
        public override async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            if (manager.TryGetComponent(out IExamplePlayer player))
                while (!token.IsCancellationRequested)
                {
                    // Reduce DamageOverTimeth based on the Statu Effect base value
                    player.Health -= statusEffect.Data.BaseValue * statusEffect.Stacks;
                    // Wait for the interval before applying the damage again
                    await Awaitable.WaitForSecondsAsync(damageOverTimeInstance.IntervalSeconds);
                }
        }
#else
        public override IEnumerator EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            if (manager.TryGetComponent(out IExamplePlayer player))
                for (; ; )
                {
                    // Reduce DamageOverTimeth based on the Statu Effect base value
                    player.Health -= statusEffect.Data.BaseValue * statusEffect.Stacks;
                    // Wait for the interval before applying the damage again
                    yield return new WaitForSeconds(damageOverTimeInstance.IntervalSeconds);
                }
        }
#endif
    }
}