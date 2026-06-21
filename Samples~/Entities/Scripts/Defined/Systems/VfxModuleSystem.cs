using StatusEffectFramework.Entities;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(Modules<StatusEffectFramework.Entities.Samples.VfxModuleStruct>))]
[assembly: RegisterGenericComponentType(typeof(ModuleEvents<StatusEffectFramework.Entities.Samples.VfxModuleStruct>))]

namespace StatusEffectFramework.Entities.Samples
{
    public struct VfxModuleStruct
    {
        public UnityObjectRef<GameObject> Prefab;
        public bool IsLooping;
    }

    public struct VfxModuleCleanup : ICleanupBufferElementData
    {
        public uint Id;
        public UnityObjectRef<GameObject> Value;
    }

#if NETCODE
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
#endif
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class VfxModuleSystem : SystemBase
    {
        private EntityQuery m_CleanupQuery;
        private EntityQuery m_EventQuery;
        public EntityQuery[] m_Queries;

        private TypeIndex m_TypeIndex;

        protected override void OnCreate()
        {
            m_CleanupQuery = SystemAPI.QueryBuilder().WithAll<VfxModuleCleanup>().Build();
            m_EventQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, StatusEffectEvents>().Build();

            m_TypeIndex = TypeManager.GetTypeIndex<Modules<VfxModuleStruct>>();

            m_Queries = new EntityQuery[]
            {
                m_CleanupQuery,
                m_EventQuery
            };

            RequireAnyForUpdate(m_Queries);
            RequireForUpdate<StatusReferences>();
        }

        protected override void OnUpdate()
        {
            var statusReferences = SystemAPI.GetSingleton<StatusReferences>();
            var commandBuffer = SystemAPI.GetSingleton<EndSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(World.Unmanaged);
            var statusEffectsLookup = SystemAPI.GetBufferLookup<StatusEffects>(true);
            var statusEffectEventsLookup = SystemAPI.GetBufferLookup<StatusEffectEvents>(true);
            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var moduleLookup = SystemAPI.GetBufferLookup<Modules<VfxModuleStruct>>();
            var cleanupLookup = SystemAPI.GetBufferLookup<VfxModuleCleanup>();

            using var entities = m_EventQuery.ToEntityArray(Allocator.Temp);
            StatusEffects statusEffect;

            foreach (var entity in entities)
            {
                var statusEffects = statusEffectsLookup[entity];
                var statusEffectEvents = statusEffectEventsLookup[entity];

                bool foundBuffer = moduleLookup.TryGetBuffer(entity, out var buffer);
                bool foundCleanupBuffer = cleanupLookup.TryGetBuffer(entity, out var cleanupBuffer);

                foreach (var statusEffectEvent in statusEffectEvents)
                {
                    if (!statusReferences.TryGetReference(statusEffectEvent.StatusEffectDataId, out var reference))
                        continue;

                    ref var data = ref reference.Value;

                    if (!StatusEffectsECSUtility.ModuleInfosContainType(ref data.Modules, m_TypeIndex))
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

                                if (moduleInfo.TypeIndex != m_TypeIndex)
                                    continue;

                                if (!foundBuffer)
                                {
                                    foundBuffer = true;
                                    buffer = commandBuffer.AddBuffer<Modules<VfxModuleStruct>>(entity);
                                }

                                if (!foundCleanupBuffer)
                                {
                                    foundCleanupBuffer = true;
                                    cleanupBuffer = commandBuffer.AddBuffer<VfxModuleCleanup>(entity);
                                }

                                var module = StatusEffectsECSUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffectEvent.Id);
#if NETCODE
                                // Special case where we don't want old events to instantiate VFX.
                                if (!module.Value.IsLooping && statusEffectEvent.IsOld)
                                    continue;

#endif
                                localToWorldLookup.TryGetComponent(entity, out var localToWorld);
                                var vfxObject = UnityEngine.Object.Instantiate(module.Value.Prefab, localToWorld.Position, localToWorld.Rotation) as GameObject;

                                cleanupBuffer.Add(new VfxModuleCleanup
                                {
                                    Id = module.Id,
                                    Value = vfxObject,
                                });
                            }

                            break;
                        case StatusEffectEvent.Removed:
                            if (foundBuffer)
                                StatusEffectsECSUtility.RemoveModulesFromBuffer(ref buffer, statusEffectEvent.Id);

                            if (!foundCleanupBuffer)
                                break;

                            for (int i = cleanupBuffer.Length - 1; i >= 0; i--)
                            {
                                var cleanup = cleanupBuffer[i];

                                if (cleanup.Id != statusEffectEvent.Id)
                                    continue;

                                cleanupBuffer.RemoveAtSwapBack(i);

                                if (!cleanup.Value.IsValid())
                                    continue;

                                cleanup.Value.Value.GetComponent<ParticleSystem>()?.Stop();
                            }

                            break;
                        case StatusEffectEvent.Updated:
#if NETCODE
                            // Special case where we don't want old events to instantiate VFX.
                            if (statusEffectEvent.IsOld)
                                continue;

#endif
                            if (!foundCleanupBuffer)
                            {
                                foundCleanupBuffer = true;
                                cleanupBuffer = commandBuffer.AddBuffer<VfxModuleCleanup>(entity);
                            }

                            if (!StatusEffectsECSUtility.TryGetStatusEffect(statusEffects, statusEffectEvent.Id, out statusEffect))
                                continue;

                            foreach (var module in buffer)
                            {
                                if (module.Value.IsLooping || module.Id != statusEffectEvent.Id || statusEffect.Stacks < statusEffectEvent.PreviousStacks)
                                    continue;

                                localToWorldLookup.TryGetComponent(entity, out var localToWorld);
                                var vfxObject = UnityEngine.Object.Instantiate(module.Value.Prefab, localToWorld.Position, localToWorld.Rotation) as GameObject;

                                cleanupBuffer.Add(new VfxModuleCleanup
                                {
                                    Id = module.Id,
                                    Value = vfxObject,
                                });
                            }
                            break;
                    }
                }

                if (foundBuffer && buffer.Length <= 0)
                    commandBuffer.RemoveComponent<Modules<VfxModuleStruct>>(entity);
            }

            foreach (var (cleanupBuffer, entity) in SystemAPI.Query<DynamicBuffer<VfxModuleCleanup>>().WithEntityAccess())
            {
                for (int i = cleanupBuffer.Length - 1; i >= 0; i--)
                {
                    var cleanup = cleanupBuffer[i];

                    if (!cleanup.Value.IsValid())
                    {
                        cleanupBuffer.RemoveAtSwapBack(i);
                        continue;
                    }

                    // If the entity is being destoyed we need to cleanup the VFX.
                    if (!statusEffectsLookup.HasBuffer(entity))
                    {
                        cleanupBuffer.RemoveAtSwapBack(i);
                        cleanup.Value.Value.GetComponent<ParticleSystem>()?.Stop();
                        continue;
                    }

                    localToWorldLookup.TryGetComponent(entity, out var localToWorld);
                    cleanup.Value.Value.transform.SetPositionAndRotation(localToWorld.Position, localToWorld.Rotation);
                }

                if (cleanupBuffer.Length <= 0)
                    commandBuffer.RemoveComponent<VfxModuleCleanup>(entity);
            }
        }
    }
}