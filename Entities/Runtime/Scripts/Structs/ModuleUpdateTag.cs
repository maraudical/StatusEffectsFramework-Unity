#if ENTITIES
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    [GhostEnabledBit]
    public struct ModuleUpdateTag : IComponentData, IEnableableComponent { }
}
#endif