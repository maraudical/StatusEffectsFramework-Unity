#if ENTITIES && ADDRESSABLES
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    public struct ModuleUpdateTag : IComponentData, IEnableableComponent { }
}
#endif