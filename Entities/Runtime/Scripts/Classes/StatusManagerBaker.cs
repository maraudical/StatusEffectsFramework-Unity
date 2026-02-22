#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public class StatusManagerBaker : Baker<global::StatusEffects.StatusManager>
    {
        public override void Bake(global::StatusEffects.StatusManager authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<StatusManager>(entity);
            AddComponent<StatusVariableUpdate>(entity);
            AddBuffer<StatusEffects>(entity);
#if NETCODE
            AddBuffer<InterpolatedStatusEffects>(entity);
#endif
            AddBuffer<StatusEffectRequests>(entity);
            AddBuffer<ModuleSystemRequests>(entity);
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