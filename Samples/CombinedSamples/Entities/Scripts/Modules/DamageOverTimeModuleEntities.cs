using StatusEffectsFramework.Entities;
using StatusEffectsFramework.Entities.Samples;
using Unity.Entities;

namespace StatusEffectsFramework.Samples
{
    public partial class DamageOverTimeModule : Module, IEntityModule
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