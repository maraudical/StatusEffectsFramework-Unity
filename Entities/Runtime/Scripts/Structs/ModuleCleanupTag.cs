#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public struct ModuleCleanupTag : IComponentData, IEnableableComponent { }
}
#endif