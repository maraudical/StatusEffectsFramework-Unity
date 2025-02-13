#if ENTITIES
using System;
using Unity.Entities;

namespace StatusEffects.Entities
{
    /// <summary>
    /// A simple struct simply for reordering <see cref="StatusEffects"/> 
    /// buffers while retaining their origional index.
    /// </summary>
    internal struct IndexedStatusEffect : IEquatable<int>, IEquatable<Hash128>
    {
        public int Index;

        public bool HasModule;
        public Hash128 Id;
        public StatusEffectTiming Timing;
        public float Duration;
        public float Interval;
        public int Stacks;
        public Hash128 EventId;

        public IndexedStatusEffect(int index, StatusEffects statusEffect)
        {
            Index = index;

            HasModule = statusEffect.Module != Entity.Null;
 
            Id = statusEffect.Id;
            Timing = statusEffect.Timing;
            Duration = statusEffect.Duration;
            Interval = statusEffect.Interval;
            Stacks = statusEffect.Stacks;
            EventId = statusEffect.EventId;
        }

        public bool Equals(int other)
        {
            return Index.Equals(other);
        }

        public bool Equals(Hash128 other)
        {
            return Id.Equals(other);
        }
    }
}
#endif