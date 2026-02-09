#if ENTITIES && NETCODE
using Unity.Burst;
using Unity.Mathematics;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    [BurstCompile]
    public static class NetworkTimeExtensions
    {
        [BurstCompile]
        public static float PredictedTimeSinceTick(this NetworkTime networkTime, in NetworkTick tick, in ClientServerTickRate tickRate)
        {
            var ticksElapsed = networkTime.ServerTick.TicksSince(tick);
            float timeElapsed = (ticksElapsed + networkTime.ServerTickFraction) / tickRate.SimulationTickRate;
            return math.max(0f, timeElapsed);
        }

        [BurstCompile]
        public static float InterpolatedTimeSinceTick(this NetworkTime networkTime, in NetworkTick tick, in ClientServerTickRate tickRate)
        {
            var ticksElapsed = networkTime.InterpolationTick.TicksSince(tick);
            float timeElapsed = (ticksElapsed + networkTime.InterpolationTickFraction) / tickRate.SimulationTickRate;
            return math.max(0f, timeElapsed);
        }
    }
}
#endif