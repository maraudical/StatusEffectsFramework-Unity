#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
using System;
#else
using System.Collections;
#endif
using UnityEngine;

namespace StatusEffects.Modules
{
    [CreateAssetMenu(fileName = "VFX Module", menuName = "Status Effect Framework/Modules/VFX", order = 1)]
    [AttachModuleInstance(typeof(VFXInstance))]
    public class VFXModule : Module
    {
#if UNITASK
        public override async UniTask EnableModule<T>(T monoBehaviour, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            VFXInstance VFXInstance = moduleInstance as VFXInstance;
            // Give the vfx the name of the prefab so it can be queried later
            GameObject VFXGameObject = Instantiate(VFXInstance.prefab, monoBehaviour.transform);

            await UniTask.WaitUntilCanceled(token);
            // Note that you need to check if the effect is null in case the
            // cancellation was invoked from the destruction of the MonoBehaviour
            if (!VFXGameObject)
                return;
            // Attempt to stop the particle system
            if (VFXGameObject.TryGetComponent(out ParticleSystem particleSystem))
                particleSystem.Stop();
            // Unset the parent so that if multiple effects are being removed it doesn't
            // grab the same VFX twice.
            VFXGameObject.transform.SetParent(null);
            // Destroy after a wait so that particles have time to fade out
            Destroy(VFXGameObject, 5);
        }
#else
        public override IEnumerator EnableModule<T>(T monoBehaviour, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            VFXInstance VFXInstance = moduleInstance as VFXInstance;
            // Give the vfx the name of the prefab so it can be queried later
            Instantiate(VFXInstance.prefab, monoBehaviour.transform).name = VFXInstance.prefab.name;

            yield break;
        }

        public override void DisableModule<T>(T monoBehaviour, StatusEffect statusEffect, ModuleInstance moduleInstance) 
        {
            VFXInstance VFXInstance = moduleInstance as VFXInstance;
            // This magic name finding system is horrible but it works. Unitask would do
            // the enabling and disabling so much better since the reference to the
            // GameObject can be kept as disable is just when cancellation is called.
            Transform VFXTransform = monoBehaviour.transform.Find(VFXInstance.prefab.name);
            
            if (!VFXTransform)
                return;
            
            GameObject VFXGameObject = VFXTransform.gameObject;
            // Attempt to stop the particle system
            if (VFXGameObject.TryGetComponent(out ParticleSystem particleSystem))
                particleSystem.Stop();
            // Unset the parent so that if multiple effects are being removed it doesn't
            // grab the same VFX twice.
            VFXTransform.SetParent(null);
            // Destroy after a wait so that particles have time to fade out
            Destroy(VFXGameObject, 5);
        }
#endif
    }
}
