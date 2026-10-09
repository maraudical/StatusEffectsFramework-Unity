#if Netcode_for_Entities
using StatusEffectsFramework.Entities;
using Unity.NetCode;

namespace StatusEffectsFramework.Samples
{
    [GhostComponentVariation(typeof(ExamplePlayerComponent), "Default")]
    [GhostComponent]
    public struct ExamplePlayerComponentVariation
    {
        [GhostField(Quantization = 1000)]
        public float Health;
    }
}

#endif
