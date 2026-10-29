using System;
using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// Takes each <see cref="StatusEvent.Invoke"/> value off <see cref="StatusEffect.Duration"/>,
    /// and ends its effect when nothing is left.
    /// </summary>
    [Serializable]
    public sealed class EventStatusTimer : StatusTimer
    {
        /// <summary>
        /// The event the effect counts down on.
        /// </summary>
        public StatusEvent StatusEvent => m_StatusEvent;

        [SerializeField] private StatusEvent m_StatusEvent;

        private StatusEffect m_StatusEffect;

        public EventStatusTimer(StatusEvent statusEvent)
        {
            if (statusEvent == null)
                throw new ArgumentNullException(nameof(statusEvent));

            m_StatusEvent = statusEvent;
        }

        /// <inheritdoc/>
        public override StatusEffectTiming Timing => StatusEffectTiming.Event;

        /// <inheritdoc/>
        public override float TimeRemaining(StatusEffect statusEffect) => statusEffect.Duration;

        /// <inheritdoc/>
        public override bool CanCompareTimeRemaining(StatusTimer other)
        {
            // Effects counting down on different events can't be compared.
            return other is EventStatusTimer eventTimer && ReferenceEquals(eventTimer.m_StatusEvent, m_StatusEvent);
        }

        /// <inheritdoc/>
        public override StatusTimer CreateInherited() => new EventStatusTimer(m_StatusEvent);

        /// <inheritdoc/>
        protected override void OnStart(StatusEffect statusEffect)
        {
            // Nothing left to count down.
            if (statusEffect.Duration <= 0)
            {
                End();
                return;
            }

            m_StatusEffect = statusEffect;
            m_StatusEvent.Invoked += Decrement;
        }

        /// <inheritdoc/>
        protected override void OnStop(StatusEffect statusEffect)
        {
            m_StatusEvent.Invoked -= Decrement;
            m_StatusEffect = null;
        }

        private void Decrement(float value)
        {
            m_StatusEffect.Duration -= value;

            if (m_StatusEffect.Duration <= 0)
                End();
        }
    }
}
