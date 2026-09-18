#if ENTITIES
using Unity.Assertions;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
    public struct StatusFloats : IBufferElementData
    {
        internal Hash128 UniqueKey;
#if NETCODE
        [GhostField(Composite = true)]
#endif
        public TypeIndex TypeIndex;
#if NETCODE
        [GhostField]
#endif
        public ushort Id;
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

        public StatusFloats(TypeIndex typeIndex, Hash128 uniqueKey, float baseValue, bool signProtected = true)
        {
            UniqueKey = uniqueKey;
            TypeIndex = typeIndex;
            Id = default;
            SignProtected = signProtected;
            BaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusFloats(TypeIndex typeIndex, StatusFloat statusFloat)
        {
            Assert.IsNotNull(statusFloat, $"{nameof(StatusFloat)} cannot be null when creating a {nameof(StatusFloats)} buffer element.");
            Assert.IsNotNull(statusFloat.StatusName, $"{nameof(StatusFloat.StatusName)} cannot be null when creating a {nameof(StatusFloats)} buffer element.");

            UniqueKey = statusFloat.StatusName.GetUniqueKeyHash();
            TypeIndex = typeIndex;
            Id = default;
            SignProtected = statusFloat.SignProtected;
            BaseValue = statusFloat.BaseValue;
            PreEvaluationValue = statusFloat.BaseValue;
            PostEvaluationValue = statusFloat.BaseValue;
        }
    }
}
#endif