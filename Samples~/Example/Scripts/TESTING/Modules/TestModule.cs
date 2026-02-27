using StatusEffects.Entities;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(Modules<StatusEffects.Example.TestModuleStruct>))]

namespace StatusEffects.Example
{
    [CreateAssetMenu(fileName = "TestModule", menuName = "Status Effect Framework/Modules/TestModule", order = 1)]
    [AttachModuleInstance(typeof(TestModuleInstance))]
    public class TestModule : Module, IEntityModule
    {
        public ModuleInfo CreateModuleInfo(ModuleInstance moduleInstance)
        {
            var testModuleInstance = moduleInstance as TestModuleInstance;
            var testModuleStruct = new TestModuleStruct
            {
                TestValue = testModuleInstance.TestValue,
            };
            return (this as IEntityModule).AllocateModule(testModuleStruct);
        }
    }

    // Should be almost identical to your ModuleInstance except with only burstable types.
    public struct TestModuleStruct
    {
        public int TestValue;
    }

    //[UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct TestModuleSystem : ISystem
    {
        private EntityQuery m_EventQuery;
        private TypeIndex m_TypeIndex;
        //private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EventQuery = SystemAPI.QueryBuilder().WithAll<ActiveStatusEffects, StatusEffectEvents>().WithAll<Simulate>().Build();
            m_TypeIndex = TypeManager.GetTypeIndex<Modules<TestModuleStruct>>();
            //m_EntityQuery = SystemAPI.QueryBuilder().WithAll<Modules<TestModuleStruct>>().Build();

            state.RequireForUpdate<StatusReferences>();
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var networkTime = SystemAPI.GetSingleton<NetworkTime>();
            var statusReferences = SystemAPI.GetSingleton<StatusReferences>();
            //var commandBuffer = SystemAPI.GetSingleton<EndPredictedSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter();
            var testModuleStructLookup = SystemAPI.GetBufferLookup<Modules<TestModuleStruct>>();

            /*if (networkTime.IsFirstPredictionTick)
            {
                var firstPredictionTickJob = new TestModuleFirstPredictionTickJob
                {
                    TypeIndex = m_TypeIndex,
                    References = statusReferences,
                    Lookup = testModuleStructLookup,
                    EventsLookup = SystemAPI.GetBufferLookup<StatusEffectEvents>()
                };
                state.Dependency = firstPredictionTickJob.ScheduleParallelByRef(state.Dependency);
            }*/

            var eventJob = new TestModuleEventJob
            {
                IsServer = state.WorldUnmanaged.IsServer(),
                TypeIndex = m_TypeIndex,
                References = statusReferences,
                CommandBuffer = commandBuffer,
                Lookup = testModuleStructLookup,
            };
            state.Dependency = eventJob.ScheduleParallelByRef(m_EventQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        [WithAll(typeof(Simulate))]
        partial struct TestModuleFirstPredictionTickJob : IJobEntity
        {
            public TypeIndex TypeIndex;
            public StatusReferences References;
            [NativeDisableParallelForRestriction]
            public BufferLookup<Modules<TestModuleStruct>> Lookup;
            [ReadOnly]
            public BufferLookup<StatusEffectEvents> EventsLookup;

            public void Execute(Entity entity, in DynamicBuffer<ActiveStatusEffects> statusEffects)
            {
                if (!Lookup.TryGetBuffer(entity, out var buffer))
                    return;

                buffer.Clear();
                bool foundEvents = EventsLookup.TryGetBuffer(entity, out var events);
                var eventsArray = foundEvents ? events.AsNativeArray() : default;

                foreach (var statusEffect in statusEffects)
                {
                    if (foundEvents)
                    {
                        int index = eventsArray.IndexOf(statusEffect.Id);
                        if (index >= 0 && eventsArray[index].Event is StatusEffectEvent.Added)
                            continue;
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

                        var module = StatusEffectsUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffect.Id);
                    }
                }
            }
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct TestModuleEventJob : IJobEntity
        {
            public bool IsServer;
            public TypeIndex TypeIndex;
            public StatusReferences References;
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [NativeDisableParallelForRestriction]
            public BufferLookup<Modules<TestModuleStruct>> Lookup;

            public void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<ActiveStatusEffects> statusEffects, in DynamicBuffer<StatusEffectEvents> statusEffectEvents)
            {
                bool foundBuffer = Lookup.TryGetBuffer(entity, out var buffer);

                foreach (var statusEffectEvent in statusEffectEvents)
                {
                    if (!References.TryGetReference(statusEffectEvent.StatusEffectDataId, out var reference))
                        continue;

                    if (!StatusEffectsUtility.ModuleInfosContainType(ref reference.Value.Modules, TypeIndex))
                        continue;

                    switch (statusEffectEvent.Event)
                    {
                        case StatusEffectEvent.Added:
                            if (!IsServer)
                                UnityEngine.Debug.Log("test added");
                            if (!foundBuffer)
                            {
                                foundBuffer = true;
                                buffer = CommandBuffer.AddBuffer<Modules<TestModuleStruct>>(sortKey, entity);
                            }

                            ref var modules = ref reference.Value.Modules;
                            for (int i = 0; i < modules.Length; i++)
                            {
                                var moduleInfo = modules[i];

                                if (moduleInfo.TypeIndex != TypeIndex)
                                    continue;

                                var module = StatusEffectsUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffectEvent.Id);
                                // Put specific logic when added here.
                            }

                            break;
                        case StatusEffectEvent.Removed:
                            if (!IsServer)
                                UnityEngine.Debug.Log("test removed");
                            if (foundBuffer)
                                StatusEffectsUtility.RemoveModulesFromBuffer(ref buffer, statusEffectEvent.Id);
                            // Put specific logic when removed here. Do not use the buffer since the modules have already been removed.
                            break;
                        /*case StatusEffectEvent.Updated:
                            foreach (var module in buffer)
                                if (module.Id == statusEffectEvent.Id)
                                {
                                    Put specific logic when updated here.
                                }
                            break;*/
                    }
                }

                if (foundBuffer && buffer.Length <= 0)
                    CommandBuffer.RemoveComponent<Modules<TestModuleStruct>>(sortKey, entity);
            }
        }
    }
}