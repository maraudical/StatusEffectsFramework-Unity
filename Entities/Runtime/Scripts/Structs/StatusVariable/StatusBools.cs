#if ENTITIES
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    public struct StatusBools : IBufferElementData
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
        public bool BaseValue;
#if NETCODE
        [GhostField]
#endif
        public bool Value;

        public StatusBools(Hash128 componentId, Hash128 id, bool baseValue)
        {
            ComponentId = componentId;
            Id = id;
            BaseValue = baseValue;
            Value = baseValue;
        }

        public StatusBools(Hash128 componentId, global::StatusEffects.StatusBool statusBool)
        {
            ComponentId = componentId;
            Id = statusBool.StatusName.Id;
            BaseValue = statusBool.BaseValue;
            Value = statusBool.BaseValue;
        }
    }
}
#endif