using Unity.Entities;
using UnityEngine;

namespace StatusEffects.Entities
{
    [DisallowMultipleComponent]
    public class PredictedDestroyAuthoring : MonoBehaviour
    {
        public class Baker : Baker<PredictedDestroyAuthoring>
        {
            public override void Bake(PredictedDestroyAuthoring authoring)
            {
                var entity = GetEntity(TransformUsageFlags.None);
                AddComponent<PredictedDestroy>(entity);
            }
        }
    }
}
