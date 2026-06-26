using StatusEffectFramework.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.NetCode;

[assembly: RegisterGenericComponentType(typeof(Modules<StatusEffectFramework.Entities.Samples.HealModuleStruct>))]

namespace StatusEffectFramework.Entities.Samples
{
    public struct HealModuleStruct { }

#if NETCODE
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
#else
    [UpdateInGroup(typeof(SimulationSystemGroup))]
#endif
    [BurstCompile]
    public partial struct HealModuleSystem : ISystem
    {
        private EntityQuery m_EventQuery;
#if NETCODE
        private EntityQuery m_RebuildModulesTagQuery;
        private NativeArray<EntityQuery> m_Queries;

#endif
        private TypeIndex m_TypeIndex;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EventQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, StatusEffectEvents>().WithAll<Simulate>().Build();
            m_TypeIndex = TypeManager.GetTypeIndex<Modules<HealModuleStruct>>();

            if (state.WorldUnmanaged.IsServer())
            {
                
                UnityEngine.Debug.Log($"Heal: {m_TypeIndex.Value}");
                UnityEngine.Debug.Log($"DamageOverTime: {TypeManager.GetTypeIndex<Modules<DamageOverTimeModuleStruct>>().Value}");
            }

#if NETCODE
            m_RebuildModulesTagQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects>().WithAll<RebuildModulesTag>().Build();

            m_Queries = new(2, Allocator.Persistent);
            m_Queries[0] = m_EventQuery;
            m_Queries[1] = m_RebuildModulesTagQuery;

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
            var lookup = SystemAPI.GetBufferLookup<Modules<HealModuleStruct>>();
            var playerLookup = SystemAPI.GetComponentLookup<ExamplePlayerComponent>();
            var statusFloatsLookup = SystemAPI.GetBufferLookup<StatusFloats>();
#if NETCODE

            var firstPredictionTickJob = new HealModuleFirstPredictionTickJob
            {
                IsServer = state.WorldUnmanaged.IsServer(),
                TypeIndex = m_TypeIndex,
                References = statusReferences,
                CommandBuffer = commandBuffer,
                Lookup = lookup,
                EventsLookup = SystemAPI.GetBufferLookup<StatusEffectEvents>(true)
            };
            state.Dependency = firstPredictionTickJob.ScheduleParallelByRef(m_RebuildModulesTagQuery, state.Dependency);
#endif

            var eventJob = new HealEventJob
            {
                TypeIndex = m_TypeIndex,
                References = statusReferences,
                CommandBuffer = commandBuffer,
                Lookup = lookup,
                PlayerLookup = playerLookup,
                StatusFloatsLookup = statusFloatsLookup,
            };
            state.Dependency = eventJob.ScheduleParallelByRef(m_EventQuery, state.Dependency);
        }
#if NETCODE

        [BurstCompile]
        partial struct HealModuleFirstPredictionTickJob : IJobEntity
        {
            public bool IsServer;
            public TypeIndex TypeIndex;
            public StatusReferences References;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [NativeDisableParallelForRestriction]
            public BufferLookup<Modules<HealModuleStruct>> Lookup;
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
                            buffer = CommandBuffer.AddBuffer<Modules<HealModuleStruct>>(sortKey, entity);
                        }

                        if (!willBeAdded)
                            StatusEffectsECSUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffect.Id);
                    }
                }

                if (foundBuffer && buffer.Length <= 0)
                    CommandBuffer.RemoveComponent<Modules<HealModuleStruct>>(sortKey, entity);
            }
        }
#endif

        [BurstCompile]
        partial struct HealEventJob : IJobEntity
        {
            public TypeIndex TypeIndex;
            public StatusReferences References;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [NativeDisableParallelForRestriction]
            public BufferLookup<Modules<HealModuleStruct>> Lookup;
            [NativeDisableParallelForRestriction]
            public ComponentLookup<ExamplePlayerComponent> PlayerLookup;
            [ReadOnly]
            public BufferLookup<StatusFloats> StatusFloatsLookup;

            public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<StatusEffects> statusEffects, in DynamicBuffer<StatusEffectEvents> statusEffectEvents)
            {
                bool foundBuffer = Lookup.TryGetBuffer(entity, out var buffer);
                bool foundPlayer = PlayerLookup.TryGetComponent(entity, out var player);
                bool foundStatusFloats = StatusFloatsLookup.TryGetBuffer(entity, out var statusFloats);
                float maxHealth = default;
                bool isValid = foundPlayer && foundStatusFloats && player.MaxHealth.TryGetValue(player.ComponentId, statusFloats, out maxHealth);
                StatusEffects statusEffect;

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
                            if (!StatusEffectsECSUtility.TryGetStatusEffect(statusEffects, statusEffectEvent.Id, out statusEffect))
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
                                    buffer = CommandBuffer.AddBuffer<Modules<HealModuleStruct>>(sortKey, entity);
                                }
                                StatusEffectsECSUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffectEvent.Id);

                                if (!isValid)
                                    continue;

                                player.Health += data.BaseValue * math.max(0, statusEffect.Stacks);
                            }

                            break;
                        case StatusEffectEvent.Removed:
                            if (foundBuffer)
                                StatusEffectsECSUtility.RemoveModulesFromBuffer(ref buffer, statusEffectEvent.Id);
                            break;
                        case StatusEffectEvent.Updated:
                            if (!isValid)
                                break;

                            if (!StatusEffectsECSUtility.TryGetStatusEffect(statusEffects, statusEffectEvent.Id, out statusEffect))
                                continue;

                            foreach (var module in buffer)
                                if (module.Id == statusEffectEvent.Id)
                                    player.Health += data.BaseValue * math.max(0, statusEffect.Stacks - statusEffectEvent.PreviousStacks);
                            break;
                    }
                }

                if (foundBuffer && buffer.Length <= 0)
                    CommandBuffer.RemoveComponent<Modules<HealModuleStruct>>(sortKey, entity);

                if (isValid)
                {
                    player.Health = math.min(player.Health, maxHealth);
                    PlayerLookup[entity] = player;
                }
            }
        }
    }
}