#if ENTITIES
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffects.Entities
{
#if NETCODE
    [GhostComponent]
#endif
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
        // These are specific to the client as they are
        // for recieving immediately updated values via RPC.
        public int ReplicatedStacks;

        public int ReplicatedPreviousStacks;

        public bool IsReplicated;
#endif
        
        // Sending is irrelivant since updates are
        // replicated through rpcs.
        public bool IsBeingUpdated;

        public bool IsBeingDestroyed;
    }
}
#endif