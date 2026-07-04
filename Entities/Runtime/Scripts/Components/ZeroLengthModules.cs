#if ENTITIES
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    internal struct ZeroLengthModules : IBufferElementData
    {
        public TypeIndex TypeIndex;
    }
}
#endif