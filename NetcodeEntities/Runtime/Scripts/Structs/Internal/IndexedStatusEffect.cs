#if ENTITIES && ADDRESSABLES
using System;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    /// <summary>
    /// A simple struct simply for reordering <see cref="StatusEffects"/> 
    /// buffers while retaining their origional index.
    /// </summary>
    internal struct IndexedStatusEffect : IEquatable<int>, IEquatable<FixedString64Bytes>
    {
        public int Index;

        public bool HasModule;
        public Entity Module;
        public BlobAssetReference<StatusEffectData> Data;
        public StatusEffectTiming Timing;
        public float Duration;
        public float Interval;
        public int Stacks;
        public FixedString64Bytes EventId;

        public IndexedStatusEffect(int index, StatusEffects statusEffect)
        {
            Index = index;

            HasModule = statusEffect.HasModule;
            Module = statusEffect.Module;
            Data = statusEffect.Data;
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

        public bool Equals(FixedString64Bytes other)
        {
            return Data.Value.Id.Equals(other);
        }
    }
}
#endif