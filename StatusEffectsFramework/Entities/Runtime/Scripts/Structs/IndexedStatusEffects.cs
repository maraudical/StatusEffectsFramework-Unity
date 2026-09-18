#if ENTITIES
using System;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// A simple struct simply for reordering <see cref="StatusEffects"/> 
    /// buffers while retaining their origional index.
    /// </summary>
    internal struct IndexedStatusEffects : IEquatable<int>, IEquatable<Hash128>
    {
        public int Index;

        public ushort Id;
        public StatusEffectTiming Timing;
        public float Duration;
        public float Interval;
        public int Stacks;
        public ushort EventId;

        public IndexedStatusEffects(int index, StatusEffects statusEffect)
        {
            Index = index;

            Id = statusEffect.Id;
            Timing = statusEffect.Timing;
            Duration = statusEffect.Duration;
            Interval = statusEffect.Interval;
            Stacks = statusEffect.Stacks;
            EventId = statusEffect.EventId;
        }

        public bool Equals(int other) => Index.Equals(other);

        public bool Equals(Hash128 other) => Id.Equals(other);
    }
}
#endif