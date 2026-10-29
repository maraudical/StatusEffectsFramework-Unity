#if UNITASK
using Cysharp.Threading.Tasks;
#endif
using System;
using System.Threading;
using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// Ends its effect once <see cref="StatusEffect.Duration"/> seconds of <see cref="CurrentTime"/> have passed
    /// since <see cref="StartTime"/>. Checked once a frame.
    /// </summary>
    /// <remarks>
    /// Override <see cref="CurrentTime"/> to count with another clock, such as a network clock every machine shares.
    /// </remarks>
    [Serializable]
    public class DurationStatusTimer : StatusTimer
    {
        /// <summary>
        /// The <see cref="CurrentTime"/> the countdown started at.
        /// </summary>
        public double StartTime => m_StartTime;

        [SerializeField] private double m_StartTime;
        private bool m_HasStartTime;
        private CancellationTokenSource m_Source;

        /// <summary>
        /// Starts counting down when the effect starts.
        /// </summary>
        public DurationStatusTimer() { }

        /// <summary>
        /// Counts down from <paramref name="startTime"/>, such as the time an authority added the effect.
        /// </summary>
        public DurationStatusTimer(double startTime)
        {
            m_StartTime = startTime;
            m_HasStartTime = true;
        }

        /// <summary>
        /// The time counted down with. <see cref="Time.timeAsDouble"/> unless overridden.
        /// </summary>
        public virtual double CurrentTime => Time.timeAsDouble;

        /// <inheritdoc/>
        public override StatusEffectTiming Timing => StatusEffectTiming.Duration;

        /// <inheritdoc/>
        public override float TimeRemaining(StatusEffect statusEffect)
        {
            // Not started yet, so none of the duration has passed.
            if (!m_HasStartTime)
                return statusEffect.Duration;

            return Mathf.Max(0f, statusEffect.Duration - (float)(CurrentTime - m_StartTime));
        }

        /// <inheritdoc/>
        public override bool CanCompareTimeRemaining(StatusTimer other) => other?.Timing is StatusEffectTiming.Duration;

        /// <inheritdoc/>
        public override StatusTimer CreateInherited() => new DurationStatusTimer();

        /// <inheritdoc/>
        protected override void OnStart(StatusEffect statusEffect)
        {
            if (!m_HasStartTime)
            {
                m_StartTime = CurrentTime;
                m_HasStartTime = true;
            }

            // A duration of 0 ends now rather than a frame later.
            if (TimeRemaining(statusEffect) <= 0)
            {
                End();
                return;
            }

            m_Source = new CancellationTokenSource();
#if UNITASK
            CountDown(statusEffect, m_Source.Token).Forget();
#else
            _ = CountDown(statusEffect, m_Source.Token);
#endif
        }

        /// <inheritdoc/>
        protected override void OnStop(StatusEffect statusEffect)
        {
            // Null when the effect ended before the countdown started.
            if (m_Source == null)
                return;

            m_Source.Cancel();
            m_Source.Dispose();
            m_Source = null;
        }

#if UNITASK
        private async UniTaskVoid CountDown(StatusEffect statusEffect, CancellationToken token)
#else
        private async Awaitable CountDown(StatusEffect statusEffect, CancellationToken token)
#endif
        {
            try
            {
                while (TimeRemaining(statusEffect) > 0)
#if UNITASK
                    await UniTask.NextFrame(token);
#else
                    await Awaitable.NextFrameAsync(token);
#endif
            }
            catch (OperationCanceledException)
            {
                // Stopped before the time ran out.
                return;
            }

            End();
        }
    }
}
