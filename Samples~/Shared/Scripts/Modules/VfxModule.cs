using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Vfx Module", menuName = "Status Effect Framework/Modules/Vfx", order = 1)]
    [AttachModuleInstance(typeof(VfxInstance))]
    public partial class VfxModule : Module
    {
        protected void OnStackUpdate(GameObject prefab, StatusManager manager, StatusEffect statusEffect, int previous, int stack)
        {
            if (previous >= stack)
                return;

            Instantiate(prefab, manager.transform);
        }
    }
}
