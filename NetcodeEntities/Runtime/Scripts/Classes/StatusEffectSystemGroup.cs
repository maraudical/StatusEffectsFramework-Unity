#if ENTITIES && ADDRESSABLES
using Unity.Entities;

namespace StatusEffects.NetCode.Entities
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(VariableRateSimulationSystemGroup))]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
#if NETCODE
    [UpdateBefore(typeof(Unity.NetCode.GhostSimulationSystemGroup))]
#endif
    public partial class StatusEffectSystemGroup : ComponentSystemGroup { }
}
#endif