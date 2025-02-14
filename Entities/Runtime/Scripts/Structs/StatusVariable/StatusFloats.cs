#if ENTITIES
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffects.Entities
{
    public struct StatusFloats : IBufferElementData
    {
#if NETCODE
        [GhostField]
#endif
        public Hash128 ComponentId;
#if NETCODE
        [GhostField]
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
            if (statusFloat != null && statusFloat.StatusName)
            {
                Id = statusFloat.StatusName.Id;
                BaseValue = statusFloat.BaseValue;
                SignProtected = statusFloat.SignProtected;
                Value = statusFloat.BaseValue;
            }
            else
            {
                Id = default;
                BaseValue = default;
                SignProtected = default;
                Value = default;
            }
        }
    }
}
#endif