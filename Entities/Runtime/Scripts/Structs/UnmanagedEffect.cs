#if ENTITIES
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    public struct UnmanagedEffect
    {
        public Hash128 Id;
        public ValueModifier ValueModifier;
        public bool UseBaseValue;
        public float FloatValue;
        public int IntValue;
        public bool BoolValue;
        public int Priority;
    }
}
#endif