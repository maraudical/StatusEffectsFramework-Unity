#if ENTITIES && ADDRESSABLES
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    public struct ModulePrefabs : IBufferElementData
    {
        public Entity Entity;
    }
}
#endif