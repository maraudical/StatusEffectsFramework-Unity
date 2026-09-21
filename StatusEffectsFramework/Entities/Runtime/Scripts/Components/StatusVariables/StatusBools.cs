#if ENTITIES
using Unity.Assertions;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
    public struct StatusBools : IBufferElementData
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

        public StatusBools(ulong stableTypeHash, Hash128 uniqueKey, bool baseValue)
        {
            UniqueKey = uniqueKey;
            StableTypeHash = stableTypeHash;
            Id = default;
            BaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusBools(ulong stableTypeHash, StatusBool statusBool)
        {
            Assert.IsNotNull(statusBool, $"{nameof(StatusBool)} cannot be null when creating a {nameof(StatusBools)} buffer element.");
            Assert.IsNotNull(statusBool.StatusName, $"{nameof(StatusBool.StatusName)} cannot be null when creating a {nameof(StatusBools)} buffer element.");

            UniqueKey = statusBool.StatusName.GetUniqueKeyHash();
            StableTypeHash = stableTypeHash;
            Id = default;
            BaseValue = statusBool.BaseValue;
            PreEvaluationValue = statusBool.BaseValue;
            PostEvaluationValue = statusBool.BaseValue;
        }

        public static implicit operator bool(StatusBools statusBool) => statusBool.Value;
    }
}
#endif