#if ENTITIES && ADDRESSABLES
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    public struct StatusVariableUpdate : IComponentData, IEnableableComponent { }
}
#endif