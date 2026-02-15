#if ENTITIES
using System;
using Unity.Entities;

namespace StatusEffects.Entities
{
    /// <summary>
    /// A simple struct simply for reordering <see cref="StatusEffects"/> 
    /// buffers while retaining their origional index.
    /// </summary>
    internal struct IndexedStatusEffects : IEquatable<int>, IEquatable<Hash128>
    {
        public int Index;

        public Hash128 StatusEffectDataId;
        public StatusEffectTiming Timing;
        public float Duration;
        public float Interval;
        public int Stacks;
        public Hash128 EventId;

        public IndexedStatusEffects(int index, StatusEffects statusEffect)
        {
            Index = index;

            StatusEffectDataId = statusEffect.StatusEffectDataId;
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
            return StatusEffectDataId.Equals(other);
        }
    }
}
#endif