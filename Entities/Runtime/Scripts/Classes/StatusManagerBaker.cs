#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public class StatusManagerBaker : Baker<StatusManager>
    {
        public override void Bake(StatusManager authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<StatusManagerComponent>(entity);
            AddBuffer<ActiveStatusEffects>(entity);
#if NETCODE
            AddBuffer<InterpolatedStatusEffects>(entity);
#endif
            AddBuffer<StatusEffectRequests>(entity);
            AddBuffer<StatusEffectEvents>(entity);
            SetComponentEnabled<StatusEffectEvents>(entity, false);
            AddBuffer<StatusFloats>(entity);
            AddBuffer<StatusInts>(entity);
            AddBuffer<StatusBools>(entity);

            IEntityStatus[] statuses = authoring.GetComponents<IEntityStatus>();

            foreach (var status in statuses)
                status.OnBake(entity, this);
        }
    }
}
#endif