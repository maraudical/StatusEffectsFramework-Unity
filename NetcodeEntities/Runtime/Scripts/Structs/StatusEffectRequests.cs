#if ENTITIES && ADDRESSABLES
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    /// <summary>
    /// Adding this to any <see cref="Entity"> will make a request to add/remove a 
    /// StatusEffect. See the <see cref="StatusEffectRequests"/> constructors for options.
    /// </summary>
    public struct StatusEffectRequests : IBufferElementData
    {
        public StatusEffectRequestType Type;
        public StatusEffectRemovalType RemovalType;
        public StatusEffectGroup Group;
        public FixedString64Bytes Id;
        public StatusEffectTiming Timing;
        public float Duration;
        public float Interval;
        public int Stacks;
        /// <inheritdoc cref="StatusEffects.EventId"/>
        public FixedString64Bytes EventId;

        /// <summary>
        /// Add a <see cref="StatusEffects"/>. Optional stack count.
        /// </summary>
        public StatusEffectRequests(FixedString64Bytes statusEffectData, int stacks = 1)
        {
            Type = StatusEffectRequestType.Add;
            RemovalType = default;
            Group = default;
            Id = statusEffectData;
            Timing = StatusEffectTiming.Infinite;
            Duration = -1;
            Interval = default;
            Stacks = stacks;
            EventId = default;
        }

        /// <summary>
        /// Add a <see cref="StatusEffects"/> with a duration. Optional 
        /// stack count.
        /// </summary>
        public StatusEffectRequests(FixedString64Bytes statusEffectData, float duration, int stacks = 1)
        {
            Type = StatusEffectRequestType.Add;
            RemovalType = default;
            Group = default;
            Id = statusEffectData;
            Timing = StatusEffectTiming.Duration;
            Duration = duration;
            Interval = default;
            Stacks = stacks;
            EventId = default;
        }

        /// <summary>
        /// Add a <see cref="StatusEffects"/> with a duration. Timing 
        /// option will be <see cref="StatusEffectTiming.Event"/>. 
        /// Duration should be decremented by <paramref name="interval"/> 
        /// in custom systems by querying <see cref="StatusEffects"/> 
        /// buffers with the given <paramref name="eventId"/>. Optional 
        /// stack count.
        /// </summary>
        public StatusEffectRequests(FixedString64Bytes statusEffectData, float duration, FixedString64Bytes eventId, float interval = 1, int stacks = 1)
        {
            Type = StatusEffectRequestType.Add;
            RemovalType = default;
            Group = default;
            Id = statusEffectData;
            Timing = StatusEffectTiming.Event;
            Duration = duration;
            Interval = interval;
            Stacks = stacks;
            EventId = eventId;
        }

        /// <summary>
        /// Add a <see cref="StatusEffects"/>. Timing option will be 
        /// <see cref="StatusEffectTiming.Predicate"/>. Duration should 
        /// be set to 0 in custom systems by querying <see cref="StatusEffects"/> 
        /// buffers with the given <paramref name="eventId"/>. Optional 
        /// stack count.
        /// </summary>
        public StatusEffectRequests(FixedString64Bytes statusEffectData, FixedString64Bytes eventId, int stacks = 1)
        {
            Type = StatusEffectRequestType.Add;
            RemovalType = default;
            Group = default;
            Id = statusEffectData;
            Timing = StatusEffectTiming.Predicate;
            Duration = 1;
            Interval = default;
            Stacks = stacks;
            EventId = eventId;
        }

        /// <summary>
        /// Remove all of <see cref="StatusEffects"/> given 
        /// either a <see cref="FixedString64Bytes"/> ID, a 
        /// <see cref="StatusEffectGroup"/>, or nothing.
        /// </summary>
        public StatusEffectRequests(StatusEffectRemovalType removalType, FixedString64Bytes id = default, StatusEffectGroup group = default)
        {
            Type = StatusEffectRequestType.RemoveAll;
            RemovalType = removalType;
            Group = group;
            Id = id;
            Timing = default;
            Duration = default;
            Interval = default;
            Stacks = default;
            EventId = default;
        }

        /// <summary>
        /// Remove any number of <see cref="StatusEffects"/> given 
        /// either a <see cref="FixedString64Bytes"/> ID, a 
        /// <see cref="StatusEffectGroup"/>, or nothing.
        /// </summary>
        public StatusEffectRequests(StatusEffectRemovalType removalType, FixedString64Bytes id = default, StatusEffectGroup group = default, int stacks = 1)
        {
            Type = StatusEffectRequestType.Remove;
            RemovalType = removalType;
            Group = group;
            Id = id;
            Timing = default;
            Duration = default;
            Interval = default;
            Stacks = stacks;
            EventId = default;
        }
    }
}
#endif