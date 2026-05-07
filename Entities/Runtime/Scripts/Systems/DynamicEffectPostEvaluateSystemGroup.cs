#if ENTITIES && NETCODE
using Unity.Entities;

namespace StatusEffectFramework.Entities
{
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation)]
#if NETCODE
    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
#else
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
#endif
    [UpdateAfter(typeof(StatusVariablePreEvaluateSystem))]
    [UpdateBefore(typeof(StatusVariablePostEvaluateSystem))]
    public partial class DynamicEffectPostEvaluateSystemGroup : ComponentSystemGroup {  }
}
#endif