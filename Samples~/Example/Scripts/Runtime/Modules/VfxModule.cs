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
#if ENTITIES
using StatusEffects.Entities;
using Unity.Entities;
using Unity.Collections;
using Unity.Transforms;
using System;

[assembly: RegisterGenericComponentType(typeof(Modules<StatusEffects.Example.VfxModuleStruct>))]
#endif

namespace StatusEffects.Example
{
    [CreateAssetMenu(fileName = "Vfx Module", menuName = "Status Effect Framework/Modules/Vfx", order = 1)]
    [AttachModuleInstance(typeof(VfxInstance))]
    public class VfxModule : Module
#if ENTITIES
        , IEntityModule
    {
        public ModuleInfo CreateModuleInfo(ModuleInstance moduleInstance)
        {
            var instance = moduleInstance as VfxInstance;

            bool isLooping = false;
            if (instance && instance.Prefab && instance.Prefab.TryGetComponent(out ParticleSystem particleSystem))
                isLooping = particleSystem.main.loop;

            var moduleStruct = new VfxModuleStruct
            {
                Prefab = instance.Prefab,
                IsLooping = isLooping,
            };
            return (this as IEntityModule).AllocateModule(moduleStruct);
        }
#else
    {
#endif

#if UNITASK
        public override async UniTaskVoid EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;
            // Make sure the particle system stop action is set to destroy so it
            // automatically destroys itself when all particles die.
            GameObject vfxGameObject = Instantiate(vfxInstance.Prefab, manager.transform);
            ParticleSystem particleSystem = vfxGameObject.GetComponent<ParticleSystem>();
            // If we want this effect to be added everytime more stacks are
            // added we just immediately begin destruction on the current particle.
            if (particleSystem && particleSystem.main.loop)
                await UniTask.WaitUntilCanceled(token);
            else
                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(vfxInstance.Prefab, manager, statusEffect, previous, stack);

            // Attempt to stop the particle system.
            particleSystem?.Stop();
        }
#elif UNITY_2023_1_OR_NEWER
        public override async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token)
        {
        VfxInstance vfxInstance = moduleInstance as VfxInstance;
            // Make sure the particle system stop action is set to destroy so it
            // automatically destroys itself when all particles die.
            GameObject vfxGameObject = Instantiate(vfxInstance.Prefab, manager.transform);
            ParticleSystem particleSystem = vfxGameObject.GetComponent<ParticleSystem>();
            // If we want this effect to be added everytime more stacks are
            // added we just immediately begin destruction on the current particle.
            if (particleSystem && particleSystem.main.loop)
                await AwaitableExtensions.WaitUntilCanceled(token);
            else
                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(vfxInstance.Prefab, manager, statusEffect, previous, stack);

            // Attempt to stop the particle system.
            particleSystem?.Stop();
        }
#else
        public override IEnumerator EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance)
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;
            // Make sure the particle system stop action is set to destroy so it
            // automatically destroys itself when all particles die.
            // Give the vfx the name of the prefab so it can be queried later.
            GameObject vfxGameObject = Instantiate(vfxInstance.Prefab, manager.transform);
            ParticleSystem particleSystem = vfxGameObject.GetComponent<ParticleSystem>();
            vfxGameObject.name = vfxInstance.Prefab.name;

            if (particleSystem && particleSystem.main.loop)
                statusEffect.OnStackUpdate += (previous, stack) => OnStackUpdate(vfxInstance.Prefab, manager, statusEffect, previous, stack);

            yield break;
        }

        public override void DisableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance) 
        {
            VfxInstance vfxInstance = moduleInstance as VfxInstance;
            // This magic name finding system is horrible but it works. Unitask/Await would 
            // do the enabling and disabling so much better since the reference to the
            // GameObject can be kept within the method.
            Transform vfxTransform = manager.transform.Find(vfxInstance.Prefab.name);
            
            if (!vfxTransform)
                return;
            
            GameObject vfxGameObject = vfxTransform.gameObject;
            // Attempt to stop the particle system.
            vfxGameObject.GetComponent<ParticleSystem>()?.Stop();
            // Unset the parent so that if multiple effects are being removed it doesn't
            // grab the same VFX twice.
            vfxTransform.SetParent(null);
        }
#endif

        private void OnStackUpdate(GameObject prefab, StatusManager manager, StatusEffect statusEffect, int previous, int stack)
        {
            if (previous >= stack)
                return;

            Instantiate(prefab, manager.transform);
        }
    }
#if ENTITIES

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

#if NETCODE_ENTITIES
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation)]
#endif
    [UpdateInGroup(typeof(SimulationSystemGroup))]
    public partial class VfxModuleSystem : SystemBase
    {
        private EntityQuery m_CleanupQuery;
        private EntityQuery m_EventQuery;
        public EntityQuery[] m_Queries;
        
        private TypeIndex m_TypeIndex;
        
        protected override void OnCreate()
        {
            m_CleanupQuery = SystemAPI.QueryBuilder().WithAll<VfxModuleCleanup>().Build();
            m_EventQuery = SystemAPI.QueryBuilder().WithAll<ActiveStatusEffects, StatusEffectEvents>().Build();

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
            var statusEffectsLookup = SystemAPI.GetBufferLookup<ActiveStatusEffects>(true);
            var statusEffectEventsLookup = SystemAPI.GetBufferLookup<StatusEffectEvents>(true);
            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);
            var moduleLookup = SystemAPI.GetBufferLookup<Modules<VfxModuleStruct>>();
            var cleanupLookup = SystemAPI.GetBufferLookup<VfxModuleCleanup>();

            using var entities = m_EventQuery.ToEntityArray(Allocator.Temp);
            ActiveStatusEffects statusEffect;

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

                    if (!StatusEffectsUtility.ModuleInfosContainType(ref data.Modules, m_TypeIndex))
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

                                var module = StatusEffectsUtility.AddModuleToBuffer(ref buffer, moduleInfo, statusEffectEvent.Id);
#if NETCODE_ENTITIES
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
                            if (!foundBuffer)
                                break;
                            
                            StatusEffectsUtility.RemoveModulesFromBuffer(ref buffer, statusEffectEvent.Id);

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
#if NETCODE_ENTITIES
                            // Special case where we don't want old events to instantiate VFX.
                            if (statusEffectEvent.IsOld)
                                continue;

#endif
                            if (!foundCleanupBuffer)
                            {
                                foundCleanupBuffer = true;
                                cleanupBuffer = commandBuffer.AddBuffer<VfxModuleCleanup>(entity);
                            }

                            if (!StatusEffectsUtility.TryGetStatusEffect(statusEffects, statusEffectEvent.Id, out statusEffect))
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
#endif
}
