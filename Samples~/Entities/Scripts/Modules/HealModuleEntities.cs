using StatusEffectFramework.Entities;
using StatusEffectFramework.Entities.Samples;

namespace StatusEffectFramework.Samples
{
    public partial class HealModule : Module, IEntityModule
    {
        public ModuleInfo CreateModuleInfo(ModuleInstance moduleInstance)
        {
            return IEntityModule.AllocateModule(new HealModuleStruct());
        }
    }
}