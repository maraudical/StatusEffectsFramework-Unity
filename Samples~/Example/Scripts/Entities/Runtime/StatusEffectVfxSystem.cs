#if ENTITIES
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using static StatusEffects.Modules.VfxModule;

namespace StatusEffects.Entities.Example
{
    public struct VfxCleanupComponent : ICleanupComponentData { }

    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    // Order last because we sync position after all other simulations.
    [UpdateBefore(typeof(TransformSystemGroup))]
    public partial class StatusEffectVfxSystem : SystemBase
    {
        private Dictionary<Entity, Transform> m_EntityParticles;
        private Transform m_Transform;
        
        private EntityQuery m_VfxEntityModuleChangedQuery;
        private EntityQuery m_VfxCleanupTagQuery;

        protected override void OnCreate()
        {
            m_EntityParticles = new();
            
            m_VfxEntityModuleChangedQuery = SystemAPI.QueryBuilder().WithAll<VfxEntityModule, Modules>().Build();
            m_VfxEntityModuleChangedQuery.AddChangedVersionFilter(ComponentType.ReadOnly<Modules>());
            m_VfxCleanupTagQuery = SystemAPI.QueryBuilder().WithAll<VfxCleanupComponent, ModuleCleanupComponent>().WithNone<Modules>().Build();
        }

        protected override void OnUpdate()
        {
            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>(true);

            var commandBuffer = SystemAPI.GetSingletonRW<BeginSimulationEntityCommandBufferSystem.Singleton>().ValueRW.CreateCommandBuffer(World.Unmanaged);
            
            foreach ((VfxEntityModule vfx, Module module, Entity entity) in SystemAPI.Query<VfxEntityModule, Modules>().WithEntityAccess())
            {
                // Check if it doesn't exists in the Dictionary (we are adding).
                if (!m_EntityParticles.TryGetValue(entity, out m_Transform))
                {
#if NETCODE_ENTITIES
                    // Special case where instantiate again vfx should not get
                    // respawned after a client DCs and reconnects.
                    if (vfx.InstantiateAgainWhenAddingStacks && module.PreviousStacks != 0)
                    {
                        m_EntityParticles.Add(entity, null);
                        continue;
                    }
#endif
                    GameObject VfxObject = Object.Instantiate(vfx.Prefab) as GameObject;

                    m_Transform = VfxObject.transform;
                    m_EntityParticles.Add(entity, VfxObject.transform);

                    commandBuffer.AddComponent<VfxCleanupComponent>(entity);
                }
                else if (m_VfxEntityModuleChangedQuery.Matches(entity) && vfx.InstantiateAgainWhenAddingStacks)
                {
                    if (module.PreviousStacks >= module.Stacks)
                        continue;

                    GameObject VfxObject = Object.Instantiate(vfx.Prefab) as GameObject;

                    m_Transform = VfxObject.transform;
                    m_EntityParticles[entity] = VfxObject.transform;
                }
                // Set position to current entity position.
                if (m_Transform && localToWorldLookup.TryGetComponent(module.Target, out var localToWorld))
                    m_Transform.position = localToWorld.Position;
            }

            if (m_VfxCleanupTagQuery.IsEmpty)
                return;

            using var cleanupEntities = m_VfxCleanupTagQuery.ToEntityArray(Allocator.Temp);
            foreach (var entity in cleanupEntities)
            {
                if (!m_EntityParticles.TryGetValue(entity, out m_Transform))
                    continue;
                // Attempt to stop the particle system.
                if (m_Transform)
                    m_Transform.GetComponent<ParticleSystem>()?.Stop();
                
                m_EntityParticles.Remove(entity);
            }
        }
    }
}
#endif