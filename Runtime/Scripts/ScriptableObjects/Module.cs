#if UNITASK
using System.Threading;
using Cysharp.Threading.Tasks;
#else
using System.Threading;
using System.Threading.Tasks;
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
        public virtual async UniTaskVoid EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token) { await UniTask.CompletedTask; }
#else
        /// <summary>
        /// This will run as an <see cref="Awaitable"/> when the effect starts. 
        /// Note that you will need to implement the token cancellation in your 
        /// custom effect logic. The token can be cancelled from either the 
        /// destruction of the <see cref="StatusManager"/> or removal of the 
        /// effect, so if you have logic that references the 
        /// <see cref="StatusManager"/> after the cancellation you need to 
        /// check if it is null.
        /// </summary>
        public virtual async Awaitable EnableModule(StatusManager manager, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token) { await Task.CompletedTask; return; }
#endif
    }
}
