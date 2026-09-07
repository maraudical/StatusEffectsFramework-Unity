#if ENTITIES
using System;
using Unity.Burst;
using Unity.Entities;
using Unity.Mathematics;
using Unity.Collections;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
    [BurstCompile]
    public struct StatusEffects : IBufferElementData, IComparable<StatusEffects>, IEquatable<uint>, IEquatable<StatusEffects>
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
        public ushort StatusEffectDataId;
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
        public ushort EventId;

        public int CompareTo(StatusEffects other) => Id.CompareTo(other.Id);

        public bool Equals(uint other) => Id.Equals(other);

        public bool Equals(StatusEffects other) => Id.Equals(other.Id);

        /// <summary>
        /// Calculated remaining time until the status effect expires.
        /// </summary>
        [BurstCompile]
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

        /// <summary>
        /// Finds the index in the <see cref="DynamicBuffer{T}"/> where the <see cref="StatusEffects.Id"/> equals a given <paramref name="id"/>.
        /// </summary>
        /// <returns>The index of the first occurrence of the value in the buffer. Returns -1 if no occurrence is found.</returns>
        [BurstCompile]
        public static int IndexOfStatusEffect(in DynamicBuffer<StatusEffects> buffer, uint id)
        {
            return buffer.AsNativeArray().IndexOf(id);
        }

        /// <summary>
        /// Attempts to find the <see cref="StatusEffects"/> in a <see cref="DynamicBuffer{T}"/> where the <see cref="StatusEffects.Id"/> equals a given <paramref name="id"/>.
        /// </summary>
        [BurstCompile]
        public static bool TryGetStatusEffect(in DynamicBuffer<StatusEffects> buffer, uint id, out StatusEffects statusEffect)
        {
            var index = IndexOfStatusEffect(buffer, id);
            bool foundIndex = index >= 0;
            statusEffect = foundIndex ? buffer[index] : default;
            return foundIndex;
        }
    }
}
#endif