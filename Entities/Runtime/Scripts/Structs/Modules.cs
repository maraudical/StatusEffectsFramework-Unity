#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public struct Modules : ICleanupBufferElementData
    {
        public Entity Value;
    }
}
#endif