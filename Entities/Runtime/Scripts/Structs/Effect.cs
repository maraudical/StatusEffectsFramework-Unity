#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public struct Effect
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