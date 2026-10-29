using Mirror;
using UnityEngine;

namespace StatusEffectsFramework.Mirror
{
    /// <summary>
    /// The timer of a status effect a client mirrors from the server. It counts on <see cref="NetworkTime.time"/>,
    /// the clock the server's start time is measured on, so the time left matches the server's even when
    /// <see cref="Time.timeScale"/> or frame hitches differ. It never ends its effect; the server removes it.
    /// </summary>
    internal sealed class NetworkMirroredStatusTimer : StatusTimer
    {
        private readonly StatusEffectTiming m_Timing;
        private readonly double m_ServerTimeAdded;

        /// <param name="timing">The server's timing. Infinite effects are mirrored without a timer.</param>
        /// <param name="serverTimeAdded">When the server's countdown started, on <see cref="NetworkTime.time"/>.</param>
        public NetworkMirroredStatusTimer(StatusEffectTiming timing, double serverTimeAdded)
        {
            m_Timing = timing;
            m_ServerTimeAdded = serverTimeAdded;
        }

        /// <inheritdoc/>
        public override StatusEffectTiming Timing => m_Timing;

        /// <inheritdoc/>
        public override float TimeRemaining(StatusEffect statusEffect)
        {
            // Event and predicate effects have no time to count; the server syncs their duration.
            if (m_Timing is not StatusEffectTiming.Duration)
                return statusEffect.Duration;

            return Mathf.Max(0f, statusEffect.Duration - (float)(NetworkTime.time - m_ServerTimeAdded));
        }

        /// <inheritdoc/>
        public override bool CanCompareTimeRemaining(StatusTimer other)
        {
            return m_Timing is StatusEffectTiming.Duration && other?.Timing is StatusEffectTiming.Duration;
        }

        /// <inheritdoc/>
        public override StatusTimer CreateInherited() => new NetworkMirroredStatusTimer(m_Timing, m_ServerTimeAdded);

        /// <inheritdoc/>
        protected override void OnStart(StatusEffect statusEffect) { }

        /// <inheritdoc/>
        protected override void OnStop(StatusEffect statusEffect) { }
    }
}
