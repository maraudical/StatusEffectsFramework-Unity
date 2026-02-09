#if ENTITIES
using Unity.Burst;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffects.Entities
{
    [BurstCompile]
    public struct StatusEffects : IBufferElementData
    {
#if NETCODE
        [GhostField]
        public NetworkTick TickAdded;
#else
        public double TimeAdded;
#endif
#if NETCODE
        [GhostField]
#endif
        public uint Id;
#if NETCODE
        [GhostField]
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
        [GhostField]
#endif
        public Hash128 EventId;

        [BurstCompile]
        /// <summary>
        /// Calculated remaining time until the status effect expires.
        /// </summary>
        public float TimeRemaining
#if NETCODE   
            (ClientServerTickRate tickRate, NetworkTime networkTime, bool isPredicted)
        {
            return Timing switch
            {
                StatusEffectTiming.Infinite => -1f,
                StatusEffectTiming.Event or StatusEffectTiming.Predicate => Duration,
                _ => isPredicted ? networkTime.PredictedTimeSinceTick(TickAdded, tickRate) 
                                 : networkTime.InterpolatedTimeSinceTick(TickAdded, tickRate)
            };
        }
#else
            (TimeData timeData)
        {
            return Timing switch
            {
                StatusEffectTiming.Infinite => -1f,
                StatusEffectTiming.Event or StatusEffectTiming.Predicate => Duration,
                _ => Unity.Mathematics.math.max(Duration - timeData.ElapsedTime + TimeAdded)
            };
        } 
#endif
    }
}
#endif