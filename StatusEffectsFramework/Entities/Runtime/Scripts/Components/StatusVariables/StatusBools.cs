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
    public struct StatusBools : IBufferElementData
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
        public bool BaseValue;
#if NETCODE
        [GhostField]
#endif
        public bool PreEvaluationValue;
#if NETCODE
        [GhostField]
#endif
        public bool PostEvaluationValue;
        public bool Value => PostEvaluationValue;

        public StatusBools(ulong stableTypeHash, Hash128 keyHash, bool baseValue)
        {
            KeyHash = keyHash;
            StableTypeHash = stableTypeHash;
            Id = StatusRegistry.NullId;
            BaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusBools(ulong stableTypeHash, StatusBool statusBool)
        {
            Assert.IsNotNull(statusBool, $"{nameof(StatusBool)} cannot be null when creating a {nameof(StatusBools)} buffer element.");
            Assert.IsFalse(statusBool.StatusName.IsEmpty, $"{nameof(StatusBool.StatusName)} cannot be empty when creating a {nameof(StatusBools)} buffer element.");

            KeyHash = statusBool.StatusName.GetKeyHash();
            StableTypeHash = stableTypeHash;
            Id = StatusRegistry.NullId;
            BaseValue = statusBool.BaseValue;
            PreEvaluationValue = statusBool.BaseValue;
            PostEvaluationValue = statusBool.BaseValue;
        }

        public static implicit operator bool(StatusBools statusBool) => statusBool.Value;
    }
}