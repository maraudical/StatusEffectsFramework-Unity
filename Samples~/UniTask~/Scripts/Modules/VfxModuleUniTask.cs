using Cysharp.Threading.Tasks;
using System.Threading;
using UnityEngine;

namespace StatusEffectFramework.Samples
{
    public partial class VfxModule : Module
    {
        public override async UniTaskVoid EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;
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
                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(vfxInstance.Prefab, manager, statusEffect, previous, stack);
        }
    }
}