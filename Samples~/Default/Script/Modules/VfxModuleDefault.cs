#if UNITY_2023_1_OR_NEWER
using System.Threading;
#else
using System.Collections;
#endif
using UnityEngine;

namespace StatusEffectFramework.Samples
{
    public partial class VfxModule : Module
    {
#if UNITY_2023_1_OR_NEWER
        public override async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;
            // Make sure the particle system stop action is set to destroy so it
            // automatically destroys itself when all particles die.
            GameObject vfxGameObject = Instantiate(vfxInstance.Prefab, manager.transform);
            ParticleSystem particleSystem = vfxGameObject.GetComponent<ParticleSystem>();
            // If we want this effect to be added everytime more stacks are
            // added we just immediately begin destruction on the current particle.
            if (particleSystem && particleSystem.main.loop)
                await AwaitableExtensions.WaitUntilCanceled(token);
            else
                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(vfxInstance.Prefab, manager, statusEffect, previous, stack);

            // Attempt to stop the particle system.
            particleSystem?.Stop();
        }
#else
        public override IEnumerator EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;
            // Make sure the particle system stop action is set to destroy so it
            // automatically destroys itself when all particles die.
            // Give the vfx the name of the prefab so it can be queried later.
            GameObject vfxGameObject = Instantiate(vfxInstance.Prefab, manager.transform);
            ParticleSystem particleSystem = vfxGameObject.GetComponent<ParticleSystem>();
            vfxGameObject.name = vfxInstance.Prefab.name;

            if (particleSystem && particleSystem.main.loop)
                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(vfxInstance.Prefab, manager, statusEffect, previous, stack);

            yield break;
        }

        public override void DisableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance) 
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;
            // This magic name finding system is horrible but it works. Unitask/Await would 
            // do the enabling and disabling so much better since the reference to the
            // GameObject can be kept within the method.
            Transform vfxTransform = manager.transform.Find(vfxInstance.Prefab.name);
            
            if (!vfxTransform)
                return;
            
            GameObject vfxGameObject = vfxTransform.gameObject;
            // Attempt to stop the particle system.
            vfxGameObject.GetComponent<ParticleSystem>()?.Stop();
            // Unset the parent so that if multiple effects are being removed it doesn't
            // grab the same VFX twice.
            vfxTransform.SetParent(null);
        }
#endif
    }
}