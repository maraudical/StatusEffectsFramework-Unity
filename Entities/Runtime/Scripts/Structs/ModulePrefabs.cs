#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public struct ModulePrefabs : IBufferElementData
    {
        public Entity Entity;
    }
}
#endif