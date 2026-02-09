#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    internal struct StatusEffectEvents : IBufferElementData
    {
        public StatusEffectEvent Event;
        public int Id;
        public int PreviousStacks;
    }
}
#endif