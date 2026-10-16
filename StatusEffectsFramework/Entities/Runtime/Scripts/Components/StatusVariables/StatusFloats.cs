using Unity.Assertions;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
#if NETCODE
    [GhostComponent(SendDataForChildEntity = true)]
#endif
    public struct StatusFloats : IBufferElementData
    {
        internal Hash128 KeyHash;
        internal ulong StableTypeHash;
#if NETCODE
        [GhostField(SendData = false)]
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

        public StatusFloats(ulong stableTypeHash, Hash128 keyHash, float baseValue, bool signProtected = true)
        {
            KeyHash = keyHash;
            StableTypeHash = stableTypeHash;
            Id = StatusRegistry.NullId;
            SignProtected = signProtected;
            BaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusFloats(ulong stableTypeHash, StatusFloat statusFloat)
        {
            Assert.IsNotNull(statusFloat, $"{nameof(StatusFloat)} cannot be null when creating a {nameof(StatusFloats)} buffer element.");
            Assert.IsFalse(statusFloat.StatusName.IsEmpty, $"{nameof(StatusFloat.StatusName)} cannot be empty when creating a {nameof(StatusFloats)} buffer element.");

            KeyHash = statusFloat.StatusName.GetKeyHash();
            StableTypeHash = stableTypeHash;
            Id = StatusRegistry.NullId;
            SignProtected = statusFloat.SignProtected;
            BaseValue = statusFloat.BaseValue;
            PreEvaluationValue = statusFloat.BaseValue;
            PostEvaluationValue = statusFloat.BaseValue;
        }

        public static implicit operator float(StatusFloats statusFloat) => statusFloat.Value;
    }
}