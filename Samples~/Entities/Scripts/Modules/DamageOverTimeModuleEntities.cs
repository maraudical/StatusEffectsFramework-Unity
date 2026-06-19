using StatusEffectFramework.Entities;
using StatusEffectFramework.Entities.Samples;

namespace StatusEffectFramework.Samples
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