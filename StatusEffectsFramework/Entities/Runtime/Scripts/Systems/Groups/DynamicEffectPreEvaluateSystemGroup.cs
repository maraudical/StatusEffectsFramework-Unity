#if ENTITIES
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Group for systems that update dynamic effect values before status variables are evaluated.
    /// Runs after the end status effect command buffer system, so dynamic effect buffers added this
    /// update already exist, and before the <see cref="StatusVariablePreEvaluateSystem"/>.
    /// </summary>
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