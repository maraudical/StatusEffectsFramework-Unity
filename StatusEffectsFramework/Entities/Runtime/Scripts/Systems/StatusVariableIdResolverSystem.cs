#if ENTITIES
using Unity.Burst;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
    internal partial struct StatusVariableIdResolverJob : IJobEntity
    {
        public UnmanagedStatusRegistry Registry;

        void Execute(ref DynamicBuffer<StatusFloats> statusFloats,
            ref DynamicBuffer<StatusInts> statusInts,
            ref DynamicBuffer<StatusBools> statusBools)
        {
            for (int i = 0; i < statusFloats.Length; i++)
            {
                ref var statusFloat = ref statusFloats.ElementAt(i);
                if (!Registry.TryGetId(statusFloat.UniqueKey, out statusFloat.Id))
                    DebugError(statusFloat.UniqueKey);
            }

            for (int i = 0; i < statusInts.Length; i++)
            {
                ref var statusInt = ref statusInts.ElementAt(i);
                if (!Registry.TryGetId(statusInt.UniqueKey, out statusInt.Id))
                    DebugError(statusInt.UniqueKey);
            }

            for (int i = 0; i < statusBools.Length; i++)
            {
                ref var statusBool = ref statusBools.ElementAt(i);
                if (!Registry.TryGetId(statusBool.UniqueKey, out statusBool.Id))
                    DebugError(statusBool.UniqueKey);
            }

            void DebugError(Hash128 uniqueKey) => UnityEngine.Debug.LogError($"A status variable that contains the unique key hash \"{uniqueKey}\" was not found in the registry. Make sure all registry dependencies are loaded.");
        }
    }

    /// <summary>
    /// Resolves the status variable ids of every entity with a <see cref="StatusResolver"/> component,
    /// then removes the component so the entity is only resolved once.
    /// </summary>
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderFirst = true)]
    [UpdateAfter(typeof(UnmanagedStatusRegistrySetupSystem))]
    [BurstCompile]
    public partial struct StatusVariableIdResolverSystem : ISystem
    {
        EntityQuery m_ResolverQuery;
        EntityQuery m_RequestQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ResolverQuery = SystemAPI.QueryBuilder().WithAll<StatusResolver>().WithAllRW<StatusFloats>().WithAllRW<StatusInts>().WithAllRW<StatusBools>().WithOptions(EntityQueryOptions.IncludePrefab | EntityQueryOptions.IncludeDisabledEntities | EntityQueryOptions.IgnoreComponentEnabledState).Build();
            m_RequestQuery = SystemAPI.QueryBuilder().WithAll<UnmanagedStatusRegistrySetupRequest>().Build();

            state.RequireForUpdate(m_ResolverQuery);
            state.RequireForUpdate<UnmanagedStatusRegistry>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var commandBuffer = SystemAPI.GetSingleton<EndStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged);

            // Capture the entities now so anything created before playback still gets resolved next update.
            commandBuffer.RemoveComponent<StatusResolver>(m_ResolverQuery, EntityQueryCaptureMode.AtPlayback);

            // The registry is being rebuilt so the current one may already be disposed. The
            // UnmanagedStatusRegistrySetupSystem resolves every entity once the new one is created.
            if (!m_RequestQuery.IsEmptyIgnoreFilter)
                return;

            var job = new StatusVariableIdResolverJob
            {
                Registry = SystemAPI.GetSingleton<UnmanagedStatusRegistry>()
            };
            state.Dependency = job.ScheduleParallelByRef(m_ResolverQuery, state.Dependency);
        }
    }
}
#endif
