#if UNITASK
using Cysharp.Threading.Tasks;
#endif
using System;
using System.Threading;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusEffect : IEquatable<StatusEffect>
    {
        [HideInInspector] public Action started;
        [HideInInspector] public Action stopped;

        public StatusEffectData data;
        public float duration;

#if UNITASK
        [HideInInspector] public UniTask effectTask;
        [HideInInspector] public CancellationTokenSource effectTokenSource;
        [HideInInspector] public UniTask timedTask;
        [HideInInspector] public CancellationTokenSource timedTokenSource;
#else
        [HideInInspector] public Coroutine effectCoroutine;
        [HideInInspector] public Coroutine timedCoroutine;
#endif

        public StatusEffect(StatusEffectData data, float duration)
        {
            this.data = data;
            this.duration = duration;
        }

        public void StartCustomEffect<T>(T monoBehaviour) where T : MonoBehaviour, IStatus
        {
            if (data.customEffect != null)
#if UNITASK
            {
                effectTokenSource = CancellationTokenSource.CreateLinkedTokenSource(monoBehaviour.GetCancellationTokenOnDestroy());
                effectTask = data.customEffect.Effect(monoBehaviour, this, effectTokenSource.Token);
            }
#else
                effectCoroutine = monoBehaviour.StartCoroutine(data.customEffect.Effect(monoBehaviour, this));
#endif

            started?.Invoke();
        }

        public void StopCustomEffect<T>(T monoBehaviour) where T : MonoBehaviour, IStatus
        {
            if (data.customEffect != null)
#if UNITASK
                effectTokenSource?.Cancel();
#else
                data.customEffect.EffectEnd(monoBehaviour, this);

            if (effectCoroutine != null)
                monoBehaviour.StopCoroutine(effectCoroutine);
#endif

                stopped?.Invoke();
        }

        public bool Equals(StatusEffect other)
        {
            if (other == null)
                return false;

            return (data == other.data);
        }

        public override bool Equals(object obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return data.GetHashCode();
        }
    }
}
