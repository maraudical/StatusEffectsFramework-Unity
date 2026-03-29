#if ENTITIES
using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectFramework.Entities
{
    [BurstCompile]
    public struct StatusEffects : IBufferElementData, IComparable<StatusEffects>, IEquatable<uint>
    {
#if NETCODE
        [GhostField(Composite = true)]
        public NetworkTick TickAdded;
#else
        public double TimeAdded;
#endif
#if NETCODE
        [GhostField(Composite = true)]
        public NetworkTick TickUpdated;
#endif
#if NETCODE
        [GhostField]
#endif
        public uint Id;
#if NETCODE
        [GhostField(Composite = true)]
#endif
        public Hash128 StatusEffectDataId;
#if NETCODE
        [GhostField]
#endif
        public StatusEffectTiming Timing;
#if NETCODE
        [GhostField(Quantization = 1000)]
#endif
        /// <summary>
        /// The total duration until the status effect expires. Depending on the 
        /// <see cref="StatusEffectTiming"/> this may be seconds or based on an event.
        /// </summary>
        public float Duration;
#if NETCODE
        [GhostField(Quantization = 1000)]
#endif
        public float Interval;
#if NETCODE
        [GhostField]
#endif
        public int Stacks;
        /// <summary>
        /// If a custom system for checking when to decrement duration 
        /// (in the case of <see cref="StatusEffectTiming.Event"/> and 
        /// <see cref="StatusEffectTiming.Predicate"/>) is needed. That 
        /// system can query when and what to decrement by looping 
        /// through all <see cref="StatusEffects"/> buffers and manually 
        /// decrement them.
        /// </summary>
#if NETCODE
        [GhostField(Composite = true)]
#endif
        public Hash128 EventId;

        public int CompareTo(StatusEffects other) => Id.CompareTo(other.Id);

        public bool Equals(uint other) => Id.Equals(other);

        [BurstCompile]
        /// <summary>
        /// Calculated remaining time until the status effect expires.
        /// </summary>
        public float TimeRemaining
#if NETCODE
            (NetworkTick currentTick, ClientServerTickRate tickRate)
        {
            return TimeRemaining(currentTick, 0f, tickRate);
        }

        public float TimeRemaining (NetworkTick currentTick, float currentTickFraction, ClientServerTickRate tickRate)
        {
            return Timing switch
            {
                StatusEffectTiming.Infinite => -1f,
                StatusEffectTiming.Event or StatusEffectTiming.Predicate => Duration,
                _ => math.max(0, Duration - currentTick.TimeSince(TickAdded, currentTickFraction, tickRate))
            };
        }
#else
            (double elapsedTime)
        {
            return Timing switch
            {
                StatusEffectTiming.Infinite => -1f,
                StatusEffectTiming.Event or StatusEffectTiming.Predicate => Duration,
                _ => math.max(0f, Duration - (float)(elapsedTime + TimeAdded))
            };
        } 
#endif
    }
}
#endif