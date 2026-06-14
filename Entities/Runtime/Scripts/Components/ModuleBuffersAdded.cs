#if ENTITIES
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    internal struct ModuleBuffersAdded : IBufferElementData, IEnableableComponent
    {
        public TypeIndex TypeIndex;
    }
}
#endif