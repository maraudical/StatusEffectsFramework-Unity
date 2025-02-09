#if ENTITIES
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    public struct Modules : ICleanupBufferElementData
    {
        [GhostField]
        public Entity Value;
    }
}
#endif