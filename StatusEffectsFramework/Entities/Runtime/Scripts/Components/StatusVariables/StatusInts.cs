#if ENTITIES
using Unity.Assertions;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
    public struct StatusInts : IBufferElementData
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
        [GhostField]
#endif
        public int BaseValue;
#if NETCODE
        [GhostField]
#endif
        public int PreEvaluationValue;
#if NETCODE
        [GhostField]
#endif
        public int PostEvaluationValue;
        public int Value => PostEvaluationValue;

        public StatusInts(TypeIndex typeIndex, Hash128 uniqueKey, int baseValue, bool signProtected = true)
        {
            UniqueKey = uniqueKey;
            TypeIndex = typeIndex;
            Id = default;
            SignProtected = signProtected;
            BaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusInts(TypeIndex typeIndex, StatusInt statusInt)
        {
            Assert.IsNotNull(statusInt, $"{nameof(StatusInt)} cannot be null when creating a {nameof(StatusInts)} buffer element.");
            Assert.IsNotNull(statusInt.StatusName, $"{nameof(StatusInt.StatusName)} cannot be null when creating a {nameof(StatusInts)} buffer element.");

            UniqueKey = statusInt.StatusName.GetUniqueKeyHash(); 
            TypeIndex = typeIndex;
            Id = default;
            SignProtected = statusInt.SignProtected;
            BaseValue = statusInt.BaseValue;
            PreEvaluationValue = statusInt.BaseValue;
            PostEvaluationValue = statusInt.BaseValue;
        }
    }
}
#endif