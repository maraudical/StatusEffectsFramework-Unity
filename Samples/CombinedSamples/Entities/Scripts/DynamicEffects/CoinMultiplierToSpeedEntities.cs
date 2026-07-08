using StatusEffectsFramework.Entities;
using StatusEffectsFramework.Entities.Samples;

namespace StatusEffectsFramework.Samples
{
    public partial class CoinMultiplierToSpeed : DynamicEffectFloat, IEntityDynamicEffect
    {
        public DynamicEffectInfo CreateDynamicEffectInfo()
        {
            return DynamicEffectInfo.AllocateDynamicEffect(new CoinMultiplierToSpeedStruct(), ValueType.Float);
        }
    }
}