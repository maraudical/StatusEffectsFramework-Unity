#if ENTITIES && ADDRESSABLES
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    public struct StatusInts : IBufferElementData
    {
        public FixedString64Bytes ComponentId;

        public FixedString64Bytes Id;
        public int BaseValue;
        public bool SignProtected;
        public int Value;

        public StatusInts(FixedString64Bytes componentId, FixedString64Bytes id, int baseValue, bool signProtected)
        {
            ComponentId = componentId;
            Id = id;
            BaseValue = baseValue;
            SignProtected = signProtected;
            Value = baseValue;
        }

        public StatusInts(FixedString64Bytes componentId, StatusInt statusInt)
        {
            ComponentId = componentId;
            Id = statusInt.StatusName.Id;
            BaseValue = statusInt.BaseValue;
            SignProtected = statusInt.SignProtected;
            Value = statusInt.BaseValue;
        }
    }
}
#endif