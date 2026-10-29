using PurrNet.Modules;
using UnityEngine;

namespace StatusEffectsFramework.PurrNet
{
    /// <summary>
    /// The timer of a status effect a client mirrors from the server. It counts on the <see cref="TickManager"/>'s
    /// synced tick, the clock the server's start time is measured on, so the time left matches the server's even when
    /// <see cref="Time.timeScale"/> or frame hitches differ. It never ends its effect; the server removes it.
    /// </summary>
    internal sealed class NetworkMirroredStatusTimer : StatusTimer
    {
        private readonly StatusEffectTiming m_Timing;
        private readonly double m_ServerTimeAdded;
        private readonly TickManager m_TickManager;

        /// <param name="timing">The server's timing. Infinite effects are mirrored without a timer.</param>
        /// <param name="serverTimeAdded">When the server's countdown started, in seconds of synced ticks.</param>
        /// <param name="tickManager">The tick module of the network manager the effect is synced through.</param>
        public NetworkMirroredStatusTimer(StatusEffectTiming timing, double serverTimeAdded, TickManager tickManager)
        {
            m_Timing = timing;
            m_ServerTimeAdded = serverTimeAdded;
            m_TickManager = tickManager;
        }

        /// <inheritdoc/>
        public override StatusEffectTiming Timing => m_Timing;

        /// <inheritdoc/>
        public override float TimeRemaining(StatusEffect statusEffect)
        {
            // Event and predicate effects have no time to count; the server syncs their duration.
            if (m_Timing is not StatusEffectTiming.Duration)
                return statusEffect.Duration;

            // The synced tick follows the server's on clients and is the server's own tick on the server.
            double currentTime = m_TickManager.syncedPreciseTick * m_TickManager.tickDeltaDouble;
            return Mathf.Max(0f, statusEffect.Duration - (float)(currentTime - m_ServerTimeAdded));
        }

        /// <inheritdoc/>
        public override bool CanCompareTimeRemaining(StatusTimer other)
        {
            return m_Timing is StatusEffectTiming.Duration && other?.Timing is StatusEffectTiming.Duration;
        }

        /// <inheritdoc/>
        public override StatusTimer CreateInherited() => new NetworkMirroredStatusTimer(m_Timing, m_ServerTimeAdded, m_TickManager);

        /// <inheritdoc/>
        protected override void OnStart(StatusEffect statusEffect) { }

        /// <inheritdoc/>
        protected override void OnStop(StatusEffect statusEffect) { }
    }
}
