#if ENTITIES
using StatusEffects.Modules;
using Unity.Entities;

namespace StatusEffects.Entities
{
    public interface IEntityModule
    {
        public void ModifyCommandBuffer(ref EntityCommandBuffer commandBuffer, in Entity entity, ModuleInstance moduleInstance);
    }
}
#endif