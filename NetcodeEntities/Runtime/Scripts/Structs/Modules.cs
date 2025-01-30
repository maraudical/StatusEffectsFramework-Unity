#if ENTITIES && ADDRESSABLES
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    public struct Modules : ICleanupBufferElementData
    {
        public Entity Value;
    }
}
#endif