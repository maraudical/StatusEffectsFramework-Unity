#if ENTITIES
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    public struct UnmanagedEffect
    {
        public Hash128 StatusName;
        public ValueModifier ValueModifier;
        public ValueType ValueType;
        public int Priority;
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;
    }
}
#endif