using StatusEffectFramework.Entities;
using Unity.Entities;
using UnityEngine;

namespace StatusEffectFramework.Samples
{
    public partial class CoinMultiplierToSpeed : DynamicFloatEffect, IEntityDynamicEffect
    {
        public TypeIndex GetTypeIndex() => TypeManager.GetTypeIndex<CoinMultiplierToSpeedComponent>();
    }

    public struct CoinMultiplierToSpeedComponent : IComponentData
    {
        public float Value;
    }
}