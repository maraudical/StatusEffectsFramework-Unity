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

        public StatusBools(TypeIndex typeIndex, Hash128 uniqueKey, bool baseValue)
        {
            UniqueKey = uniqueKey;
            TypeIndex = typeIndex;
            Id = default;
            BaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusBools(TypeIndex typeIndex, StatusBool statusBool)
        {
            Assert.IsNotNull(statusBool, $"{nameof(StatusBool)} cannot be null when creating a {nameof(StatusBools)} buffer element.");
            Assert.IsNotNull(statusBool.StatusName, $"{nameof(StatusBool.StatusName)} cannot be null when creating a {nameof(StatusBools)} buffer element.");

            UniqueKey = statusBool.StatusName.GetUniqueKeyHash(); 
            TypeIndex = typeIndex;
            Id = default;
            BaseValue = statusBool.BaseValue;
            PreEvaluationValue = statusBool.BaseValue;
            PostEvaluationValue = statusBool.BaseValue;
        }
    }
}
#endif