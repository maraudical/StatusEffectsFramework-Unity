using StatusEffectsFramework.Entities;
using StatusEffectsFramework.Entities.Samples;

namespace StatusEffectsFramework.Samples
{
    public partial class HealModule : Module, IEntityModule
    {
        public ModuleInfo CreateModuleInfo(ModuleInstance moduleInstance)
        {
            return ModuleInfo.AllocateModule(new HealModuleStruct());
        }
    }
}