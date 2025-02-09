#if ENTITIES
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    public struct StatusInts : IBufferElementData
    {
#if NETCODE
        [GhostField(SendData = false)]
#endif
        public Hash128 ComponentId;
#if NETCODE
        [GhostField(SendData = false)]
#endif
        public Hash128 Id;
#if NETCODE
        [GhostField]
#endif
        public int BaseValue;
#if NETCODE
        [GhostField]
#endif
        public bool SignProtected;
#if NETCODE
        [GhostField]
#endif
        public int Value;

        public StatusInts(Hash128 componentId, Hash128 id, int baseValue, bool signProtected)
        {
            ComponentId = componentId;
            Id = id;
            BaseValue = baseValue;
            SignProtected = signProtected;
            Value = baseValue;
        }

        public StatusInts(Hash128 componentId, global::StatusEffects.StatusInt statusInt)
        {
            ComponentId = componentId;
            Id = statusInt.StatusName.Id;
            BaseValue = statusInt.BaseValue;
            SignProtected = statusInt.SignProtected;
            Value = statusInt.BaseValue;
        }
    }
}
#endif