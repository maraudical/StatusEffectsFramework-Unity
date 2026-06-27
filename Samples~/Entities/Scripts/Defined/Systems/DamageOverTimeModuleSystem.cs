using StatusEffectFramework.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[assembly: RegisterGenericComponentType(typeof(Modules<StatusEffectFramework.Entities.Samples.DamageOverTimeModuleStruct>))]

namespace StatusEffectFramework.Entities.Samples
{
    public struct DamageOverTimeModuleStruct
    {
        public float IntervalSeconds;
        public int TimesDamaged;
    }
    /*
#if NETCODE
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
#else
    [UpdateInGroup(typeof(SimulationSystemGroup))]
#endif
    [BurstCompile]
    [DisableAutoCreation]
    public partial struct DamageOverTimeModuleSystem : ISystem
    {
        private EntityQuery m_ModuleQuery;
        private EntityQuery m_EventQuery;
#if NETCODE
        private EntityQuery m_RebuildModulesTagQuery;
        private NativeArray<EntityQuery> m_Queries;

#endif
        private TypeIndex m_TypeIndex;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModuleQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, ExamplePlayerComponent, Modules<DamageOverTimeModuleStruct>>().WithAll<Simulate>().Build();
            m_EventQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, StatusEffectEvents>().WithAll<Simulate>().Build();

            m_TypeIndex = TypeManager.GetTypeIndex<Modules<DamageOverTimeModuleStruct>>();

#if NETCODE
            m_RebuildModulesTagQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects>().WithAll<RebuildModulesTag>().Build();

            m_Queries = new(3, Allocator.Persistent);
            m_Queries[0] = m_ModuleQuery;
            m_Queries[1] = m_EventQuery;
            m_Queries[2] = m_RebuildModulesTagQuery;

            state.RequireForUpdate<NetworkTime>();
            state.RequireAnyForUpdate(m_Queries);
#else
            state.RequireForUpdate(m_EventQuery);
#endif
            state.RequireForUpdate<StatusReferences>();
        }
#if NETCODE

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            m_Queries.Dispose();
        }
#endif

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statusReferences = SystemAPI.GetSingleton<StatusReferences>();
            var commandBuffer = SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var lookup = SystemAPI.GetBufferLookup<Modules<DamageOverTimeModuleStruct>>();
            var playerLookup = SystemAPI.GetComponentLookup<ExamplePlayerComponent>();
            var statusFloatsLookup = SystemAPI.GetBufferLookup<StatusFloats>();
#if NETCODE
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            SystemAPI.TryGetSingleton<ClientServerTickRate>(out var tickRate);
            tickRate.ResolveDefaults();

            var firstPredictionTickJob = new DamageOverTimeModuleFirstPredictionTickJob
            {
                IsServer = state.WorldUnmanaged.IsServer(),
                TypeIndex = m_TypeIndex,
                NetworkTime = networkTime,
                TickRate = tickRate,
                References = statusReferences,
                CommandBuffer = commandBuffer,
                Lookup = lookup,
                EventsLookup = SystemAPI.GetBufferLookup<StatusEffectEvents>(true)
            };
            state.Dependency = firstPredictionTickJob.ScheduleParallelByRef(m_RebuildModulesTagQuery, state.Dependency);
#endif

            var eventJob = new DamageOverTimeEventJob
            {
                TypeIndex = m_TypeIndex,
                References = statusReferences,
                CommandBuffer = commandBuffer,
                Lookup = lookup,
            };
            state.Dependency = eventJob.ScheduleParallelByRef(m_EventQuery, state.Dependency);

            var damageOverTimeJob = new DamageOverTimeJob
            {
#if NETCODE
                NetworkTime = networkTime,
                TickRate = tickRate,
#else
                ElapsedTime = SystemAPI.Time.ElapsedTime,
#endif
                References = statusReferences,
            };
            state.Dependency = damageOverTimeJob.ScheduleParallelByRef(m_ModuleQuery, state.Dependency);
        }
#if NETCODE

        [BurstCompile]
        partial struct DamageOverTimeModuleFirstPredictionTickJob : IJobEntity
        {
            public bool IsServer;
            public TypeIndex TypeIndex;
            public NetworkTime NetworkTime;
            public ClientServerTickRate TickRate;
            public StatusReferences References;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [NativeDisableParallelForRestriction]
            public BufferLookup<Modules<DamageOverTimeModuleStruct>> Lookup;
            [ReadOnly]
            public BufferLookup<StatusEffectEvents> EventsLookup;

            public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<StatusEffects> statusEffects)
            {
                bool foundBuffer = Lookup.TryGetBuffer(entity, out var buffer);

                if (foundBuffer)
                    buffer.Clear();

                bool foundEvents = EventsLookup.TryGetBuffer(entity, out var events);
                var eventsArray = events.AsNativeArray();

                foreach (var statusEffect in statusEffects)
                {
                    bool willBeAdded = false;

                    if (foundEvents)
                    {
                        int index = eventsArray.IndexOf(statusEffect.Id);
                        if (index >= 0 && eventsArray[index].Event is StatusEffectEvent.Added)
                            willBeAdded = true;
                    }

                    if (!References.TryGetReference(statusEffect.StatusEffectDataId, out var reference))
                        continue;

                    ref var data = ref reference.Value;

                    if (!StatusEffectsECSUtility.ModuleInfosContainType(ref data.Modules, TypeIndex))
                        continue;

                    ref var modules = ref data.Modules;
                    for (int i = 0; i < modules.Length; i++)
                    {
                        var moduleInfo = modules[i];

                        if (moduleInfo.TypeIndex != TypeIndex)
                            continue;

                        if (!foundBuffer)
                        {
                            foundBuffer = true;
                            buffer = CommandBuffer.AddBuffer<Modules<DamageOverTimeModuleStruct>>(sortKey, entity);
                        }

                        if (!willBeAdded)
                        {
                            ref var module = ref StatusEffectsECSUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffect.Id);
                            // Recalculate how many times this module would have been damaged for the current prediction tick.
                            float timeSinceAdded = NetworkTime.ServerTick.TimeSince(statusEffect.TickAdded, NetworkTime.ServerTickFraction, TickRate);
                            module.Value.TimesDamaged = (int)(timeSinceAdded / module.Value.IntervalSeconds);
                        }
                    }
                }

                if (foundBuffer && buffer.Length <= 0)
                    CommandBuffer.RemoveComponent<Modules<DamageOverTimeModuleStruct>>(sortKey, entity);
            }
        }
#endif

        [BurstCompile]
        partial struct DamageOverTimeEventJob : IJobEntity
        {
            public TypeIndex TypeIndex;
            public StatusReferences References;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [NativeDisableParallelForRestriction]
            public BufferLookup<Modules<DamageOverTimeModuleStruct>> Lookup;

            public void Execute([ChunkIndexInQuery] int sortKey,
                Entity entity,
                in DynamicBuffer<StatusEffects> statusEffects,
                in DynamicBuffer<StatusEffectEvents> statusEffectEvents)
            {
                bool foundBuffer = Lookup.TryGetBuffer(entity, out var buffer);

                foreach (var statusEffectEvent in statusEffectEvents)
                {
                    if (!References.TryGetReference(statusEffectEvent.StatusEffectDataId, out var reference))
                        continue;

                    ref var data = ref reference.Value;

                    if (!StatusEffectsECSUtility.ModuleInfosContainType(ref data.Modules, TypeIndex))
                        continue;

                    switch (statusEffectEvent.Event)
                    {
                        case StatusEffectEvent.Added:
                            ref var modules = ref data.Modules;
                            for (int i = 0; i < modules.Length; i++)
                            {
                                var moduleInfo = modules[i];

                                if (moduleInfo.TypeIndex != TypeIndex)
                                    continue;

                                if (!foundBuffer)
                                {
                                    foundBuffer = true;
                                    buffer = CommandBuffer.AddBuffer<Modules<DamageOverTimeModuleStruct>>(sortKey, entity);
                                }
                                var module = StatusEffectsECSUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffectEvent.Id);
                            }

                            break;
                        case StatusEffectEvent.Removed:
                            if (foundBuffer)
                                StatusEffectsECSUtility.RemoveModulesFromBuffer(ref buffer, statusEffectEvent.Id);
                            break;
                    }
                }

                if (foundBuffer && buffer.Length <= 0)
                    CommandBuffer.RemoveComponent<Modules<DamageOverTimeModuleStruct>>(sortKey, entity);
            }
        }

        [BurstCompile]
        partial struct DamageOverTimeJob : IJobEntity
        {
            public StatusReferences References;
#if NETCODE
            public NetworkTime NetworkTime;
            public ClientServerTickRate TickRate;
#else
            public double ElapsedTime;
#endif

            public void Execute(in DynamicBuffer<StatusEffects> statusEffects,
                ref ExamplePlayerComponent player,
                ref DynamicBuffer<Modules<DamageOverTimeModuleStruct>> modules)
            {
                StatusEffects statusEffect;

                for (int i = 0; i < modules.Length; i++)
                {
                    ref var module = ref modules.ElementAt(i);

                    if (!StatusEffectsECSUtility.TryGetStatusEffect(statusEffects, module.Id, out statusEffect))
                        continue;

                    if (!References.TryGetReference(statusEffect.StatusEffectDataId, out var reference))
                        continue;

                    ref var data = ref reference.Value;

#if NETCODE
                    float timeSinceAdded = NetworkTime.ServerTick.TimeSince(statusEffect.TickAdded, NetworkTime.ServerTickFraction, TickRate);
                    while (timeSinceAdded >= module.Value.IntervalSeconds * module.Value.TimesDamaged)
#else
                    while (Time >= module.Value.TimesDamaged * module.Value.IntervalSeconds + statusEffect.TimeAdded)
#endif
                    {
                        module.Value.TimesDamaged++;
                        player.Health -= data.BaseValue * statusEffect.Stacks;
                    }
                }

                player.Health = math.max(player.Health, 0);
            }
        }
    }*/
}