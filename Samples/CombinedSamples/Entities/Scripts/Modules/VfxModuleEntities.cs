using StatusEffectsFramework.Entities;
using StatusEffectsFramework.Entities.Samples;
using Unity.Entities;
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    public partial class VfxModule : Module, IEntityModule
    {
        public void CreateModuleInfo(ModuleInstance moduleInstance, ref ModuleInfo info, ref BlobBuilder builder)
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
            ModuleInfo.AllocateModule(moduleStruct, ref info, ref builder);
        }
    }
}