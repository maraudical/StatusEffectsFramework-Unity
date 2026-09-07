#if ENTITIES
using NUnit.Framework;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
    public struct StatusFloats : IBufferElementData
    {
#if NETCODE
        [GhostField(Composite = true)]
#endif
        public TypeIndex TypeIndex;
#if NETCODE
        [GhostField]
#endif
        public ushort StatusName;
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

        public StatusFloats(TypeIndex typeIndex, ushort statusName, float baseValue, bool signProtected)
        {
            TypeIndex = typeIndex;
            StatusName = statusName;
            SignProtected = signProtected;
            BaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusFloats(TypeIndex typeIndex, StatusFloat statusFloat)
        {
            TypeIndex = typeIndex;

            if (statusFloat != null && statusFloat.StatusName != null)
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