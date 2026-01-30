#if ENTITIES
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffects.Entities
{
    public struct Module : IComponentData
    {
#if NETCODE
        [GhostField]
#endif
        public Entity Target;
#if NETCODE
        [GhostField(Quantization = 1000)]
#endif
        public float BaseValue;
#if NETCODE
        [GhostField]
#endif
        public int Stacks;
#if NETCODE
        [GhostField]
#endif
        public int PreviousStacks;
    }
}
#endif