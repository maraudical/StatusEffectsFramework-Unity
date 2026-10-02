using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// The <see cref="EntityCommandBufferSystem"/> near the end of the <see cref="StatusEffectSystemGroup"/>.
    /// Plays back the structural changes recorded in this group, such as module and dynamic effect
    /// buffers being added or removed by the <see cref="ModulesSystem"/> and <see cref="DynamicEffectsSystem"/>.
    /// </summary>
    /// <remarks>
    /// This is not the last system in the group. Without Netcode, status variable evaluation runs
    /// after it so it can see the buffers added here in the same frame:
    /// <list type="number">
    /// <item><see cref="DynamicEffectPreEvaluateSystemGroup"/></item>
    /// <item><see cref="StatusVariablePreEvaluateSystem"/></item>
    /// <item><see cref="DynamicEffectPostEvaluateSystemGroup"/></item>
    /// <item><see cref="StatusVariablePostEvaluateSystem"/></item>
    /// </list>
    /// Commands recorded by any of those systems should use a later command buffer system. With
    /// Netcode those systems run in the <c>PredictedStatusEffectSystemGroup</c> instead, after the
    /// <c>EndPredictedStatusEffectEntityCommandBufferSystem</c>.
    /// </remarks>
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    public partial class EndStatusEffectEntityCommandBufferSystem : EntityCommandBufferSystem
    {
        /// <summary>
        /// Call <see cref="SystemAPI.GetSingleton{T}"/> to get this component for this system, and then call
        /// <see cref="CreateCommandBuffer"/> on this singleton to create an ECB to be played back by this system.
        /// </summary>
        /// <remarks>
        /// Useful if you want to record entity commands now, but play them back at a later point in
        /// the frame, or early in the next frame.
        /// </remarks>
        public unsafe struct Singleton : IComponentData, IECBSingleton
        {
            internal UnsafeList<EntityCommandBuffer>* pendingBuffers;
            internal AllocatorManager.AllocatorHandle allocator;

            /// <summary>
            /// Create a command buffer for the parent system to play back.
            /// </summary>
            /// <remarks>The command buffers created by this method are automatically added to the system's list of
            /// pending buffers.</remarks>
            /// <param name="world">The world in which to play it back.</param>
            /// <returns>The command buffer to record to.</returns>
            public EntityCommandBuffer CreateCommandBuffer(WorldUnmanaged world)
            {
                return EntityCommandBufferSystem.CreateCommandBuffer(ref *pendingBuffers, allocator, world);
            }

            /// <summary>
            /// Sets the list of command buffers to play back when this system updates.
            /// </summary>
            /// <remarks>This method is only intended for internal use, but must be in the public API due to language
            /// restrictions. Command buffers created with <see cref="CreateCommandBuffer"/> are automatically added to
            /// the system's list of pending buffers to play back.</remarks>
            /// <param name="buffers">The list of buffers to play back. This list replaces any existing pending command buffers on this system.</param>
            public void SetPendingBufferList(ref UnsafeList<EntityCommandBuffer> buffers)
            {
                pendingBuffers = (UnsafeList<EntityCommandBuffer>*)UnsafeUtility.AddressOf(ref buffers);
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(Allocator allocatorIn)
            {
                allocator = allocatorIn;
            }

            /// <summary>
            /// Set the allocator that command buffers created with this singleton should be allocated with.
            /// </summary>
            /// <param name="allocatorIn">The allocator to use</param>
            public void SetAllocator(AllocatorManager.AllocatorHandle allocatorIn)
            {
                allocator = allocatorIn;
            }
        }
        /// <inheritdoc cref="EntityCommandBufferSystem.OnCreate"/>
        protected override void OnCreate()
        {
            base.OnCreate();

            this.RegisterSingleton<Singleton>(ref PendingBuffers, World.Unmanaged);
        }
    }
    [WorldSystemFilter(WorldSystemFilterFlags.Default | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderFirst = true)]
#if NETCODE
    [UpdateAfter(typeof(PredictedSimulationSystemGroup))]
#else
    [UpdateAfter(typeof(VariableRateSimulationSystemGroup))]
#endif
    [UpdateBefore(typeof(FixedStepSimulationSystemGroup))]
    public partial class StatusEffectSystemGroup : ComponentSystemGroup { }
}