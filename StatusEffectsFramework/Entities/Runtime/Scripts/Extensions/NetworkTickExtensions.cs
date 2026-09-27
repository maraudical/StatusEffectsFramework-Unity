#if ENTITIES && NETCODE
using Unity.Burst;
using Unity.Mathematics;
using Unity.NetCode;

namespace StatusEffectsFramework.Entities
{
    [BurstCompile]
    public static class NetworkTickExtensions
    {
        /// <inheritdoc cref="TimeSince(NetworkTick, in NetworkTick, float, in ClientServerTickRate)"/>
        /// <remarks>Treats <paramref name="tick"/> as a full tick.</remarks>
        [BurstCompile]
        public static float TimeSince(this NetworkTick tick, in NetworkTick oldTick, in ClientServerTickRate tickRate)
        {
            return TimeSince(tick, oldTick, 1f, tickRate);
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
        /// <param name="thisTickFraction">How much of <paramref name="tick"/> has been simulated,
        /// such as <see cref="NetworkTime.ServerTickFraction"/>. This is 1 on full ticks.
        /// </param>
        /// <returns>The time in seconds that has elapsed since this tick, based on
        /// the current tick and the tick rate.
        /// </returns>
        [BurstCompile]
        public static float TimeSince(this NetworkTick tick, in NetworkTick oldTick, float thisTickFraction, in ClientServerTickRate tickRate)
        {
            // A partial tick is still being simulated towards tick, so only (fraction - 1) of it
            // has passed since the previous full tick. On full ticks the fraction is 1 and this
            // is just the whole ticks elapsed.
            var ticksElapsed = tick.TicksSince(oldTick);
            float timeElapsed = (ticksElapsed - 1f + thisTickFraction) / tickRate.SimulationTickRate;
            return math.max(0f, timeElapsed);
        }
    }
}
#endif