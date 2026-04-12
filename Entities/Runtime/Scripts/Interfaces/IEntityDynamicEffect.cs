using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    /// <summary>
    /// If an effect is dynamic, the associated <see cref="DynamicEffect"/> reference must implement this interface otherwise it will be excluded during runtime conversion.
    /// </summary>
    public interface IEntityDynamicEffect
    {
        public TypeIndex GetTypeIndex();
    }
}
