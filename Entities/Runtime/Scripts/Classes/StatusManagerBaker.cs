#if ENTITIES
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    public class StatusManagerBaker : Baker<StatusManager>
    {
        public override void Bake(StatusManager authoring)
        {
            var entity = GetEntity(TransformUsageFlags.None);
            AddComponent<StatusManagerComponent>(entity);
            AddBuffer<StatusEffects>(entity);
#if NETCODE
            AddBuffer<InterpolatedStatusEffects>(entity);
#endif
            AddBuffer<StatusEffectRequests>(entity);
            AddBuffer<StatusEffectEvents>(entity);
            SetComponentEnabled<StatusEffectEvents>(entity, false);
            AddBuffer<StatusFloats>(entity);
            AddBuffer<StatusInts>(entity);
            AddBuffer<StatusBools>(entity);
            AddComponent<StatusVariablePreEvaluateUpdate>(entity);
            SetComponentEnabled<StatusVariablePreEvaluateUpdate>(entity, false);
            AddComponent<StatusVariablePostEvaluateUpdate>(entity);
            SetComponentEnabled<StatusVariablePostEvaluateUpdate>(entity, false);
            AddBuffer<ZeroLengthModules>(entity);

            IEntityStatus[] statuses = authoring.GetComponents<IEntityStatus>();

            foreach (var status in statuses)
                status.OnBake(entity, this);
        }
    }
}
#endif