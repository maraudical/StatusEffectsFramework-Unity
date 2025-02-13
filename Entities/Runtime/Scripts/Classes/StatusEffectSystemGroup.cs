#if ENTITIES
using Unity.Entities;

namespace StatusEffects.Entities
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation | WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation, WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(VariableRateSimulationSystemGroup))]
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
#if NETCODE
    [UpdateBefore(typeof(Unity.NetCode.GhostSimulationSystemGroup))]
#endif
    public partial class StatusEffectSystemGroup : ComponentSystemGroup { }
}
#endif