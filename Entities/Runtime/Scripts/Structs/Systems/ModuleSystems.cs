#if ENTITIES
using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using UnityEngine;

namespace StatusEffects.Entities
{
    [UpdateInGroup(typeof(SimulationSystemGroup), OrderLast = true)]
    public partial class ModuleSystem : SystemBase
    {
        public EntityQuery m_EntityQuery;

        private const double k_ModuleSystemMaxInactivityTime = 120;

        protected override void OnCreate()
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAllRW<ModuleSystemHandles>().Build();

            RequireForUpdate(m_EntityQuery);

            unsafe
            {
                int value = 5;
                MyStruct copy = new MyStruct { x = 0, z = new MyInt { x = 8 } };
                MyStruct test = new MyStruct { x = 1, z = new MyInt { x = 2 } };
                MyInt inttest = new MyInt { x = 11 };
                void* valuePtr = Unsafe.AsPointer(ref value);
                void* copyPtr = Unsafe.AsPointer(ref copy);
                void* testPtr = Unsafe.AsPointer(ref test);
                void* inttestPtr = Unsafe.AsPointer(ref inttest);

                UnsafeUtility.MemCpy(testPtr, copyPtr, UnsafeUtility.SizeOf<MyStruct>());
                UnsafeUtility.MemCpy(testPtr, valuePtr, UnsafeUtility.SizeOf<int>());
                IntPtr newPtr = IntPtr.Add(new IntPtr(testPtr), UnsafeUtility.SizeOf<int>());
                UnsafeUtility.MemCpy(newPtr.ToPointer(), inttestPtr, UnsafeUtility.SizeOf<MyInt>());


                Debug.Log("SIZE TEST---------------");
                Debug.Log($"MyStruct: {test.x}, {test.z.x}");
            }
        }

        public struct MyInt
        {
            public int x;
        }

        public struct MyStruct
        {
            public int x;
            public MyInt z;
        }

        protected override void OnUpdate()
        {
            var time = SystemAPI.Time.ElapsedTime;

            foreach (var handles in SystemAPI.Query<DynamicBuffer<ModuleSystemHandles>>())
            {
                for (int i = handles.Length - 1; i >= 0; i--)
                {
                    ref var handle = ref handles.ElementAt(i);

                    if (handle.TimeUpdated + k_ModuleSystemMaxInactivityTime > time)
                        continue;

                    var query = EntityManager.CreateEntityQuery(TypeManager.GetType(handle.ModuleTypeIndex));

                    if (!query.IsEmptyIgnoreFilter)
                    {
                        Debug.Log("found some");
                        handle.TimeUpdated = time;
                        continue;
                    }

                    handles.RemoveAtSwapBack(i);

                    // Destroy inactive systems
                    _ = DestroyEndOfFrame(Application.exitCancellationToken, handle);
                    async Awaitable DestroyEndOfFrame(CancellationToken token, ModuleSystemHandles handle)
                    {
                        await Awaitable.EndOfFrameAsync(token);
                        
                        if (token.IsCancellationRequested)
                            return;
                        Debug.Log("removing from !!!" + World.Name);

                        using var attributes = TypeManager.GetSystemAttributes(handle.SystemTypeIndex, TypeManager.SystemAttributeKind.UpdateInGroup);
                        Debug.Log(attributes.Length + " found!!!");
                        if (attributes.Length == 0)
                            World.GetOrCreateSystemManaged<SimulationSystemGroup>().RemoveSystemFromUpdateList(handle.Value);
                        else
                            foreach (var attr in attributes)
                            {
                                Debug.Log("ATTR FOUND!!!");
                                var groupTypeIndex = attr.TargetSystemTypeIndex;
                                var groupSys = World.GetExistingSystemManaged(groupTypeIndex);
                                var group = groupSys as ComponentSystemGroup;

                                if (group != null)
                                {
                                    group.RemoveSystemFromUpdateList(handle.Value);
                                }
                            }
                        Debug.Log("destroyed!!!");
                        World.DestroySystem(handle.Value);
                    }
                }
            }
        }
    }

#if NETCODE
    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(EndPredictedStatusEffectEntityCommandBufferSystem))]
#else
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    [UpdateAfter(typeof(EndStatusEffectEntityCommandBufferSystem))]
#endif
    public partial class ModuleRequestSystem : SystemBase
    {
        public EntityQuery m_EntityQuery;

        protected override void OnCreate()
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAllRW<ModuleSystemRequests>().Build();
            m_EntityQuery.AddChangedVersionFilter(ComponentType.ReadWrite<ModuleSystemRequests>());

            RequireForUpdate(m_EntityQuery);
            RequireForUpdate<ModuleSystemHandles>();
        }

        protected unsafe override void OnUpdate()
        {
            var handles = SystemAPI.GetSingletonBuffer<ModuleSystemHandles>();
            var handlesPtr = handles.GetUnsafePtr();
            var time = SystemAPI.Time.ElapsedTime;

            foreach (var requests in SystemAPI.Query<DynamicBuffer<ModuleSystemRequests>>())
            {
                if (requests.Length <= 0)
                    return;

                using var systemTypeIndexList = new NativeList<SystemTypeIndex>(requests.Length, Allocator.Temp);

                foreach (var request in requests)
                {
                    if (NativeArrayExtensions.Contains<ModuleSystemHandles, ModuleSystemRequests>(handlesPtr, handles.Length, request))
                        continue;
                    systemTypeIndexList.AddNoResize(request.SystemTypeIndex);
                }

                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(World, systemTypeIndexList);

                foreach (var request in requests)
                {
                    handles.Add(new ModuleSystemHandles
                    {
                        ModuleTypeIndex = request.ModuleTypeIndex,
                        SystemTypeIndex = request.SystemTypeIndex,
                        Value = World.GetExistingSystem(request.SystemTypeIndex),
                        TimeUpdated = time
                    });
                }

                requests.Clear();
            }
        }
    }
}
#endif