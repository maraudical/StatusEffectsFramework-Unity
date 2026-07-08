#if ENTITIES
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    internal struct ZeroLengthModules : IBufferElementData, IEnableableComponent
    {
        public TypeIndex TypeIndex;
    }
}
#endif