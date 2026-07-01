#if ENTITIES
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    internal struct ZeroLengthModules : IBufferElementData
    {
        public TypeIndex TypeIndex;
    }
}
#endif