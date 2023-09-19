using System.Collections;
using UnityEngine;

namespace StatusEffects
{
    public abstract class CustomEffect : ScriptableObject
    {
        /// <summary>
        /// This will run as an <see cref="IEnumerator"/> coroutine when the effect starts.
        /// </summary>
        public abstract IEnumerator Effect<T>(T monoBehaviour, StatusEffect statusEffect) where T : MonoBehaviour, IStatus;
        /// <summary>
        /// Use this callback to do something when the effect ends.
        /// </summary>
        public abstract void EffectEnd<T>(T monoBehaviour, StatusEffect statusEffect) where T : MonoBehaviour, IStatus;
    }
}
