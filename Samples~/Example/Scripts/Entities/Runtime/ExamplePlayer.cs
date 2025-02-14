#if ENTITIES
using Unity.Entities;
#if NETCODE_ENTITIES
using Unity.NetCode;
#endif

namespace StatusEffects.Entities.Example
{
    public struct ExamplePlayer : IComponentData
    {
        // This is important to look up StatusVariable
        // data from the dynamic buffer.
#if NETCODE_ENTITIES
        [GhostField]
#endif
        public Hash128 ComponentId;

#if NETCODE_ENTITIES
        [GhostField]
#endif
        public StatusFloat MaxHealth;
#if NETCODE_ENTITIES
        [GhostField]
#endif
        public StatusFloat Speed;
#if NETCODE_ENTITIES
        [GhostField]
#endif
        public StatusInt CoinMultiplier;
#if NETCODE_ENTITIES
        [GhostField]
#endif
        public StatusBool Stunned;
#if NETCODE_ENTITIES
        [GhostField(Quantization = 1000)]
#endif
        public float Health;
    }

    public class Baker : Baker<global::StatusEffects.Example.ExamplePlayer>
    {
        public override void Bake(global::StatusEffects.Example.ExamplePlayer authoring)
        {
            // Dynamic flag not actually needed in this example but most likely your entity moves around.
            var entity = GetEntity(TransformUsageFlags.Dynamic);
            AddComponent(entity, new ExamplePlayer
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