#if ENTITIES && NETCODE
using Unity.Burst;
using Unity.Mathematics;
using Unity.NetCode;

namespace StatusEffects.Entities
{
    [BurstCompile]
    public static class NetworkTickExtensions
    {
        [BurstCompile]

        /// <inheritdoc cref="TimeSince(NetworkTick, in NetworkTick, float, in ClientServerTickRate)"/>
        public static float TimeSince(this NetworkTick tick, in NetworkTick oldTick, in ClientServerTickRate tickRate)
        {
            return TimeSince(tick, oldTick, 0f, tickRate);
        }

        /// <summary>
        /// Used to calculate the time in seconds that has elapsed since this tick, 
        /// based on the current tick and the tick rate.
        /// </summary>
        /// <param name="tick">This should most likely be from 
        /// <see cref="NetworkTime.ServerTick"/> or <see cref="NetworkTime.InterpolationTick"/> 
        /// depending on if the value you are comparing is on the server/predicted 
        /// or interpolated
        /// </param>
        /// <returns>The time in seconds that has elapsed since this tick, based on 
        /// the current tick and the tick rate.
        /// </returns>
        public static float TimeSince(this NetworkTick tick, in NetworkTick oldTick, float thisTickFraction, in ClientServerTickRate tickRate)
        {
            var ticksElapsed = tick.TicksSince(oldTick);
            float timeElapsed = (ticksElapsed + thisTickFraction) / tickRate.SimulationTickRate;
            return math.max(0f, timeElapsed);
        }
    }
}
#endif