#if ENTITIES
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    public struct UnmanagedEffect
    {
        public Hash128 StatusName;
        public TypeIndex TypeIndex;
        public ValueType ValueType;
        public ValueModifier ValueModifier;
        public ValueSource ValueSource;
        public bool PostEvaluate;
        public int Priority;
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;
    }
}
#endif