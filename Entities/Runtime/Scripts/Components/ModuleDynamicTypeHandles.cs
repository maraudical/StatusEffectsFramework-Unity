#if ENTITIES
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    internal struct ModuleDynamicTypeHandles : IBufferElementData
    {
        public TypeIndex TypeIndex;
    }
}
#endif