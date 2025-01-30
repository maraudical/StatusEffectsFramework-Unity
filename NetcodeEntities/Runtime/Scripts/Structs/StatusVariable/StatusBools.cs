#if ENTITIES && ADDRESSABLES
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    public struct StatusBools : IBufferElementData
    {
        public FixedString64Bytes ComponentId;

        public FixedString64Bytes Id;
        public bool BaseValue;
        public bool Value;

        public StatusBools(FixedString64Bytes componentId, FixedString64Bytes id, bool baseValue)
        {
            ComponentId = componentId;
            Id = id;
            BaseValue = baseValue;
            Value = baseValue;
        }

        public StatusBools(FixedString64Bytes componentId, StatusBool statusBool)
        {
            ComponentId = componentId;
            Id = statusBool.StatusName.Id;
            BaseValue = statusBool.BaseValue;
            Value = statusBool.BaseValue;
        }
    }
}
#endif