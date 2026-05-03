using StatusEffectFramework.Entities;
using System.Threading;
using Unity.Entities;
using UnityEngine;

namespace StatusEffectFramework.Samples
{
    public partial class InverseHealthToSpeed : DynamicFloatEffect, IEntityDynamicEffect
    {
        public TypeIndex GetTypeIndex() => TypeManager.GetTypeIndex<InverseHealthToSpeedComponent>();
    }

    public struct InverseHealthToSpeedComponent : IComponentData
    {
        public float Value;
    }
}