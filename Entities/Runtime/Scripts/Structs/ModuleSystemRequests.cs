#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    internal struct ModuleSystemRequests : IBufferElementData
    {
        public ulong StableTypeHash;
    }
}
#endif