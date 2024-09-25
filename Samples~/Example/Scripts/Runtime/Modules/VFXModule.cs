#if UNITASK
using Cysharp.Threading.Tasks;
using StatusEffects.Example;
using System.Threading;
#elif UNITY_2023_1_OR_NEWER
using StatusEffects.Extensions;
using System.Threading;
#else
using System.Collections;
#endif
using UnityEngine;
using static UnityEngine.EventSystems.EventTrigger;

namespace StatusEffects.Modules
{
    [CreateAssetMenu(fileName = "VFX Module", menuName = "Status Effect Framework/Modules/VFX", order = 1)]
    [AttachModuleInstance(typeof(VFXInstance))]
    public class VFXModule : Module
    {
        [SerializeField] private float DestroyPrefabAfter = 8;

#if UNITASK
        public override async UniTask EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            VFXInstance VFXInstance = moduleInstance as VFXInstance;
            
            GameObject VFXGameObject = Instantiate(VFXInstance.Prefab, manager.transform);
            // If we want this effect to be added everytime more are stacks
            // added we just immediately begin destruction on the current particle.
            if (VFXInstance.InstantiateAgainWhenAddingStacks)
                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(VFXInstance.Prefab, manager, statusEffect, previous, stack);
            else
                await UniTask.WaitUntilCanceled(token);
            // Attempt to stop the particle system.
            if (!VFXInstance.InstantiateAgainWhenAddingStacks)
            {
                // Note that you need to check if the effect is null in case the
                // cancellation was invoked from the destruction of the MonoBehaviour.
                if (!VFXGameObject)
                    return;

                ParticleSystem[] particleSystems = VFXGameObject.GetComponentsInChildren<ParticleSystem>();

                foreach (ParticleSystem particleSystem in particleSystems)
                    particleSystem.Stop();
            }
            // Destroy after a wait so that particles have time to fade out.
            Destroy(VFXGameObject, DestroyPrefabAfter);
        }
#elif UNITY_2023_1_OR_NEWER
        public override async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            VFXInstance VFXInstance = moduleInstance as VFXInstance;
            
            GameObject VFXGameObject = Instantiate(VFXInstance.Prefab, manager.transform);
            // If we want this effect to be added everytime more are stacks
            // added we just immediately begin destruction on the current particle.
            if (VFXInstance.InstantiateAgainWhenAddingStacks)
                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(VFXInstance.Prefab, manager, statusEffect, previous, stack);
            else
                await AwaitableExtensions.WaitUntilCanceled(token);
            // Attempt to stop the particle system.
            if (!VFXInstance.InstantiateAgainWhenAddingStacks)
            {
                // Note that you need to check if the effect is null in case the
                // cancellation was invoked from the destruction of the MonoBehaviour.
                if (!VFXGameObject)
                    return;
                ParticleSystem[] particleSystems = VFXGameObject.GetComponentsInChildren<ParticleSystem>();

                foreach (ParticleSystem particleSystem in particleSystems)
                    particleSystem.Stop();
            }
            // Destroy after a wait so that particles have time to fade out.
            Destroy(VFXGameObject, DestroyPrefabAfter);
        }
#else
        public override IEnumerator EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            VFXInstance VFXInstance = moduleInstance as VFXInstance;
            // Give the vfx the name of the prefab so it can be queried later
            GameObject VFXGameObject = Instantiate(VFXInstance.Prefab, manager.transform).name = VFXInstance.Prefab.name;

            if (VFXInstance.InstantiateAgainWhenAddingStacks)
            {
                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(VFXInstance.Prefab, manager, statusEffect, previous, stack);

                Destroy(VFXGameObject, DestroyPrefabAfter);
            }

            yield break;
        }

        public override void DisableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance) 
        {
            VFXInstance VFXInstance = moduleInstance as VFXInstance;
            // If we are instantiating when adding stacks it has already been destroyed.
            if (VFXInstance.InstantiateAgainWhenAddingStacks)
                return;
            // This magic name finding system is horrible but it works. Unitask would do
            // the enabling and disabling so much better since the reference to the
            // GameObject can be kept as DisableModule is just when cancellation is called.
            Transform VFXTransform = manager.transform.Find(VFXInstance.Prefab.name);
            
            if (!VFXTransform)
                return;
            
            GameObject VFXGameObject = VFXTransform.gameObject;
            // Attempt to stop the particle system.
            ParticleSystem[] particleSystems = VFXGameObject.GetComponentsInChildren<ParticleSystem>();

            foreach (ParticleSystem particleSystem in particleSystems)
                particleSystem.Stop();
            // Unset the parent so that if multiple effects are being removed it doesn't
            // grab the same VFX twice.
            VFXTransform.SetParent(null);
            // Destroy after a wait so that particles have time to fade out.
            Destroy(VFXGameObject, DestroyPrefabAfter);
        }
#endif

        private void OnStackUpdate(GameObject prefab, StatusManager manager, StatusEffect statusEffect, int previous, int stack)
        {
            if (previous >= stack)
                return;

            GameObject VFXGameObject = Instantiate(prefab, manager.transform);
            // Destroy after a wait so that particles have time to fade out.
            Destroy(VFXGameObject, DestroyPrefabAfter);
        }
    }
}
