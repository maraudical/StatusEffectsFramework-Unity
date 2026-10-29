using System;

namespace StatusEffectsFramework
{
    /// <summary>
    /// Decides when a <see cref="StatusEffect"/> ends. It starts after the effect's modules are enabled and stops before
    /// they are disabled, whether it ran out, the effect was removed, or its manager was destroyed.
    /// </summary>
    /// <remarks>
    /// Implement <see cref="OnStart"/> and <see cref="OnStop"/>, and call <see cref="End"/> when the timer runs out.
    /// A timer holds the state of one status effect, so create a new one for each effect. Timers only run on the
    /// managed <see cref="StatusManager"/>; Entities uses <see cref="StatusEffectTiming"/> directly.
    /// </remarks>
    [Serializable]
    public abstract class StatusTimer
    {
        /// <summary>
        /// Raised when the timer runs out. The manager removes the effect in response.
        /// Not raised when the effect is removed some other way.
        /// </summary>
        internal event Action Ended;

        private bool m_Running;
        private bool m_HasStarted;

        /// <summary>
        /// Whether the timer has started and hasn't stopped.
        /// </summary>
        public bool IsRunning => m_Running;
        /// <summary>
        /// Whether the timer was ever started. A timer can only be started once.
        /// </summary>
        public bool HasStarted => m_HasStarted;

        /// <summary>
        /// How the timer ends its effect. Decides which effect is kept when a non-stacking effect is added,
        /// and which effects are removed first.
        /// </summary>
        public abstract StatusEffectTiming Timing { get; }

        /// <summary>
        /// The time left in seconds. Return the effect's <see cref="StatusEffect.Duration"/> when there is no time to count.
        /// </summary>
        public abstract float TimeRemaining(StatusEffect statusEffect);

        /// <summary>
        /// Whether this timer's time left can be compared with <paramref name="other"/>'s, such as two timers counting
        /// down in seconds. When it can't, the newest effect is kept.
        /// </summary>
        public virtual bool CanCompareTimeRemaining(StatusTimer other) => false;

        /// <summary>
        /// Creates a new timer that ends the same way, for a condition that adds an effect with <see cref="ConditionalTiming.Inherited"/>.
        /// </summary>
        public abstract StatusTimer CreateInherited();

        /// <summary>
        /// Called when the effect starts. Calling <see cref="End"/> from here removes the effect straight away.
        /// </summary>
        protected abstract void OnStart(StatusEffect statusEffect);

        /// <summary>
        /// Called when the effect stops, for any reason. Undo anything <see cref="OnStart"/> subscribed to.
        /// </summary>
        protected abstract void OnStop(StatusEffect statusEffect);

        /// <summary>
        /// Call when the timer has run out to remove the effect. Does nothing unless the timer is running.
        /// </summary>
        protected void End()
        {
            if (m_Running)
                Ended?.Invoke();
        }

        internal void Start(StatusEffect statusEffect)
        {
            // A shared timer would end whichever effect started it first.
            if (m_HasStarted)
                throw new InvalidOperationException($"This {GetType().Name} was already started for a status effect. Create a new timer for each status effect.");

            m_HasStarted = true;
            m_Running = true;
            OnStart(statusEffect);
        }

        internal void Stop(StatusEffect statusEffect)
        {
            if (!m_Running)
                return;

            // Cleared first so End() does nothing while the timer is stopping.
            m_Running = false;
            Ended = null;
            OnStop(statusEffect);
        }
    }
}
