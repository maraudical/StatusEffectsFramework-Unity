#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public struct StatusEffectEvents : IBufferElementData
    {
        public StatusEffectEvent Event;
        public int Id;
        public int PreviousStacks;

        public StatusEffectEvents(int id)
        {
            Event = StatusEffectEvent.Added;
            Id = id;
            PreviousStacks = 0;
        }

        public StatusEffectEvents(StatusEffectEvent statusEffectEvent, int id, int previousStacks)
        {
            Event = statusEffectEvent;
            Id = id;
            PreviousStacks = previousStacks;
        }
    }
}
#endif