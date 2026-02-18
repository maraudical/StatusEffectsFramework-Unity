#if ENTITIES
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.Entities
{
    [UpdateInGroup(typeof(StatusEffectSystemGroup))]
    public partial class ModuleSystem : SystemBase
    {
        public EntityQuery m_EntityQuery;

        private const double k_ModuleSystemMaxInactivityTime = 20;

        protected override void OnCreate()
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAllRW<ModuleSystems>().Build();

            RequireForUpdate(m_EntityQuery);
        }

        protected override void OnUpdate()
        {
            var moduleSystemJob = new ModuleSystemJob
            {
                Time = SystemAPI.Time.ElapsedTime,
                World = World,
                WorldUnmanaged = World.Unmanaged,
            };
            Dependency = moduleSystemJob.Schedule(m_EntityQuery, Dependency);
        }
            
        partial struct ModuleSystemJob : IJobEntity
        {
            public double Time;
            public World World;
            public WorldUnmanaged WorldUnmanaged;

            public unsafe void Execute(ref DynamicBuffer<ModuleSystems> moduleSystems)
            {
                for (int i = moduleSystems.Length - 1; i >= 0; i--)
                {
                    ref var moduleSystem = ref moduleSystems.ElementAt(i);
                    uint lastSystemVersion = WorldUnmanaged.ResolveSystemStateRef(moduleSystem.SystemHandle).LastSystemVersion;

                    if (moduleSystem.LastSystemVersion == lastSystemVersion)
                    {
                        if (moduleSystem.TimeUpdated + k_ModuleSystemMaxInactivityTime > Time)
                            continue;

                        // Destroy inactive systems
                        using var attributes = TypeManager.GetSystemAttributes(TypeManager.GetTypeIndexFromStableTypeHash(moduleSystem.StableTypeHash).Index, TypeManager.SystemAttributeKind.UpdateInGroup);
                        
                        World.DestroySystem(moduleSystem.SystemHandle);
                        moduleSystems.RemoveAtSwapBack(i);
                    }
                    else
                    {
                        moduleSystem.LastSystemVersion = lastSystemVersion;
                        moduleSystem.TimeUpdated = Time;
                    }
                }
            }
        }
    }

#if NETCODE
    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndPredictedStatusEffectEntityCommandBufferSystem))]
#else
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    [UpdateBefore(typeof(EndStatusEffectEntityCommandBufferSystem))]
#endif
    public partial class ModuleRequestSystem : SystemBase
    {
        public EntityQuery m_EntityQuery;

        protected override void OnCreate()
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAllRW<ModuleSystemRequests, ModuleSystems>().Build();
            m_EntityQuery.AddChangedVersionFilter(ComponentType.ReadWrite<ModuleSystemRequests>());

            RequireForUpdate(m_EntityQuery);
        }

        protected override void OnUpdate()
        {
            var moduleRequestSystemJob = new ModuleRequestSystemJob
            {
                Time = SystemAPI.Time.ElapsedTime,
                World = World,
                WorldUnmanaged = World.Unmanaged
            };
            Dependency = moduleRequestSystemJob.Schedule(m_EntityQuery, Dependency);
        }

        partial struct ModuleRequestSystemJob : IJobEntity
        {
            public double Time;
            public World World;
            public WorldUnmanaged WorldUnmanaged;

            public unsafe void Execute(ref DynamicBuffer<ModuleSystemRequests> moduleSystemRequests, ref DynamicBuffer<ModuleSystems> moduleSystems)
            {
                var list = new NativeList<SystemTypeIndex>(moduleSystemRequests.Length, Allocator.Temp);
                foreach (var request in moduleSystemRequests)
                {
                    if (NativeArrayExtensions.Contains<ModuleSystems, ModuleSystemRequests>(moduleSystems.GetUnsafeReadOnlyPtr(), moduleSystems.Length, request))
                        continue;
                    list.AddNoResize(TypeManager.GetTypeIndexFromStableTypeHash(request.StableTypeHash).Index);
                    moduleSystems.Add(new ModuleSystems
                    {
                        StableTypeHash = request.StableTypeHash,
                        SystemHandle = WorldUnmanaged.
                    })
                }  
                
                DefaultWorldInitialization.AddSystemsToRootLevelSystemGroups(World, list);
                moduleSystemRequests.Clear();
            }
        }
    }
}
#endif