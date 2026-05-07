#if ENTITIES && NETCODE
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation)]
#if NETCODE
    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(EndPredictedStatusEffectEntityCommandBufferSystem))]
#else
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(EndStatusEffectEntityCommandBufferSystem))]
#endif
    [UpdateBefore(typeof(StatusVariablePreEvaluateSystem))]
    public partial class DynamicEffectPreEvaluateSystemGroup : ComponentSystemGroup {  }
}
#endif