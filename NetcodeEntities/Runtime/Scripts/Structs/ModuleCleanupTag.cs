#if ENTITIES && ADDRESSABLES
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    public struct ModuleCleanupTag : IComponentData, IEnableableComponent { }
}
#endif