using StatusEffectsFramework.Entities;
using StatusEffectsFramework.Entities.Samples;

namespace StatusEffectsFramework.Samples
{
    public partial class DamageOverTimeModule : Module, IEntityModule
    {
        public ModuleInfo CreateModuleInfo(ModuleInstance moduleInstance)
        {
            var instance = moduleInstance as DamageOverTimeInstance;
            var moduleStruct = new DamageOverTimeModuleStruct
            {
                IntervalSeconds = instance.IntervalSeconds,
            };
            return ModuleInfo.AllocateModule(moduleStruct);
        }
    }
}