using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.NetCode;

namespace StatusEffects.Entities
{
#if NETCODE
    [UpdateInGroup(typeof(PredictedStatusEffectSystemGroup))]
#else
    [UpdateInGroup(typeof(StatusEffectSystemGroup))]
#endif
    [BurstCompile]
    public partial struct StatusEffectPredictedEventsSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffectEvents, Simulate>().Build();
            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statusEffectPredictedEventsJob = new StatusEffectPredictedEventsJob
            {
                NetworkTime = SystemAPI.GetSingleton<NetworkTime>(),
                TickRate = SystemAPI.GetSingleton<ClientServerTickRate>(),
            };
            state.Dependency = statusEffectPredictedEventsJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct StatusEffectPredictedEventsJob : IJobEntity
        {
            public NetworkTime NetworkTime;
            public ClientServerTickRate TickRate;

            public void Execute(ref DynamicBuffer<StatusEffectPredictedEvents> predictedEvents)
            {
                for (int i = predictedEvents.Length - 1; i >= 0; i--)
                {
                    var predictedEvent = predictedEvents.ElementAt(i);
                    if (NetworkTime.ServerTick.IsNewerThan(predictedEvent.NetworkTick))
                    {
                        predictedEvents.RemoveAt(i);
                    }
                }
                TickRate.
                
            }
        }
    }
}
