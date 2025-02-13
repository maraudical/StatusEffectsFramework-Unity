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
        [GhostField(SendData = false)]
        public int ReplicatedStacks;

        [GhostField(SendData = false)]
        public int ReplicatedPreviousStacks;

        [GhostField(SendData = false)]
        public bool IsReplicated;
#endif

#if NETCODE
        // Sending is irrelivant since updates are
        // replicated through rpcs.
        [GhostField(SendData = false)]
#endif
        public bool IsBeingUpdated;
#if NETCODE
        // Sending is irrelivant since it gets destroyed
        // before any clients will notice the change.
        [GhostField(SendData = false)]
#endif
        public bool IsBeingDestroyed;
    }
}
#endif