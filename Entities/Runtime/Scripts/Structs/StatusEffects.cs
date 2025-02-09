#if ENTITIES
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffects.Entities
{
    public struct StatusEffects : IBufferElementData
    {
#if NETCODE
        [GhostField]
#endif
        public bool HasModule;
#if NETCODE
        [GhostField]
#endif
        public Entity Module;
#if NETCODE
        [GhostField]
#endif
        public Hash128 Id;
#if NETCODE
        [GhostField]
#endif
        public StatusEffectTiming Timing;
#if NETCODE
        [GhostField(Quantization = 1000)]
#endif
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
    }
}
#endif