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
        internal ulong StableTypeHash;
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

        public StatusFloats(ulong stableTypeHash, Hash128 uniqueKey, float baseValue, bool signProtected = true)
        {
            UniqueKey = uniqueKey;
            StableTypeHash = stableTypeHash;
            Id = default;
            SignProtected = signProtected;
            BaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusFloats(ulong stableTypeHash, StatusFloat statusFloat)
        {
            Assert.IsNotNull(statusFloat, $"{nameof(StatusFloat)} cannot be null when creating a {nameof(StatusFloats)} buffer element.");
            Assert.IsNotNull(statusFloat.StatusName, $"{nameof(StatusFloat.StatusName)} cannot be null when creating a {nameof(StatusFloats)} buffer element.");

            UniqueKey = statusFloat.StatusName.GetUniqueKeyHash();
            StableTypeHash = stableTypeHash;
            Id = default;
            SignProtected = statusFloat.SignProtected;
            BaseValue = statusFloat.BaseValue;
            PreEvaluationValue = statusFloat.BaseValue;
            PostEvaluationValue = statusFloat.BaseValue;
        }

        public static implicit operator float(StatusFloats statusFloat) => statusFloat.Value;
    }
}
#endif