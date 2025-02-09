#if ENTITIES
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    public struct Module : IComponentData
    {
#if NETCODE
        [GhostField]
#endif
        public Entity Parent;
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
#if NETCODE
        [GhostField]
#endif
        public bool IsBeingUpdated;
#if NETCODE
        [GhostField]
#endif
        public bool IsBeingDestroyed;
    }
}
#endif