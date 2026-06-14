#if ENTITIES
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    internal struct ModuleDynamicTypeHandles : IBufferElementData, IEnableableComponent
    {
        public TypeIndex TypeIndex;
    }
}
#endif