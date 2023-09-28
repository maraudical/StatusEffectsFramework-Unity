using System;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusEffect : IEquatable<StatusEffect>
    {
        public Action started;
        public Action stopped;

        public StatusEffectData data;
        public float duration;

        public Coroutine _effectCoroutine;

        public StatusEffect(StatusEffectData data, float time)
        {
            this.data = data;
            this.duration = time;
        }

        public void StartCustomEffect<T>(T monoBehaviour) where T : MonoBehaviour, IStatus
        {
            if (data.customEffect != null)
                _effectCoroutine = monoBehaviour.StartCoroutine(data.customEffect.Effect(monoBehaviour, this));

            started?.Invoke();
        }

        public void StopCustomEffect<T>(T monoBehaviour) where T : MonoBehaviour, IStatus
        {
            if (_effectCoroutine != null)
                monoBehaviour.StopCoroutine(_effectCoroutine);

            if (data.customEffect != null)
                data.customEffect.EffectEnd(monoBehaviour, this);

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
