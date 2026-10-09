using System;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// A simple struct for reordering <see cref="StatusEffects"/>
    /// buffers while retaining their original index.
    /// </summary>
    internal struct IndexedStatusEffects : IEquatable<int>, IEquatable<ushort>
    {
        public int Index;

        public ushort Id;
        public StatusEffectTiming Timing;
        public ushort EventId;
        /// <summary>
        /// Absolute base value of the effect's data, or 0 if the data doesn't exist.
        /// </summary>
        public float BaseValue;
        /// <inheritdoc cref="StatusEffects.TimeRemaining"/>
        public float TimeRemaining;

        public IndexedStatusEffects(int index, StatusEffects statusEffect, float baseValue, float timeRemaining)
        {
            Index = index;

            Id = statusEffect.Id;
            Timing = statusEffect.Timing;
            EventId = statusEffect.EventId;
            BaseValue = baseValue;
            TimeRemaining = timeRemaining;
        }

        public bool Equals(int other) => Index.Equals(other);

        public bool Equals(ushort other) => Id.Equals(other);
    }
}