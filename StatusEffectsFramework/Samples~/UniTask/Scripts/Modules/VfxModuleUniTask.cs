using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    public partial class VfxModule : Module
    {
        public override void EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;

            var source = CancellationTokenSource.CreateLinkedTokenSource(manager.GetCancellationTokenOnDestroy());
            Task(manager, statusEffect, vfxInstance, source.Token).Forget();

            statusEffect.Stopped += source.Cancel;
        }

        async UniTaskVoid Task(StatusManager manager, StatusEffect statusEffect, VfxInstance vfxInstance, CancellationToken token)
        {
            // Make sure the particle system stop action is set to destroy so it
            // automatically destroys itself when all particles die.
            GameObject vfxGameObject = Instantiate(vfxInstance.Prefab, manager.transform);
            ParticleSystem particleSystem = vfxGameObject.GetComponent<ParticleSystem>();
            // If we want this effect to be added everytime more stacks are
            // added we just immediately begin destruction on the current particle.
            if (particleSystem && particleSystem.main.loop)
            {
                await UniTask.WaitUntilCanceled(token);
                // Attempt to stop the particle system.
                particleSystem?.Stop();
            }
            else
                statusEffect.StackUpdate += (previous, stack) => OnStackUpdate(vfxInstance.Prefab, manager, statusEffect, previous, stack);
        }
    }
}