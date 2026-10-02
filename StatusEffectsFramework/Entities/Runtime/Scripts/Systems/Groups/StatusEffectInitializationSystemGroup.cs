using Unity.Entities;
using Unity.Scenes;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Builds the <see cref="UnmanagedStatusRegistry"/> and resolves status variable ids before any simulation runs,
    /// including Netcode prediction. Runs after the <see cref="SceneSystemGroup"/> so entities streamed in this
    /// frame are resolved before they are simulated. Structural changes recorded here are played back by the
    /// <see cref="EndInitializationEntityCommandBufferSystem"/>.
    /// </summary>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(InitializationSystemGroup))]
    [UpdateAfter(typeof(SceneSystemGroup))]
    public partial class StatusEffectInitializationSystemGroup : ComponentSystemGroup { }
}
