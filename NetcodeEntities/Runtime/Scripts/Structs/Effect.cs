#if ENTITIES && ADDRESSABLES
using Unity.Collections;

namespace StatusEffects.NetCode.Entities
{
    public struct Effect
    {
        public FixedString64Bytes Id;
        public ValueModifier ValueModifier;
        public bool UseBaseValue;
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;
        public int Priority;
    }
}
#endif