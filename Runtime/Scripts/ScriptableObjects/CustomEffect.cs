#if UNITASK
using Cysharp.Threading.Tasks;
#else
using System.Collections;
#endif
using System.Threading;
using UnityEngine;

namespace StatusEffects
{
    public abstract class CustomEffect : ScriptableObject
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
        public abstract UniTask Effect<T>(T monoBehaviour, StatusEffect statusEffect, CancellationToken token) where T : MonoBehaviour, IStatus;
#else
        /// <summary>
        /// This will run as an <see cref="IEnumerator"/> coroutine when the effect starts.
        /// </summary>
        public abstract IEnumerator Effect<T>(T monoBehaviour, StatusEffect statusEffect) where T : MonoBehaviour, IStatus;
        /// <summary>
        /// Use this callback to do something when the effect ends.
        /// </summary>
        public abstract void EffectEnd<T>(T monoBehaviour, StatusEffect statusEffect) where T : MonoBehaviour, IStatus;
#endif
    }
}
