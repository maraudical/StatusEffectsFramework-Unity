#if UNITASK
using System.Threading;
using Cysharp.Threading.Tasks;
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
        /// destruction of the <see cref="MonoBehaviour"/> or removal of the 
        /// effect, so if you have logic that references the 
        /// <see cref="MonoBehaviour"/> after the cancellation you need to 
        /// check if it is null.
        /// </summary>
        public abstract UniTask EnableModule<T>(T monoBehaviour, StatusEffect statusEffect, ModuleInstance moduleInstance, CancellationToken token) where T : MonoBehaviour, IStatus;
#else
        /// <summary>
        /// This will run as an <see cref="IEnumerator"/> coroutine when the effect starts.
        /// </summary>
        public abstract IEnumerator EnableModule<T>(T monoBehaviour, StatusEffect statusEffect, ModuleInstance moduleInstance) where T : MonoBehaviour, IStatus;
        /// <summary>
        /// Use this callback to do something when the effect ends.
        /// </summary>
        public abstract void DisableModule<T>(T monoBehaviour, StatusEffect statusEffect, ModuleInstance moduleInstance) where T : MonoBehaviour, IStatus;
#endif
    }
}
