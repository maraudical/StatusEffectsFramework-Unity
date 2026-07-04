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
        public Hash128 ComponentId;
#if NETCODE
        [GhostField(Composite = true)]
#endif
        public Hash128 StatusName;
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

        public StatusBools(Hash128 componentId, Hash128 id, bool baseValue)
        {
            ComponentId = componentId;
            StatusName = id;
            BaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusBools(Hash128 componentId, StatusBool statusBool)
        {
            ComponentId = componentId;
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