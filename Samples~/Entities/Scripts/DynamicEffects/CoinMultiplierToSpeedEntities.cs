using StatusEffectFramework.Entities;
using StatusEffectsFramework.Entities.Samples;
using Unity.Entities;

namespace StatusEffectFramework.Samples
{
    public partial class CoinMultiplierToSpeed : DynamicEffectFloat, IEntityDynamicEffect
    {
        public TypeIndex GetTypeIndex() => TypeManager.GetTypeIndex<CoinMultiplierToSpeedComponent>();
        public DynamicEffectInfo CreateDynamicEffectInfo() => default;
    }
}