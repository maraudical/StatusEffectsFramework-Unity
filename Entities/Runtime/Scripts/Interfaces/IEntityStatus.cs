#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public interface IEntityStatus
    {
        public Hash128 ComponentId { get; }
        public void OnBake(Entity entity, StatusManagerBaker baker);
    }
}
#endif