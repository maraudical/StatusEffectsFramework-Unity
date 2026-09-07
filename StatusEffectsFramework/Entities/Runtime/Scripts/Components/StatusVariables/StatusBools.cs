#if ENTITIES
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
    public struct StatusBools : IBufferElementData
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

        public StatusBools(TypeIndex typeIndex, ushort statusName, bool baseValue)
        {
            TypeIndex = typeIndex;
            StatusName = statusName;
            BaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusBools(TypeIndex typeIndex, StatusBool statusBool)
        {
            TypeIndex = typeIndex;
            if (statusBool != null && statusBool.StatusName)
            {
                StatusName = statusBool.StatusName.Id;
                BaseValue = statusBool.BaseValue;
                PreEvaluationValue = statusBool.BaseValue;
                PostEvaluationValue = statusBool.BaseValue;
            }
            else
            {
                StatusName = default;
                BaseValue = default;
                PreEvaluationValue = default;
                PostEvaluationValue = default;
            }
        }
    }
}
#endif