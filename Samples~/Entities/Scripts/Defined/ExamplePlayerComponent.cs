using Unity.Entities;

namespace StatusEffectFramework.Entities.Samples
{
    public struct ExamplePlayerComponent : IComponentData
    {
        // This is important to look up StatusVariable
        // data from the dynamic buffer.
        public Hash128 ComponentId;
        
        public UnmanagedStatusFloat MaxHealth;
        public UnmanagedStatusFloat Speed;
        public UnmanagedStatusInt CoinMultiplier;
        public UnmanagedStatusBool Stunned;
        public float Health;
    }
}