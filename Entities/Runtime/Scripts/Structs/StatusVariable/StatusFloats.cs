#if ENTITIES
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    public struct StatusFloats : IBufferElementData
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
        [GhostField(Quantization = 1000)]
#endif
        public float BaseValue;
#if NETCODE
        [GhostField]
#endif
        public bool SignProtected;
#if NETCODE
        [GhostField(Quantization = 1000)]
#endif
        public float Value;

        public StatusFloats(Hash128 componentId, Hash128 id, float baseValue, bool signProtected)
        {
            ComponentId = componentId;
            Id = id;
            BaseValue = baseValue;
            SignProtected = signProtected;
            Value = baseValue;
        }

        public StatusFloats(Hash128 componentId, global::StatusEffects.StatusFloat statusFloat)
        {
            ComponentId = componentId;
            Id = statusFloat.StatusName.Id;
            BaseValue = statusFloat.BaseValue;
            SignProtected = statusFloat.SignProtected;
            Value = statusFloat.BaseValue;
        }
    }
}
#endif