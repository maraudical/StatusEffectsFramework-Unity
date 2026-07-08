#if ENTITIES
using Unity.Burst;
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Adding this to any <see cref="Entity"> will make a request to add/remove a 
    /// StatusEffect. See the <see cref="StatusEffectRequests"/> static methods for 
    /// options.
    /// </summary>
#if NETCODE
    [GhostComponent(PrefabType = GhostPrefabType.AllPredicted)]
#endif
    [BurstCompile]
    public struct StatusEffectRequests : IBufferElementData
    {
        [GhostField]
        public StatusEffectRequestType Type;
        [GhostField]
        public StatusEffectRemovalType RemovalType;
        [GhostField]
        public StatusEffectGroup Group;
        [GhostField]
        public uint Id;
        [GhostField]
        public Hash128 Hash;
        [GhostField]
        public StatusEffectTiming Timing;
        [GhostField(Quantization = 1000)]
        public float Duration;
        [GhostField(Quantization = 1000)]
        public float Interval;
        [GhostField]
        public int Stacks;
        /// <inheritdoc cref="StatusEffects.EventId"/>
        [GhostField]
        public Hash128 EventId;

        /// <summary>
        /// Constructs a <see cref="StatusEffectRequests"/> to request adding a new 
        /// effect. Optional <paramref name="stacks"/> count.
        /// </summary>
        [BurstCompile]
        public static StatusEffectRequests Add(Hash128 statusEffectData, int stacks = 1)
        {
            return new StatusEffectRequests
            {
                Type = StatusEffectRequestType.Add,
                Hash = statusEffectData,
                Timing = StatusEffectTiming.Infinite,
                Duration = -1,
                Stacks = stacks,
            };
        }

        /// <summary>
        /// Constructs a <see cref="StatusEffectRequests"/> to request adding a new 
        /// effect with a <paramref name="duration"/>. Optional 
        /// <paramref name="stacks"/> count.
        /// </summary>
        [BurstCompile]
        public static StatusEffectRequests AddWithDuration(Hash128 statusEffectData, float duration, int stacks = 1)
        {
            return new StatusEffectRequests
            {
                Type = StatusEffectRequestType.Add,
                Hash = statusEffectData,
                Timing = StatusEffectTiming.Duration,
                Duration = duration,
                Stacks = stacks,
            };
        }

        /// <summary>
        /// Constructs a <see cref="StatusEffectRequests"/> to request adding a new 
        /// effect with a <paramref name="duration"/> that will decrement from an 
        /// <paramref name="eventId"/>. Optional <paramref name="stacks"/> count and 
        /// <paramref name="interval"/> for decrementing effect duration.
        /// </summary>
        /// <remarks>
        /// Note that decrementing duration for this effect must be controlled by the 
        /// user. It will not happen automatically.This can be done by querying 
        /// relevant <see cref="StatusEffects"/> and checking for similar 
        /// <paramref name="eventId"/>.
        /// </remarks>
        [BurstCompile]
        public static StatusEffectRequests AddWithEvent(Hash128 statusEffectData, float duration, Hash128 eventId, float interval = 1, int stacks = 1)
        {
            return new StatusEffectRequests
            {
                Type = StatusEffectRequestType.Add,
                Hash = statusEffectData,
                Timing = StatusEffectTiming.Event,
                Duration = duration,
                Interval = interval,
                Stacks = stacks,
                EventId = eventId,
            };
        }

        /// <summary>
        /// Constructs a <see cref="StatusEffectRequests"/> to request adding a new 
        /// effect with a predicate <paramref name="eventId"/>. Optional 
        /// <paramref name="stacks"/> count.
        /// </summary>
        /// <remarks>
        /// Note that ending the predicate for this effect must be controlled by the 
        /// user. It will not happen automatically.This can be done by querying 
        /// relevant <see cref="StatusEffects"/> and checking for similar 
        /// <paramref name="eventId"/>. The user only needs to set 
        /// <see cref="StatusEffects.Duration"/> to 0.
        /// </remarks>
        [BurstCompile]
        public static StatusEffectRequests AddWithPredicate(Hash128 statusEffectData, Hash128 eventId, int stacks = 1)
        {
            return new StatusEffectRequests
            {
                Type = StatusEffectRequestType.Add,
                Hash = statusEffectData,
                Timing = StatusEffectTiming.Duration,
                Duration = 1,
                Stacks = stacks,
                EventId = eventId,
            };
        }

        /// <summary>
        /// Removes all <see cref="StatusEffects"/>. 
        ///</summary>
        [BurstCompile]
        public static StatusEffectRequests RemoveAll()
        {
            return new StatusEffectRequests
            {
                Type = StatusEffectRequestType.Remove,
                RemovalType = StatusEffectRemovalType.Any,
                Stacks = -1
            };
        }

        /// <summary>
        /// Remove any amount of <see cref="StatusEffects"/> given a 
        /// <see cref="uint"/> <paramref name="id"/>. Optional 
        /// <paramref name="stacks"/> count.
        /// </summary>
        [BurstCompile]
        public static StatusEffectRequests RemoveWithId(uint id, int stacks = -1)
        {
            return new StatusEffectRequests
            {
                Type = StatusEffectRequestType.Remove,
                RemovalType = StatusEffectRemovalType.Id,
                Id = id,
                Stacks = stacks
            };
        }

        /// <summary>
        /// Remove any amount of <see cref="StatusEffects"/> given a 
        /// <see cref="Hash128"/> <paramref name="id"/> of the 
        /// <see cref="UnmanagedStatusEffectData.Id"/> reference. Optional 
        /// <paramref name="stacks"/> count.
        /// </summary>
        [BurstCompile]
        public static StatusEffectRequests RemoveWithStatusEffectDataId(Hash128 id, int stacks = -1)
        {
            return new StatusEffectRequests
            {
                Type = StatusEffectRequestType.Remove,
                RemovalType = StatusEffectRemovalType.StatusEffectDataId,
                Hash = id,
                Stacks = stacks
            };
        }

        /// <summary>
        /// Remove any amount of <see cref="StatusEffects"/> given a 
        /// <see cref="Hash128"/> <paramref name="name"/> of the 
        /// <see cref="ComparableName"/> <see cref="Name.Id"/> reference. Optional 
        /// <paramref name="stacks"/> count.
        /// </summary>
        [BurstCompile]
        public static StatusEffectRequests RemoveWithComparableName(Hash128 name, int stacks = -1)
        {
            return new StatusEffectRequests
            {
                Type = StatusEffectRequestType.Remove,
                RemovalType = StatusEffectRemovalType.ComparableName,
                Hash = name,
                Stacks = stacks
            };
        }

        /// <summary>
        /// Remove any amount of <see cref="StatusEffects"/> given a 
        /// <see cref="StatusEffectGroup"/> <paramref name="group"/>. Optional 
        /// <paramref name="stacks"/> count and <paramref name="matchAllGroups"/> 
        /// toggle to check if all group flags in the given <paramref name="group"/> 
        /// need to match in order to remove.
        /// </summary>
        [BurstCompile]
        public static StatusEffectRequests RemoveWithGroup(StatusEffectGroup group, int stacks = -1, bool matchAllGroups = true)
        {
            return new StatusEffectRequests
            {
                Type = StatusEffectRequestType.Remove,
                RemovalType = matchAllGroups ? StatusEffectRemovalType.AllGroups : StatusEffectRemovalType.AnyGroups,
                Group = group,
                Stacks = stacks
            };
        }
    }
}
#endif