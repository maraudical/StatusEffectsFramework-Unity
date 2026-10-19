using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Vfx Module", menuName = "Status Effects Framework/Modules/Vfx", order = 1)]
    [AttachModuleInstance(typeof(VfxInstance))]
    public class VfxModule : Module
    {
        public override void EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;

            // Make sure the particle system stop action is set to destroy so it
            // automatically destroys itself when all particles die.
            GameObject vfxGameObject = Instantiate(vfxInstance.Prefab, manager.transform);
            ParticleSystem particleSystem = vfxGameObject.GetComponent<ParticleSystem>();
            // If we want this effect to be added everytime more stacks are
            // added we just immediately begin destruction on the current particle.
            if (particleSystem && particleSystem.main.loop)
                statusEffect.Stopped += () => particleSystem?.Stop();
            else
                statusEffect.StackUpdate += (previous, stack) => OnStackUpdate(vfxInstance.Prefab, manager, statusEffect, previous, stack);

            void OnStackUpdate(GameObject prefab, StatusManager manager, StatusEffect statusEffect, int previous, int stack)
            {
                if (previous >= stack)
                    return;

                Instantiate(prefab, manager.transform);
            }
        }
    }
}
