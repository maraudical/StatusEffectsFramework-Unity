using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;
using Unity.Transforms;
using UnityEngine;

[assembly: RegisterGenericComponentType(typeof(StatusEffectsFramework.Entities.Modules<StatusEffectsFramework.Entities.Samples.VfxModuleStruct>))]

namespace StatusEffectsFramework.Entities.Samples
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

        protected override void OnCreate()
        {
            m_CleanupQuery = SystemAPI.QueryBuilder().WithAll<VfxModuleCleanup>().Build();
            m_EventQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, StatusEffectEvents>().Build();

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
            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var cleanupLookup = SystemAPI.GetBufferLookup<VfxModuleCleanup>();

            StatusEffects statusEffect;
            
            foreach (var (modules, statusEffectEvents, entity) in SystemAPI.Query<DynamicBuffer<Modules<VfxModuleStruct>>, DynamicBuffer<StatusEffectEvents>>().WithEntityAccess())
            {
                // AsNativeArray does not create a copy of the data so any changes will effect the source buffer.
                var modulesArray = modules.AsNativeArray();
                modulesArray.Sort();

                var statusEffects = statusEffectsLookup[entity];
                bool foundCleanupBuffer = cleanupLookup.TryGetBuffer(entity, out var cleanupBuffer);
                int index;

                foreach (var statusEffectEvent in statusEffectEvents)
                {
                    switch (statusEffectEvent.Event)
                    {
                        case StatusEffectEvent.Added:
                            index = modulesArray.BinarySearchFirst(statusEffectEvent.Id);

                            if (index < 0 || !StatusEffects.TryGetStatusEffect(statusEffects, statusEffectEvent.Id, out statusEffect))
                                return;

                            for (int i = index; i < modulesArray.Length; i++)
                            {
                                var module = modulesArray[i];

                                if (module.Id != statusEffectEvent.Id)
                                    break;

#if NETCODE
                                // Special case where we don't want old events to instantiate VFX.
                                if (!module.Struct.IsLooping && statusEffectEvent.IsOld)
                                    continue;

#endif
                                var localToWorld = localToWorldLookup[entity];
                                var vfxObject = Object.Instantiate(module.Struct.Prefab, localToWorld.Position, localToWorld.Rotation) as GameObject;

                                if (!foundCleanupBuffer)
                                {
                                    foundCleanupBuffer = true;
                                    cleanupBuffer = commandBuffer.AddBuffer<VfxModuleCleanup>(entity);
                                }

                                cleanupBuffer.Add(new VfxModuleCleanup
                                {
                                    Id = module.Id,
                                    Value = vfxObject,
                                });
                            }
                            break;
                        case StatusEffectEvent.Removed:
                            if (!foundCleanupBuffer)
                                break;
                            
                            for (int i = cleanupBuffer.Length - 1; i >= 0; i--)
                            {
                                var cleanup = cleanupBuffer[i];

                                if (cleanup.Id != statusEffectEvent.Id)
                                    continue;

                                cleanupBuffer.RemoveAtSwapBack(i);

                                if (cleanup.Value.IsValid())
                                    cleanup.Value.Value.GetComponent<ParticleSystem>()?.Stop();
                            }

                            break;
                        case StatusEffectEvent.Updated:
#if NETCODE
                            // Special case where we don't want old events to instantiate VFX.
                            if (statusEffectEvent.IsOld)
                                continue;

#endif
                            index = modulesArray.BinarySearchFirst(statusEffectEvent.Id);

                            if (index < 0 || !StatusEffects.TryGetStatusEffect(statusEffects, statusEffectEvent.Id, out statusEffect))
                                return;

                            for (int i = index; i < modulesArray.Length; i++)
                            {
                                var module = modulesArray[i];

                                if (module.Id != statusEffectEvent.Id)
                                    break;

                                if (module.Struct.IsLooping || statusEffect.Stacks < statusEffectEvent.PreviousStacks)
                                    continue;

                                var localToWorld = localToWorldLookup[entity];
                                var vfxObject = UnityEngine.Object.Instantiate(module.Struct.Prefab, localToWorld.Position, localToWorld.Rotation) as GameObject;

                                if (!foundCleanupBuffer)
                                {
                                    foundCleanupBuffer = true;
                                    cleanupBuffer = commandBuffer.AddBuffer<VfxModuleCleanup>(entity);
                                }

                                cleanupBuffer.Add(new VfxModuleCleanup
                                {
                                    Id = module.Id,
                                    Value = vfxObject,
                                });
                            }
                            break;
                    }
                }
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