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
            var ghostAuthoring = GetComponentInParent<Unity.NetCode.GhostAuthoringComponent>();
            if (ghostAuthoring != null)
            {
                AddBuffer<InterpolatedStatusEffects>(entity);
                AddComponent<PredictedStatusEffectsFlag>(entity);

                // Only children need it, the root has PredictedGhost itself.
                var root = GetEntity(ghostAuthoring, TransformUsageFlags.None);
                if (root != entity)
                    AddComponent(entity, new StatusEffectsGhostRoot { Root = root });
            }
#endif
            AddBuffer<StatusEffectRequests>(entity);
            AddBuffer<StatusEffectEvents>(entity);
            SetComponentEnabled<StatusEffectEvents>(entity, false);
            AddBuffer<StatusFloats>(entity);
            AddBuffer<StatusInts>(entity);
            AddBuffer<StatusBools>(entity);
            AddComponent<StatusResolver>(entity);
            AddComponent<StatusVariablePreEvaluateUpdate>(entity);
            SetComponentEnabled<StatusVariablePreEvaluateUpdate>(entity, false);
            AddComponent<StatusVariablePostEvaluateUpdate>(entity);
            SetComponentEnabled<StatusVariablePostEvaluateUpdate>(entity, false);
            AddBuffer<ZeroLengthModules>(entity);
            SetComponentEnabled<ZeroLengthModules>(entity, false);

            IEntityStatus[] statuses = authoring.GetComponents<IEntityStatus>();

            foreach (var status in statuses)
                status.OnBake(entity, this);
        }
    }
}