using Cysharp.Threading.Tasks;
using StatusEffectsFramework.Entities;
using Unity.Entities;
using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Heal Module", menuName = "Status Effects Framework/Modules/Heal", order = 1)]
    public class HealModule : Module, IEntityModule
    {
        public void CreateModuleInfo(ModuleInstance moduleInstance, ref ModuleInfo info, ref BlobBuilder builder)
        {
            ModuleInfo.AllocateModule(new HealModuleStruct(), ref info, ref builder);
        }
    }
}