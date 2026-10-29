#if UNITASK
using Cysharp.Threading.Tasks;
#else
using UnityEngine;
#endif
using System;
using System.Threading;

namespace StatusEffectsFramework
{
    /// <summary>
    /// Ends its effect when <see cref="Predicate"/> returns true, checked once a frame.
    /// </summary>
    [Serializable]
    public sealed class PredicateStatusTimer : StatusTimer
    {
        /// <summary>
        /// Ends the effect when it returns true.
        /// </summary>
        public Func<bool> Predicate { get; }

        private CancellationTokenSource m_Source;

        public PredicateStatusTimer(Func<bool> predicate)
        {
            Predicate = predicate ?? throw new ArgumentNullException(nameof(predicate));
        }

        /// <inheritdoc/>
        public override StatusEffectTiming Timing => StatusEffectTiming.Predicate;

        /// <inheritdoc/>
        public override float TimeRemaining(StatusEffect statusEffect) => statusEffect.Duration;

        /// <inheritdoc/>
        public override StatusTimer CreateInherited() => new PredicateStatusTimer(Predicate);

        /// <inheritdoc/>
        protected override void OnStart(StatusEffect statusEffect)
        {
            m_Source = new CancellationTokenSource();
#if UNITASK
            WaitForPredicate(m_Source.Token).Forget();
#else
            _ = WaitForPredicate(m_Source.Token);
#endif
        }

        /// <inheritdoc/>
        protected override void OnStop(StatusEffect statusEffect)
        {
            m_Source.Cancel();
            m_Source.Dispose();
            m_Source = null;
        }

#if UNITASK
        private async UniTaskVoid WaitForPredicate(CancellationToken token)
#else
        private async Awaitable WaitForPredicate(CancellationToken token)
#endif
        {
            try
            {
#if UNITASK
                await UniTask.WaitUntil(Predicate, cancellationToken: token);
#else
                // Returns instead of throwing when canceled; End() does nothing once the timer has stopped.
                await AwaitableExtensions.WaitUntil(Predicate, token);
#endif
            }
            catch (OperationCanceledException)
            {
                // Stopped before the predicate was true.
                return;
            }

            End();
        }
    }
}
