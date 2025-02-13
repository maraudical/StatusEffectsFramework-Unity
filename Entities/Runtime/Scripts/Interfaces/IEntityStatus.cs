#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    public interface IEntityStatus
    {
        public Hash128 ComponentId { get; }
        /// <summary>
        /// Add <see cref="StatusVariable"/>s to the <see cref="StatusFloats"/>, <see cref="StatusInts"/>, <see cref="StatusBools"/> buffers.
        /// </summary>
        public void OnBake(Entity entity, StatusManagerBaker baker);
    }
}
#endif