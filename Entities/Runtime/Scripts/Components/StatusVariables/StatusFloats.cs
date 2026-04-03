#if ENTITIES
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectFramework.Entities
{
    public struct StatusFloats : IBufferElementData
    {
#if NETCODE
        [GhostField(Composite = true)]
#endif
        public Hash128 ComponentId;
#if NETCODE
        [GhostField(Composite = true)]
#endif
        public Hash128 StatusName;
#if NETCODE
        [GhostField]
#endif
        public bool SignProtected;
#if NETCODE
        [GhostField(Quantization = 1000)]
#endif
        public float BaseValue;
#if NETCODE
        [GhostField(Quantization = 1000)]
#endif
        public float PreEvaluationValue;
#if NETCODE
        [GhostField(Quantization = 1000)]
#endif
        public float PostEvaluationValue;
        public float Value => PostEvaluationValue;

        public StatusFloats(Hash128 componentId, Hash128 id, float baseValue, bool signProtected)
        {
            ComponentId = componentId;
            StatusName = id;
            SignProtected = signProtected;
            BaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusFloats(Hash128 componentId, StatusFloat statusFloat)
        {
            ComponentId = componentId;
            if (statusFloat != null && statusFloat.StatusName)
            {
                StatusName = statusFloat.StatusName.Id;
                SignProtected = statusFloat.SignProtected;
                BaseValue = statusFloat.BaseValue;
                PreEvaluationValue = statusFloat.BaseValue;
                PostEvaluationValue = statusFloat.BaseValue;
            }
            else
            {
                StatusName = default;
                SignProtected = default;
                BaseValue = default;
                PreEvaluationValue = default;
                PostEvaluationValue = default;
            }
        }
    }
}
#endif