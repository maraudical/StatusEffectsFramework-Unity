#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public struct ModuleDestroyTag : IComponentData, IEnableableComponent { }
}
#endif