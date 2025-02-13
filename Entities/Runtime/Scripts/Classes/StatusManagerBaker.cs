#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public class StatusManagerBaker : Baker<StatusManager>
    {
        public override void Bake(StatusManager authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<StatusVariableUpdate>(entity);
            AddBuffer<StatusEffects>(entity);
            AddBuffer<StatusEffectRequests>(entity);
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