#if ENTITIES
using System;
using Unity.Burst;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    [BurstCompile]
    public struct StatusEffectEvents : IBufferElementData, IEnableableComponent, IEquatable<uint>
    {
        public StatusEffectEvent Event;
        public uint InstanceId;
        public ushort Id;
        public int PreviousStacks;
#if NETCODE
        /// <summary>
        /// Niche case where not all events on an interpolated client are new events. 
        /// For example, if a client is connecting to an existing session, then it 
        /// will register all of the <see cref="StatusEffects"/> as brand new 
        /// events. But, they may have been active on the entity for a while 
        /// server-side. To catch this we can compare the tick it was added 
        /// server-side with the current interpolated tick. If the difference is 
        /// greater than a small threshold of a fraction of a second then it will be 
        /// considered old.
        /// </summary>
        public bool IsOld;

        public const float SecondsTillOldThreshold = 0.5f;
#endif

        public StatusEffectEvents(uint id, ushort statusEffectDataId
#if NETCODE
            , bool isOld = false
#endif
            )
        {
            Event = StatusEffectEvent.Added;
            InstanceId = id;
            Id = statusEffectDataId;
            PreviousStacks = 0;
#if NETCODE
            IsOld = isOld;
#endif
        }

        public StatusEffectEvents(uint id, ushort statusEffectDataId, int previousStacks, StatusEffectEvent statusEffectEvent
#if NETCODE
            , bool isOld = false
#endif
            )
        {
            Event = statusEffectEvent;
            InstanceId = id;
            Id = statusEffectDataId;
            PreviousStacks = previousStacks;
#if NETCODE
            IsOld = isOld;
#endif
        }

        public bool Equals(uint other) => InstanceId.Equals(other);
    }
}
#endif