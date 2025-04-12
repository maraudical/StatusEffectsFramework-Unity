#if ENTITIES && NETCODE
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    public struct StatusReferencesGhostsSetup : IComponentData { }

    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation | WorldSystemFilterFlags.ServerSimulation)]
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(StatusReferencesSetupSystem))]
    [UpdateAfter(typeof(BeginStatusEffectEntityCommandBufferSystem))]
    public partial class StatusReferencesGhostsSetupSystem : SystemBase
    {
        public List<Hash128> m_ConvertedGhostPrefabIds;
        public EntityQuery m_RequestQuery;

        protected override void OnCreate()
        {
            m_ConvertedGhostPrefabIds = new();

            m_RequestQuery = SystemAPI.QueryBuilder().WithAll<ModulePrefabs>().WithNone<StatusReferencesGhostsSetup>().Build();
            RequireForUpdate(m_RequestQuery);
        }

        protected override void OnUpdate() 
        {
            var prefabs = m_RequestQuery.GetSingletonBuffer<ModulePrefabs>().Reinterpret<Entity>().ToNativeArray(Allocator.Temp);
            var blobAssets = SystemAPI.GetSingleton<StatusReferences>().BlobAsset.Value.GetValueArray(Allocator.Temp);

            foreach (var blobAsset in blobAssets)
            {
                ref var data = ref blobAsset.Value;

                if (data.ModulePrefabIndex < 0 || m_ConvertedGhostPrefabIds.Contains(data.Id))
                    continue;
                
                GhostPrefabCreation.ConvertToGhostPrefab(EntityManager, prefabs[data.ModulePrefabIndex], new GhostPrefabCreation.Config()
                {
                    Name = data.Id.ToString(),
                    DefaultGhostMode = GhostMode.Interpolated,
                    SupportedGhostModes = GhostModeMask.Interpolated,
                    OptimizationMode = GhostOptimizationMode.Static
                });

                m_ConvertedGhostPrefabIds.Add(data.Id);
            }

            blobAssets.Dispose();

            EntityManager.AddComponentData<StatusReferencesGhostsSetup>(m_RequestQuery.GetSingletonEntity(), new());
        }
    }
}
#endif