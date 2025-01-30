#if ENTITIES && ADDRESSABLES
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    public struct ModuleDestroyTag : IComponentData, IEnableableComponent { }
}
#endif