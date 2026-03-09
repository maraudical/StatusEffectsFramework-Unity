using StatusEffectFramework.Samples;
using Unity.Entities;
using UnityEngine;

namespace StatusEffectFramework.Entities.Samples
{
    public class ExamplePlayerBaker : Baker<ExamplePlayer>
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
