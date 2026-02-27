#if ENTITIES
using StatusEffects.Entities;
using Unity.Entities;
#if NETCODE_ENTITIES
using Unity.NetCode;
#endif

namespace StatusEffects.Example
{
#if NETCODE_ENTITIES
    [GhostComponent]
#endif
    public struct ExamplePlayerComponent : IComponentData
    {
        // This is important to look up StatusVariable
        // data from the dynamic buffer.
        public Hash128 ComponentId;
        
        public UnmanagedStatusFloat MaxHealth;
        public UnmanagedStatusFloat Speed;
        public UnmanagedStatusInt CoinMultiplier;
        public UnmanagedStatusBool Stunned;
#if NETCODE_ENTITIES
        [GhostField(Quantization = 1000)]
#endif
        public float Health;
    }

    public class Baker : Baker<ExamplePlayer>
    {
        public override void Bake(ExamplePlayer authoring)
        {
            // Dynamic flag not actually needed in this example but most likely your entity moves around.
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ExamplePlayerComponent
            {
                ComponentId = authoring.ComponentId,
                MaxHealth = authoring.StatusMaxHealth,
                Speed = authoring.StatusSpeed,
                CoinMultiplier = authoring.StatusCoinMultiplier,
                Stunned = authoring.StatusStunned,
                Health = authoring.StatusMaxHealth,
            });
        }
    }
}
#endif