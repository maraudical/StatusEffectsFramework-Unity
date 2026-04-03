#if ENTITIES
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectFramework.Entities
{
    public struct StatusInts : IBufferElementData
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

        public StatusInts(Hash128 componentId, Hash128 id, int baseValue, bool signProtected)
        {
            ComponentId = componentId;
            StatusName = id;
            SignProtected = signProtected;
            BaseValue = baseValue;
            PreEvaluationValue = baseValue;
            PostEvaluationValue = baseValue;
        }

        public StatusInts(Hash128 componentId, StatusInt statusInt)
        {
            ComponentId = componentId;
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