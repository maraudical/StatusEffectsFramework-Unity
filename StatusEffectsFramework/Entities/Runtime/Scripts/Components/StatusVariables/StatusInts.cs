#if ENTITIES
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
    public struct StatusInts : IBufferElementData
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

        public StatusInts(TypeIndex typeIndex, ushort statusName, int baseValue, bool signProtected)
        {
            TypeIndex = typeIndex;
            StatusName = statusName;
            SignProtected = signProtected;
            BaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusInts(TypeIndex typeIndex, StatusInt statusInt)
        {
            TypeIndex = typeIndex;

            if (statusInt != null && statusInt.StatusName)
            {
                StatusName = statusInt.StatusName.Id;
                SignProtected = statusInt.SignProtected;
                BaseValue = statusInt.BaseValue;
                PreEvaluationValue = statusInt.BaseValue;
                PostEvaluationValue = statusInt.BaseValue;
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