#if ENTITIES && NETCODE
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;
using Unity.NetCode;
using UnityEngine.UIElements;
using static UnityEngine.Rendering.DebugUI;

namespace StatusEffects.Entities
{
    [WorldSystemFilter(WorldSystemFilterFlags.ClientSimulation | WorldSystemFilterFlags.ThinClientSimulation)]
    [UpdateInGroup(typeof(StatusEffectSystemGroup))]
    [UpdateAfter(typeof(StatusManager))]
    [BurstCompile]
    public partial struct InterpolatedStatusEffectEventsSystem : ISystem
    {
        private EntityQuery m_EntityQuery;

        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            m_EntityQuery = SystemAPI.QueryBuilder().WithAll<StatusEffects, InterpolatedStatusEffects>().WithNone<PredictedGhost>().Build();
            m_EntityQuery.SetChangedVersionFilter(ComponentType.ReadOnly<StatusEffects>());

            state.RequireForUpdate(m_EntityQuery);
            state.RequireForUpdate<NetworkTime>();
        }

        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            var statusEffectsInterpolatedEventsJob = new StatusEffectsInterpolatedEventsJob
            {
                CommandBuffer = SystemAPI.GetSingleton<EndStatusEffectEntityCommandBufferSystem.Singleton>().CreateCommandBuffer(state.WorldUnmanaged).AsParallelWriter()
            };
            state.Dependency = statusEffectsInterpolatedEventsJob.ScheduleParallelByRef(m_EntityQuery, state.Dependency);
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        partial struct StatusEffectsInterpolatedEventsJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;

            public void Execute(in DynamicBuffer<StatusEffects> statusEffects, ref DynamicBuffer<InterpolatedStatusEffects> interpolatedStatusEffects)
            {
                unsafe
                {
                    int length = statusEffects.Length;
                    int interpolatedLength = interpolatedStatusEffects.Length;
                    // We copy to a new array here so that we don't sort the underlying
                    // buffer and cause another changed version to trigger when the status
                    // effects are synced back to the server.
                    using var statusEffectsArray = statusEffects.ToNativeArray(Allocator.Temp);
                    using var interpolatedStatusEffectsArray = interpolatedStatusEffects.ToNativeArray(Allocator.Temp);

                    statusEffectsArray.Sort();
                    interpolatedStatusEffectsArray.Sort();

                    for (int i = 0; i != statusEffects.Length; i++)
                    {
                        if (UnsafeUtility.ReadArrayElement<T>(ptr, i).Equals(value))
                            return i;
                    }
                }
            }
        }
    }
}
#endif