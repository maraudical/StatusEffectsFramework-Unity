#if ENTITIES
using System;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;

namespace StatusEffects.NetCode.Entities.Example
{
    public struct ExamplePlayer : IComponentData
    {
        // This is important to look up StatusVariable
        // data from the dynamic buffer.
        public FixedString64Bytes ComponentId;

        public FixedString64Bytes MaxHealth;
        public FixedString64Bytes Speed;
        public FixedString64Bytes CoinMultiplier;
        public FixedString64Bytes Stunned;
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
                MaxHealth = authoring.StatusMaxHealth.StatusName.Id,
                Speed = authoring.StatusSpeed.StatusName.Id,
                CoinMultiplier = authoring.StatusCoinMultiplier.StatusName.Id,
                Stunned = authoring.StatusStunned.StatusName.Id,
                Health = authoring.StatusMaxHealth,
            });
        }
    }
}
#endif