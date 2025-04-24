#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
#elif UNITY_2023_1_OR_NEWER
using StatusEffects.Extensions;
using System.Threading;
#else
using System.Collections;
#endif
#if ENTITIES
using StatusEffects.Entities;
using Unity.Entities;
#endif
using UnityEngine;

namespace StatusEffects.Modules
{
    [CreateAssetMenu(fileName = "Vfx Module", menuName = "Status Effect Framework/Modules/Vfx", order = 1)]
    [AttachModuleInstance(typeof(VfxInstance))]
    public class VfxModule : Module
#if ENTITIES
        , IEntityModule
    {
        public void ModifyCommandBuffer(ref EntityCommandBuffer commandBuffer, in Entity entity, ModuleInstance moduleInstance)
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;
            
            commandBuffer.AddComponent(entity, new VfxEntityModule() 
            { 
                Prefab = vfxInstance.Prefab,
                InstantiateAgainWhenAddingStacks = vfxInstance.InstantiateAgainWhenAddingStacks,
            });
        }

        public struct VfxEntityModule : IComponentData
        {
            public UnityObjectRef<GameObject> Prefab;
            public bool InstantiateAgainWhenAddingStacks;
        }
#else
    {
#endif

#if UNITASK
        public override async UniTaskVoid EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;
            // Make sure the particle system stop action is set to destroy so it
            // automatically destroys itself when all particles die.
            GameObject vfxGameObject = Instantiate(vfxInstance.Prefab, manager.transform);
            // If we want this effect to be added everytime more stacks are
            // added we just immediately begin destruction on the current particle.
            if (vfxInstance.InstantiateAgainWhenAddingStacks)
                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(vfxInstance.Prefab, manager, statusEffect, previous, stack);
            else
                await UniTask.WaitUntilCanceled(token);
            // Attempt to stop the particle system.
            if (!vfxInstance.InstantiateAgainWhenAddingStacks)
            {
                // Note that you need to check if the effect is null in case the
                // cancellation was invoked from the destruction of the MonoBehaviour.
                if (!vfxGameObject)
                    return;

                vfxGameObject.GetComponent<ParticleSystem>().Stop();
            }
        }
#elif UNITY_2023_1_OR_NEWER
        public override async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;
            // Make sure the particle system stop action is set to destroy so it
            // automatically destroys itself when all particles die.
            GameObject vfxGameObject = Instantiate(vfxInstance.Prefab, manager.transform);
            // If we want this effect to be added everytime more are stacks
            // added we just immediately begin destruction on the current particle.
            if (vfxInstance.InstantiateAgainWhenAddingStacks)
                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(vfxInstance.Prefab, manager, statusEffect, previous, stack);
            else
                await AwaitableExtensions.WaitUntilCanceled(token);
            // Attempt to stop the particle system.
            if (!vfxInstance.InstantiateAgainWhenAddingStacks)
            {
                // Note that you need to check if the effect is null in case the
                // cancellation was invoked from the destruction of the MonoBehaviour.
                if (!vfxGameObject)
                    return;

                vfxGameObject.GetComponent<ParticleSystem>().Stop();
            }
        }
#else
        public override IEnumerator EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;
            // Make sure the particle system stop action is set to destroy so it
            // automatically destroys itself when all particles die.

            // Give the vfx the name of the prefab so it can be queried later.
            GameObject vfxGameObject = Instantiate(vfxInstance.Prefab, manager.transform);
            vfxGameObject.name = vfxInstance.Prefab.name;

            if (vfxInstance.InstantiateAgainWhenAddingStacks)
            {
                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(vfxInstance.Prefab, manager, statusEffect, previous, stack);
            }

            yield break;
        }

        public override void DisableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance) 
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;
            // If we are instantiating when adding stacks it has already been destroyed.
            if (vfxInstance.InstantiateAgainWhenAddingStacks)
                return;
            // This magic name finding system is horrible but it works. Unitask would do
            // the enabling and disabling so much better since the reference to the
            // GameObject can be kept as DisableModule is just when cancellation is called.
            Transform vfxTransform = manager.transform.Find(vfxInstance.Prefab.name);
            
            if (!vfxTransform)
                return;
            
            GameObject vfxGameObject = vfxTransform.gameObject;
            // Attempt to stop the particle system.
            vfxGameObject.GetComponent<ParticleSystem>().Stop();
            // Unset the parent so that if multiple effects are being removed it doesn't
            // grab the same VFX twice.
            vfxTransform.SetParent(null);
        }
#endif

        private void OnStackUpdate(GameObject prefab, StatusManager manager, StatusEffect statusEffect, int previous, int stack)
        {
            if (previous >= stack)
                return;

            Instantiate(prefab, manager.transform);
        }
    }
}
