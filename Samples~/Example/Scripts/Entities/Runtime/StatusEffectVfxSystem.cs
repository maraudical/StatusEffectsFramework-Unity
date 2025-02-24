#if ENTITIES
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using static StatusEffects.Modules.VfxModule;

namespace StatusEffects.Entities.Example
{
    [WorldSystemFilter(WorldSystemFilterFlags.LocalSimulation | WorldSystemFilterFlags.ClientSimulation)]
    // Order last because we sync position after all other simulations.
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class StatusEffectVfxSystem : SystemBase
    {
        private Dictionary<Entity, Transform> m_EntityParticles;
        private Transform m_Transform;

        private EntityQuery m_EntityQuery;

        protected override void OnCreate()
        {
            m_EntityParticles = new();
            
            var builder = new EntityQueryBuilder(Allocator.Temp).WithAny<VfxEntityModule, VfxCleanupTag>();
            m_EntityQuery = GetEntityQuery(builder);
            RequireForUpdate(m_EntityQuery);
        }

        protected override void OnUpdate()
        {
            CompleteDependency();
            var localToWorldLookup = SystemAPI.GetComponentLookup<LocalToWorld>();

            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);

            foreach ((VfxEntityModule vfx, Module module, Entity entity) in SystemAPI.Query<VfxEntityModule, Module>().WithEntityAccess())
            {
                // Check if it doesn't exists in the Dictionary (we are adding).
                if (!m_EntityParticles.TryGetValue(entity, out m_Transform))
                {
#if NETCODE_ENTITIES
                    // Special case where instantiate again vfx should no get
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

                    commandBuffer.AddComponent(entity, new VfxCleanupTag() 
                    { 
                        InstantiateAgainWhenAddingStacks = vfx.InstantiateAgainWhenAddingStacks 
                    });
                }
                // Otherwise check if updating.
                else if (module.IsBeingUpdated && vfx.InstantiateAgainWhenAddingStacks)
                {
#if NETCODE_ENTITIES
                    // Specific to client side stack counts. Replicated values
                    // are more accurate in realtime.
                    if (module.IsReplicated)
                    {
                        if (module.ReplicatedPreviousStacks >= module.ReplicatedStacks)
                            continue;
                    }
                    else
                    {
                        if (module.PreviousStacks >= module.Stacks)
                            continue;
                    }
#else
                    if (module.PreviousStacks >= module.Stacks)
                        continue;
#endif
                    GameObject VfxObject = Object.Instantiate(vfx.Prefab) as GameObject;

                    m_Transform = VfxObject.transform;
                    m_EntityParticles[entity] = VfxObject.transform;
                }
                // Set position to current entity position.
                if (m_Transform && localToWorldLookup.TryGetComponent(module.Parent, out var localToWorld))
                    m_Transform.position = localToWorld.Position;
            }
            // Cleanup tags are used here because if the Module is destroyed by the server,
            // there is a high chance that the client will not enjoy the frame delay to
            // anticipate module destruction like the server handles. If not using NetCode,
            // you can just use the Module.IsBeingDestroyed or query the ModuleDestroyTag
            foreach ((VfxCleanupTag vfx, Entity entity) in SystemAPI.Query<VfxCleanupTag>().WithNone<Module>().WithEntityAccess())
            {
                if (!m_EntityParticles.TryGetValue(entity, out m_Transform))
                    continue;
                // Attempt to stop the particle system.
                if (m_Transform && !vfx.InstantiateAgainWhenAddingStacks)
                    m_Transform.GetComponent<ParticleSystem>().Stop();
                
                m_EntityParticles.Remove(entity);

                commandBuffer.RemoveComponent<VfxCleanupTag>(entity);
            }

            commandBuffer.Playback(EntityManager);
            commandBuffer.Dispose();
        }
    }
}
#endif