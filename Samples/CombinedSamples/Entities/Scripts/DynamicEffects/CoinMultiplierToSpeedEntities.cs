using StatusEffectsFramework.Entities;
using Unity.Entities;

namespace StatusEffectsFramework.Samples
{
    public partial class CoinMultiplierToSpeed : DynamicEffectFloat, IEntityDynamicEffect
    {
        public void CreateDynamicEffectInfo(ValueType valueType, ref DynamicEffectInfo info, ref BlobBuilder builder)
        {
            DynamicEffectInfo.AllocateDynamicEffect(new CoinMultiplierToSpeedStruct(), valueType, ref info, ref builder);
        }
    }
}