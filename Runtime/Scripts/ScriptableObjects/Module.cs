#if UNITASK
using System.Threading;
using Cysharp.Threading.Tasks;
#elif UNITY_2023_1_OR_NEWER
using System.Threading;
#else
using System.Collections;
#endif
using UnityEngine;

namespace StatusEffects.Modules
{
    public abstract class Module : ScriptableObject
    {
#if UNITASK
        /// <summary>
        /// This will run as an <see cref="UniTask"/> when the effect starts. 
        /// Note that you will need to implement the token cancellation in your 
        /// custom effect logic. The token can be cancelled from either the 
        /// destruction of the <see cref="StatusManager"/> or removal of the 
        /// effect, so if you have logic that references the 
        /// <see cref="StatusManager"/> after the cancellation you need to 
        /// check if it is null.
        /// </summary>
        public abstract UniTask EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token);
#elif UNITY_2023_1_OR_NEWER
        /// <summary>
        /// This will run as an <see cref="Awaitable"/> when the effect starts. 
        /// Note that you will need to implement the token cancellation in your 
        /// custom effect logic. The token can be cancelled from either the 
        /// destruction of the <see cref="StatusManager"/> or removal of the 
        /// effect, so if you have logic that references the 
        /// <see cref="StatusManager"/> after the cancellation you need to 
        /// check if it is null.
        /// </summary>
        public abstract Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token);
#else
        /// <summary>
        /// This will run as an <see cref="IEnumerator"/> coroutine when the effect starts.
        /// </summary>
        public abstract IEnumerator EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance);
        /// <summary>
        /// Use this callback to do something when the effect ends.
        /// </summary>
        public abstract void DisableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance);
#endif
    }
}
