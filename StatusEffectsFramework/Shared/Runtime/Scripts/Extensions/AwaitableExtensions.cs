#if UNITY_2023_1_OR_NEWER
using System;
using System.Threading;
using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// Helpers for waiting on conditions with <see cref="Awaitable"/>.
    /// </summary>
    public static class AwaitableExtensions
    {
        /// <summary>
        /// Waits, checking every frame, until <paramref name="condition"/> returns true or the token is canceled.
        /// </summary>
        public static async Awaitable WaitUntil(Func<bool> condition, CancellationToken cancellationToken)
        {
            while (!condition() && !cancellationToken.IsCancellationRequested)
                await Awaitable.NextFrameAsync();
        }

        /// <summary>
        /// Waits until the token is canceled.
        /// </summary>
        public static async Awaitable WaitUntilCanceled(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
                await Awaitable.NextFrameAsync();
        }
    }
}
#endif