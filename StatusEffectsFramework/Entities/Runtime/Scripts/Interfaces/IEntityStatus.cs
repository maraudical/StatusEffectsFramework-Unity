#if ENTITIES
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Entities that need to have their <see cref="StatusVariable"/>s baked must 
    /// implement this interface. Otherwise they will not exist in the relevant 
    /// <see cref="StatusFloats"/>, <see cref="StatusInts"/>, and 
    /// <see cref="StatusBools"/> buffers.
    /// </summary>
    public interface IEntityStatus
    {
        /// <summary>
        /// Add <see cref="StatusVariable"/>s to the <see cref="StatusFloats"/>, <see cref="StatusInts"/>, <see cref="StatusBools"/> buffers.
        /// </summary>
        public void OnBake(Entity entity, StatusManagerBaker baker);
    }
}
#endif