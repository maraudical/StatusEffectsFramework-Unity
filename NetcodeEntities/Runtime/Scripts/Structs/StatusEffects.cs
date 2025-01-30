#if ENTITIES && ADDRESSABLES
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    public struct StatusEffects : IBufferElementData
    {
        public bool HasModule;
        public Entity Module;
        public BlobAssetReference<StatusEffectData> Data;
        public StatusEffectTiming Timing;
        public float Duration;
        public float Interval;
        public int Stacks;
        /// <summary>
        /// If a custom system for checking when to decrement duration 
        /// (in the case of <see cref="StatusEffectTiming.Event"/> and 
        /// <see cref="StatusEffectTiming.Predicate"/>) is needed. That 
        /// system can query when and what to decrement by looping 
        /// through all <see cref="StatusEffects"/> buffers and manually 
        /// decrement them.
        /// </summary>
        public FixedString64Bytes EventId;
    }
}
#endif