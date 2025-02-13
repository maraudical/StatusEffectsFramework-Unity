#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public struct ModuleUpdateTag : IComponentData, IEnableableComponent { }
}
#endif