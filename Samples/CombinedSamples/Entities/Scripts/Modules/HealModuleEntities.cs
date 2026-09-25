using StatusEffectsFramework.Entities;
using StatusEffectsFramework.Entities.Samples;
using Unity.Entities;

namespace StatusEffectsFramework.Samples
{
    public partial class HealModule : Module, IEntityModule
    {
        public void CreateModuleInfo(ModuleInstance moduleInstance, ref ModuleInfo info, ref BlobBuilder builder)
        {
            ModuleInfo.AllocateModule(new HealModuleStruct(), ref info, ref builder);
        }
    }
}