using StatusEffectFramework.Entities;
using StatusEffectFramework.Entities.Samples;
using UnityEngine;

namespace StatusEffectFramework.Samples
{
    public partial class VfxModule : Module, IEntityModule
    {
        public ModuleInfo CreateModuleInfo(ModuleInstance moduleInstance)
        {
            var instance = moduleInstance as VfxInstance;

            bool isLooping = false;
            if (instance && instance.Prefab && instance.Prefab.TryGetComponent(out ParticleSystem particleSystem))
                isLooping = particleSystem.main.loop;

            var moduleStruct = new VfxModuleStruct
            {
                Prefab = instance.Prefab,
                IsLooping = isLooping,
            };
            return ModuleInfo.AllocateModule(moduleStruct);
        }
    }
}