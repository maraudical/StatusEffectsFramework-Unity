#if ENTITIES
using System;
using Unity.Entities;

namespace StatusEffects.Entities
{
    public struct StatusEffectEvents : IBufferElementData, IEnableableComponent, IEquatable<uint>
    {
        public StatusEffectEvent Event;
        public uint Id;
        public Hash128 StatusEffectDataId;
        public int PreviousStacks;

        public StatusEffectEvents(uint id, Hash128 statusEffectDataId)
        {
            Event = StatusEffectEvent.Added;
            Id = id;
            StatusEffectDataId = statusEffectDataId;
            PreviousStacks = 0;
        }

        public StatusEffectEvents(uint id, Hash128 statusEffectDataId, int previousStacks, StatusEffectEvent statusEffectEvent)
        {
            Event = statusEffectEvent;
            Id = id;
            StatusEffectDataId = statusEffectDataId;
            PreviousStacks = previousStacks;
        }

        public bool Equals(uint other) => Id.Equals(other);
    }
}
#endif