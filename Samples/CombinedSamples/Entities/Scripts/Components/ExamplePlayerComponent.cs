#if Entities
using StatusEffectsFramework.Entities;
using Unity.Entities;

namespace StatusEffectsFramework.Samples
{
    public struct ExamplePlayerComponent : IComponentData
    {
        public UnmanagedStatusFloat MaxHealth;
        public UnmanagedStatusFloat Speed;
        public UnmanagedStatusInt CoinMultiplier;
        public UnmanagedStatusBool Stunned;
        public float Health;
    }
}
#endif
