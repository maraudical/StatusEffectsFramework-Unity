using Unity.Entities;
using StatusEffectsFramework.Entities;
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Damage Over Time Module", menuName = "Status Effects Framework/Modules/Damage Over Time", order = 1)]
    [AttachModuleInstance(typeof(DamageOverTimeInstance))]
    public class DamageOverTimeModule : Module, IEntityModule
    {
        public void CreateModuleInfo(ModuleInstance moduleInstance, ref ModuleInfo info, ref BlobBuilder builder)
        {
            var instance = moduleInstance as DamageOverTimeInstance;
            var moduleStruct = new DamageOverTimeModuleStruct
            {
                IntervalSeconds = instance.IntervalSeconds,
            };
            ModuleInfo.AllocateModule(moduleStruct, ref info, ref builder);
        }
    }
}