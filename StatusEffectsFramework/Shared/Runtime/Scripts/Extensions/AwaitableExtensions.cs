#if UNITY_2023_1_OR_NEWER
using System;
using System.Threading;
using UnityEngine;

namespace StatusEffectsFramework
{
    public static class AwaitableExtensions
    {
        public static async Awaitable WaitUntil(Func<bool> condition, CancellationToken cancellationToken)
        {
            while (!condition() && !cancellationToken.IsCancellationRequested)
                await Awaitable.NextFrameAsync();
        }

        public static async Awaitable WaitUntilCanceled(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
                await Awaitable.NextFrameAsync();
        }
    }
}
#endif