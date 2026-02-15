using StatusEffects.Entities;
using StatusEffects.Modules;
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(Modules<TestModuleStruct>))]

namespace StatusEffects.Modules
{
    [CreateAssetMenu(fileName = "TestModule", menuName = "Status Effect Framework/Modules/TestModule", order = 1)]
    [AttachModuleInstance(typeof(TestModuleInstance))]
    public class TestModule : Module, IEntityModule
    {
        public Type ModuleSystemType() => typeof(TestModuleSystem);

        public Type ModuleStructType() => typeof(Modules<TestModuleStruct>);

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

    public struct TestModuleStruct
    {
        public int TestValue;
    }

    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [BurstCompile]
    public partial struct TestModuleSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents/*, Modules<TestModuleStruct>*/>().Build();
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var testModuleJob = new TestModuleJob
            {
                WorldName = state.WorldUnmanaged.Name,
            };
            state.Dependency = testModuleJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct TestModuleJob : IJobEntity
        {
            public FixedString128Bytes WorldName;

            public void Execute(in DynamicBuffer<StatusEffectEvents> events/*, ref DynamicBuffer<Modules<TestModuleStruct>> modules*/)
            {
                foreach (var @event in events)
                {
                    switch (@event.Event)
                    {
                        case StatusEffectEvent.Added:
                            Debug.Log($"[{WorldName}] TestModuleStruct added!");
                            break;
                        case StatusEffectEvent.Removed:
                            Debug.Log($"[{WorldName}] TestModuleStruct removed!");
                            break;
                        case StatusEffectEvent.Updated:
                            Debug.Log($"[{WorldName}] TestModuleStruct updated!");
                            break;
                        /*case StatusEffectEvent.Added:
                            for (int i = 0; i < modules.Length; i++)
                            {
                                var module = modules.ElementAt(i);
                                if (module.Value.TestValue == 5)
                                {
                                    Debug.Log($"[{WorldName}] TestModuleStruct with TestValue 5 applied!");
                                }
                            }
                            break;
                        case StatusEffectEvent.Removed:
                            for (int i = 0; i < modules.Length; i++)
                            {
                                var module = modules.ElementAt(i);
                                if (module.Value.TestValue == 5)
                                {
                                    Debug.Log($"[{WorldName}] TestModuleStruct with TestValue 5 removed!");
                                }
                            }
                            break;
                        case StatusEffectEvent.Updated:
                            for (int i = 0; i < modules.Length; i++)
                            {
                                var module = modules.ElementAt(i);
                                if (module.Value.TestValue == 5)
                                {
                                    Debug.Log($"[{WorldName}] TestModuleStruct with TestValue 5 updated!");
                                }
                            }
                            break;*/
                    }
                }
            }
        }
    }

    [UpdateInGroup(typeof(PredictedSimulationSystemGroup))]
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [BurstCompile]
    public partial struct TEMPPredictedTestModuleSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents, Simulate>().Build();
            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var testModuleJob = new TestModuleJob
            {
                WorldName = state.WorldUnmanaged.Name,
            };
            state.Dependency = testModuleJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct TestModuleJob : IJobEntity
        {
            public FixedString128Bytes WorldName;

            public void Execute(in DynamicBuffer<StatusEffectEvents> events)
            {
                foreach (var @event in events)
                {
                    switch (@event.Event)
                    {
                        case StatusEffectEvent.Added:
                            Debug.Log($"[{WorldName}] PREDICTED TestModuleStruct added!");
                            break;
                        case StatusEffectEvent.Removed:
                            Debug.Log($"[{WorldName}] PREDICTED TestModuleStruct removed!");
                            break;
                        case StatusEffectEvent.Updated:
                            Debug.Log($"[{WorldName}] PREDICTED TestModuleStruct updated!");
                            break;
                    }
                }
            }
        }
    }
}