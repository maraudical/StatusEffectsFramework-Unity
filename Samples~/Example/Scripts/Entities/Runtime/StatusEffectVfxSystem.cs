#if ENTITIES
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.Transforms;
using UnityEngine;
using static StatusEffects.Modules.VfxModule;

namespace StatusEffects.Entities.Example
{
#if NETCODE_ENTITIES
    // AAAAAAAAAAAAAAAAAAAAAA NEEDS TO BE CLIENT
    [WorldSystemFilter(WorldSystemFilterFlags.ServerSimulation)]
#endif
    // Order last because we sync position after all other simulations.
    [UpdateInGroup(typeof(LateSimulationSystemGroup))]
    public partial class StatusEffectVfxSystem : SystemBase
    {
        private Dictionary<Entity, Transform> m_EntityParticles;
        private Transform m_Transform;

        private ComponentLookup<LocalToWorld> m_LocalToWorldLookup;
        private EntityQuery m_EntityQuery;

        protected override void OnCreate()
        {
            m_EntityParticles = new();

            m_LocalToWorldLookup = GetComponentLookup<LocalToWorld>();

            var builder = new EntityQueryBuilder(Allocator.Temp).WithAll<VfxEntityModule, Module>();
            m_EntityQuery = GetEntityQuery(builder);
            RequireForUpdate(m_EntityQuery);
        }

        protected override void OnUpdate()
        {
            CompleteDependency();
            m_LocalToWorldLookup.Update(this);
            
            foreach ((VfxEntityModule vfx, Module module, Entity entity) in SystemAPI.Query<VfxEntityModule, Module>().WithEntityAccess())
            {
                bool exists = m_EntityParticles.TryGetValue(entity, out m_Transform);
                // Check if being removed.
                if (module.IsBeingDestroyed)
                {
                    if (!exists)
                        continue;
                    // Attempt to stop the particle system.
                    if (!vfx.InstantiateAgainWhenAddingStacks)
                        m_Transform.GetComponent<ParticleSystem>().Stop();
                    
                    m_EntityParticles.Remove(entity);
                    continue;
                }
                // Check if it doesn't exists in the Dictionary.
                else if (!exists)
                {
                    GameObject VfxObject = Object.Instantiate(vfx.Prefab) as GameObject;

                    m_Transform = VfxObject.transform;
                    m_EntityParticles.Add(entity, VfxObject.transform);
                }
                // Otherwise check if updating.
                else if (module.IsBeingUpdated && vfx.InstantiateAgainWhenAddingStacks)
                {
                    if (module.PreviousStacks >= module.Stacks)
                        return;

                    GameObject VfxObject = Object.Instantiate(vfx.Prefab) as GameObject;

                    m_Transform = VfxObject.transform;
                    m_EntityParticles[entity] = VfxObject.transform;
                }
                // Set position to current entity position.
                if (m_Transform && m_LocalToWorldLookup.HasComponent(module.Parent))
                    m_Transform.position = m_LocalToWorldLookup[module.Parent].Position;
            }
        }
    }
}
#endif