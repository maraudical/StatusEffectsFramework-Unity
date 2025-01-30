#if ENTITIES && ADDRESSABLES
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    public struct StatusFloats : IBufferElementData
    {
        public FixedString64Bytes ComponentId;

        public FixedString64Bytes Id;
        public float BaseValue;
        public bool SignProtected;
        public float Value;

        public StatusFloats(FixedString64Bytes componentId, FixedString64Bytes id, float baseValue, bool signProtected)
        {
            ComponentId = componentId;
            Id = id;
            BaseValue = baseValue;
            SignProtected = signProtected;
            Value = baseValue;
        }

        public StatusFloats(FixedString64Bytes componentId, StatusFloat statusFloat)
        {
            ComponentId = componentId;
            Id = statusFloat.StatusName.Id;
            BaseValue = statusFloat.BaseValue;
            SignProtected = statusFloat.SignProtected;
            Value = statusFloat.BaseValue;
        }
    }
}
#endif