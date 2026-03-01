using UnityEngine;
#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
#elif UNITY_2023_1_OR_NEWER
using StatusEffects.Extensions;
using System.Threading;
#else
using System.Collections;
#endif
#if NETCODE_ENTITIES
using Unity.NetCode;
#endif
#if ENTITIES
using StatusEffects.Entities;
using Unity.Entities;
using Unity.Burst;
using Unity.Collections;
using Unity.Mathematics;

[assembly: RegisterGenericComponentType(typeof(Modules<StatusEffects.Example.DamageOverTimeModuleStruct>))]
#endif

namespace StatusEffects.Example
{
    [CreateAssetMenu(fileName = "Damage Over Time Module", menuName = "Status Effect Framework/Modules/Damage Over Time", order = 1)]
    [AttachModuleInstance(typeof(DamageOverTimeInstance))]
    public class DamageOverTimeModule : Module
#if ENTITIES
        , IEntityModule
    {
        public ModuleInfo CreateModuleInfo(ModuleInstance moduleInstance)
        {
            var instance = moduleInstance as DamageOverTimeInstance;
            var moduleStruct = new DamageOverTimeModuleStruct
            {
                IntervalSeconds = instance.IntervalSeconds,
            };
            return (this as IEntityModule).AllocateModule(moduleStruct);
        }
#else
    {
#endif
#if UNITASK
        public override async UniTaskVoid EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            if (manager.TryGetComponent(out IExamplePlayer player))
                while (!token.IsCancellationRequested)
                {
                    // Reduce DamageOverTimeth based on the Statu Effect base value
                    player.Health -= statusEffect.Data.BaseValue * statusEffect.Stacks;
                    // Wait for the interval before applying the damage again
                    await UniTask.WaitForSeconds(damageOverTimeInstance.IntervalSeconds);
                }
        }
#elif UNITY_2023_1_OR_NEWER
        public override async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            if (manager.TryGetComponent(out IExamplePlayer player))
                while (!token.IsCancellationRequested)
                {
                    // Reduce DamageOverTimeth based on the Statu Effect base value
                    player.DamageOverTimeth -= statusEffect.Data.BaseValue * statusEffect.Stacks;
                    // Wait for the interval before applying the damage again
                    await Awaitable.WaitForSecondsAsync(damageOverTimeInstance.IntervalSeconds);
                }
        }
#else
        public override IEnumerator EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            DamageOverTimeInstance damageOverTimeInstance = moduleInstance as DamageOverTimeInstance;

            if (manager.TryGetComponent(out IExamplePlayer player))
                for (; ; )
                {
                    // Reduce DamageOverTimeth based on the Statu Effect base value
                    player.DamageOverTimeth -= statusEffect.Data.BaseValue * statusEffect.Stacks;
                    // Wait for the interval before applying the damage again
                    yield return new WaitForSeconds(damageOverTimeInstance.IntervalSeconds);
                }
        }
#endif
    }
#if ENTITIES

    public struct DamageOverTimeModuleStruct 
    { 
        public float IntervalSeconds;
        public int TimesDamaged;
    }

#if NETCODE_ENTITIES
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
#else
    [UpdateInGroup(typeof(SimulationSystemGroup))]
#endif
    [BurstCompile]
    public partial struct DamageOverTimeModuleSystem : ISystem
    {
        private EntityQuery m_ModuleQuery;
        private EntityQuery m_EventQuery;
#if NETCODE_ENTITIES
        private EntityQuery m_RebuildModulesTagQuery;
        private NativeArray<EntityQuery> m_Queries;

#endif
        private TypeIndex m_TypeIndex;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_ModuleQuery = SystemAPI.QueryBuilder().WithAll<ActiveStatusEffects, ExamplePlayerComponent, Modules<DamageOverTimeModuleStruct>>().WithAll<Simulate>().Build();
            m_EventQuery = SystemAPI.QueryBuilder().WithAll<ActiveStatusEffects, StatusEffectEvents>().WithAll<Simulate>().Build();

            m_TypeIndex = TypeManager.GetTypeIndex<Modules<DamageOverTimeModuleStruct>>();

#if NETCODE_ENTITIES
            m_RebuildModulesTagQuery = SystemAPI.QueryBuilder().WithAll<ActiveStatusEffects>().WithAll<RebuildModulesTag>().Build();

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

        [BurstCompile]
        public void OnDestroy(ref SystemState state)
        {
            m_Queries.Dispose();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statusReferences = SystemAPI.GetSingleton<StatusReferences>();
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var lookup = SystemAPI.GetBufferLookup<Modules<DamageOverTimeModuleStruct>>();
            var playerLookup = SystemAPI.GetComponentLookup<ExamplePlayerComponent>();
            var statusFloatsLookup = SystemAPI.GetBufferLookup<StatusFloats>();
#if NETCODE_ENTITIES
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
#if NETCODE_ENTITIES
                NetworkTime = networkTime,
                TickRate = tickRate,
#else
                ElapsedTime = SystemAPI.Time.ElapsedTime,
#endif
                References = statusReferences,
            };
            state.Dependency = damageOverTimeJob.ScheduleParallelByRef(m_ModuleQuery, state.Dependency);
        }
#if NETCODE_ENTITIES

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

            public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<ActiveStatusEffects> statusEffects)
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

                    if (!StatusEffectsUtility.ModuleInfosContainType(ref reference.Value.Modules, TypeIndex))
                        continue;

                    ref var modules = ref reference.Value.Modules;
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
                            ref var module = ref StatusEffectsUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffect.Id);
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
                in DynamicBuffer<ActiveStatusEffects> statusEffects, 
                in DynamicBuffer<StatusEffectEvents> statusEffectEvents)
            {
                bool foundBuffer = Lookup.TryGetBuffer(entity, out var buffer);

                foreach (var statusEffectEvent in statusEffectEvents)
                {
                    if (!References.TryGetReference(statusEffectEvent.StatusEffectDataId, out var reference))
                        continue;

                    ref var data = ref reference.Value;

                    if (!StatusEffectsUtility.ModuleInfosContainType(ref data.Modules, TypeIndex))
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
                                var module = StatusEffectsUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffectEvent.Id);
                            }

                            break;
                        case StatusEffectEvent.Removed:
                            if (foundBuffer)
                                StatusEffectsUtility.RemoveModulesFromBuffer(ref buffer, statusEffectEvent.Id);
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
#if NETCODE_ENTITIES
            public NetworkTime NetworkTime;
            public ClientServerTickRate TickRate;
#else
            public double ElapsedTime;
#endif

            public void Execute(in DynamicBuffer<ActiveStatusEffects> statusEffects, 
                ref ExamplePlayerComponent player, 
                ref DynamicBuffer<Modules<DamageOverTimeModuleStruct>> modules)
            {
                ActiveStatusEffects statusEffect;

                for (int i = 0; i < modules.Length; i++)
                {
                    ref var module = ref modules.ElementAt(i);

                    if (!StatusEffectsUtility.TryGetStatusEffect(statusEffects, module.Id, out statusEffect))
                        continue;

                    if (!References.TryGetReference(statusEffect.StatusEffectDataId, out var reference))
                        continue;

                    ref var data = ref reference.Value;

#if NETCODE_ENTITIES
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
    }
#endif
            }
