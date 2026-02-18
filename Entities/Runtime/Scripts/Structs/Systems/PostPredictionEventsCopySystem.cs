#if ENTITIES && NETCODE
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup), OrderFirst = true)]
    [UpdateBefore(typeof(BeginPredictedStatusEffectEntityCommandBufferSystem))]
    [BurstCompile]
    // Since interpolated events are applied before the prediction loop,
    // we need to copy and apply it to a command buffer just after the
    // prediction loop. This makes it so that systems that use the events
    // are separated if they are predicted or interpolated.
    public partial struct PostPredictedSimulationEventsCopySystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents, PredictedGhost>().Build();

            state.RequireForUpdate(m_EntityQuery);
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var postPredictedSimulationEventsCopyJob = new PostPredictedSimulationEventsCopyJob
            {
                NetworkTime = SystemAPI.GetSingleton<NetworkTime>(),
                BeginPredictedStatusEffectEntityCommandBuffer = SystemAPI.GetSingleton<BeginPredictedStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter(),
                PostPredictedSimulationEntityCommandBuffer = SystemAPI.GetSingleton<PostPredictedSimulationEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            };
            state.Dependency = postPredictedSimulationEventsCopyJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct PostPredictedSimulationEventsCopyJob : IJobEntity
        {
            public NetworkTime NetworkTime;
            public EntityCommandBuffer.ParallelWriter BeginPredictedStatusEffectEntityCommandBuffer;
            public EntityCommandBuffer.ParallelWriter PostPredictedSimulationEntityCommandBuffer;

            public unsafe void Execute([ChunkIndexInQuery] int sortKey, Entity entity, in DynamicBuffer<StatusEffectEvents> statusEffectEvents)
            {
                if (!NetworkTime.IsFirstPredictionTick)
                    return;
                
                BeginPredictedStatusEffectEntityCommandBuffer.RemoveComponent<StatusEffectEvents>(sortKey, entity);
                var events = PostPredictedSimulationEntityCommandBuffer.AddBuffer<StatusEffectEvents>(sortKey, entity);
                events.CopyFrom(statusEffectEvents);
            }
        }
    }
}
#endif