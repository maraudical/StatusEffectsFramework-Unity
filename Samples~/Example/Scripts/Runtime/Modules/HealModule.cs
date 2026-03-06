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

[assembly: RegisterGenericComponentType(typeof(Modules<StatusEffects.Example.HealModuleStruct>))]
#endif

namespace StatusEffects.Example
{
    [CreateAssetMenu(fileName = "Heal Module", menuName = "Status Effect Framework/Modules/Heal Module", order = 1)]
    public class HealModule : Module
#if ENTITIES
        , IEntityModule
    {
        public ModuleInfo CreateModuleInfo(ModuleInstance moduleInstance)
        {
            return (this as IEntityModule).AllocateModule(new HealModuleStruct());
        }
#else
    {
#endif
#if UNITASK
        public override async UniTaskVoid EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            if (!manager.TryGetComponent(out IExamplePlayer player))
                return;
            // Add health according to status effect
            player.Health += statusEffect.Data.BaseValue * statusEffect.Stacks;
            player.Health = Mathf.Min(player.Health, player.MaxHealth);

            statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(player, statusEffect, previous, stack);

            await UniTask.WaitUntilCanceled(token);
            // Note that you need to check if the entity is null in case the
            // cancellation was invoked from the destruction of the MonoBehaviour
            if (player == null)
                return;
            // Clamp health after status effect ends
            player.Health = Mathf.Min(player.Health, player.MaxHealth);
        }
#elif UNITY_2023_1_OR_NEWER
        public override async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            if (!manager.TryGetComponent(out IExamplePlayer player))
                return;
            // Add health according to status effect
            player.Health += statusEffect.Data.BaseValue;
            player.Health = Mathf.Min(player.Health, player.MaxHealth);

            statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(player, statusEffect, previous, stack);
            
            await AwaitableExtensions.WaitUntilCanceled(token);
            // Note that you need to check if the player is null in case the
            // cancellation was invoked from the destruction of the MonoBehaviour
            if (player == null)
                return;
            // Clamp health after status effect ends
            player.Health = Mathf.Min(player.Health, player.MaxHealth);
        }
#else
        public override IEnumerator EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            if (manager.TryGetComponent(out IExamplePlayer player))
            {
                // Add health according to status effect
                player.Health += statusEffect.Data.BaseValue;
                player.Health = Mathf.Min(player.Health, player.MaxHealth);

                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(player, statusEffect, previous, stack);
            }

            yield break;
        }

        public override void DisableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            if (manager.TryGetComponent(out IExamplePlayer player))
                // Clamp health after status effect ends
                player.Health = Mathf.Min(player.Health, player.MaxHealth);
        }
#endif

        private void OnStackUpdate(IExamplePlayer player, StatusEffect statusEffect, int previous, int stack)
        {
            player.Health += statusEffect.Data.BaseValue * Mathf.Max(0, stack - previous);
            player.Health = Mathf.Min(player.Health, player.MaxHealth);
        }
    }
#if ENTITIES

    public struct HealModuleStruct { }

#if NETCODE_ENTITIES
    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
#else
    [UpdateInGroup(typeof(SimulationSystemGroup))]
#endif
    [BurstCompile]
    public partial struct HealModuleSystem : ISystem
    {
        private EntityQuery m_EventQuery;
#if NETCODE_ENTITIES
        private EntityQuery m_RebuildModulesTagQuery;
        private NativeArray<EntityQuery> m_Queries;

#endif
        private TypeIndex m_TypeIndex;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EventQuery = SystemAPI.QueryBuilder().WithAll<ActiveStatusEffects, StatusEffectEvents>().WithAll<Simulate>().Build();

            m_TypeIndex = TypeManager.GetTypeIndex<Modules<HealModuleStruct>>();

#if NETCODE_ENTITIES
            m_RebuildModulesTagQuery = SystemAPI.QueryBuilder().WithAll<ActiveStatusEffects>().WithAll<RebuildModulesTag>().Build();

            m_Queries = new(2, Allocator.Persistent);
            m_Queries[0] = m_EventQuery;
            m_Queries[1] = m_RebuildModulesTagQuery;

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
            var commandBuffer = SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var lookup = SystemAPI.GetBufferLookup<Modules<HealModuleStruct>>();
            var playerLookup = SystemAPI.GetComponentLookup<ExamplePlayerComponent>();
            var statusFloatsLookup = SystemAPI.GetBufferLookup<StatusFloats>();
#if NETCODE_ENTITIES

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
#if NETCODE_ENTITIES

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

                    ref var data = ref reference.Value;

                    if (!StatusEffectsUtility.ModuleInfosContainType(ref data.Modules, TypeIndex))
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
                            StatusEffectsUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffect.Id);
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

            public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<ActiveStatusEffects> statusEffects, in DynamicBuffer<StatusEffectEvents> statusEffectEvents)
            {
                bool foundBuffer = Lookup.TryGetBuffer(entity, out var buffer);
                bool foundPlayer = PlayerLookup.TryGetComponent(entity, out var player);
                bool foundStatusFloats = StatusFloatsLookup.TryGetBuffer(entity, out var statusFloats);
                float maxHealth = default;
                bool isValid = foundPlayer && foundStatusFloats && player.MaxHealth.TryGetValue(player.ComponentId, statusFloats, out maxHealth);
                ActiveStatusEffects statusEffect;

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
                            if (!StatusEffectsUtility.TryGetStatusEffect(statusEffects, statusEffectEvent.Id, out statusEffect))
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
                                StatusEffectsUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffectEvent.Id);

                                if (!isValid)
                                    continue;
                                
                                player.Health += data.BaseValue * math.max(0, statusEffect.Stacks);
                            }

                            break;
                        case StatusEffectEvent.Removed:
                            if (foundBuffer)
                                StatusEffectsUtility.RemoveModulesFromBuffer(ref buffer, statusEffectEvent.Id);
                            break;
                        case StatusEffectEvent.Updated:
                            if (!isValid)
                                break;

                            if (!StatusEffectsUtility.TryGetStatusEffect(statusEffects, statusEffectEvent.Id, out statusEffect))
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
#endif
}