#if ENTITIES
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Group for systems that update post evaluated dynamic effect values. Runs after the
    /// <see cref="StatusVariablePreEvaluateSystem"/>, so pre evaluation values can be read, and before
    /// the <see cref="StatusVariablePostEvaluateSystem"/> applies those effects.
    /// </summary>
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