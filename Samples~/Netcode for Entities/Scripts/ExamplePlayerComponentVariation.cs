using Unity.NetCode;

namespace StatusEffectFramework.Entities.Samples
{
    [GhostComponentVariation(typeof(ExamplePlayerComponent), "Default")]
    [GhostComponent]
    public struct ExamplePlayerComponentVariation
    {
        [GhostField(Quantization = 1000)]
        public float Health;
    }
}
