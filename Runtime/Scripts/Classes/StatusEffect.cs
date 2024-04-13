#if UNITASK
using Cysharp.Threading.Tasks;
#endif
using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusEffect
    {
        [HideInInspector] public Action started;
        [HideInInspector] public Action stopped;

        public StatusEffectData data;
        public StatusEffectTiming timing;
        public float duration;
        public int stack;

#if UNITASK
        [HideInInspector] public List<CancellationTokenSource> moduleTokenSources;
        [HideInInspector] public CancellationTokenSource timedTokenSource;
#else
        [HideInInspector] public List<Coroutine> effectCoroutines;
        [HideInInspector] public Coroutine timedCoroutine;
#endif

        public StatusEffect(StatusEffectData data, StatusEffectTiming timing, float duration, int stack)
        {
            this.data = data;
            this.timing = timing;
            this.duration = duration;
            this.stack = stack;
        }

        public void StartCustomEffect<T>(T monoBehaviour, int stack) where T : MonoBehaviour, IStatus
        {
            if (stack <= 0)
                return;

            if (data.modules != null)
            {
                foreach (var container in data.modules)
                {
#if UNITASK
                    CancellationTokenSource effectTokenSource;

                    for (int i = 0; i < stack; i++)
                    {
                        effectTokenSource = CancellationTokenSource.CreateLinkedTokenSource(monoBehaviour.GetCancellationTokenOnDestroy());
                        container.module.EnableModule(monoBehaviour, this, container.moduleInstance, effectTokenSource.Token);

                        if (moduleTokenSources == null)
                            moduleTokenSources = new();
                        moduleTokenSources.Add(effectTokenSource);
                    }
#else
                    for (int i = 0; i < stack; i++)
                    {
                        if (effectCoroutines == null)
                            effectCoroutines = new();
                        effectCoroutines.Add(monoBehaviour.StartCoroutine(data.customEffect.Effect(monoBehaviour, this)));
                    }
#endif
                }
            }
            
            started?.Invoke();
        }

#nullable enable
        public void StopCustomEffect<T>(T monoBehaviour, int? stack = null) where T : MonoBehaviour, IStatus
#nullable disable
        {
            if (stack <= 0)
                return;

            if (data.modules != null)
            {
                foreach (var container in data.modules)
                {
#if UNITASK
                    if (moduleTokenSources == null)
                        moduleTokenSources = new();
                    int origionalCount = moduleTokenSources.Count;

                    for (int i = 0; (stack == null || i < stack) && i < origionalCount; i++)
                    {
                        moduleTokenSources[moduleTokenSources.Count - 1].Cancel();
                        moduleTokenSources.RemoveAt(moduleTokenSources.Count - 1);
                    }
#else
                    int origionalCount = effectCoroutines.Count;

                    for (int i = 0; (stack == null || i < stack) && i < origionalCount; i++)
                    {
                        data.customEffect.EffectEnd(monoBehaviour, this);

                        monoBehaviour.StopCoroutine(effectCoroutines[effectCoroutines.Count - 1]);
                        effectCoroutines.RemoveAt(effectCoroutines.Count - 1);
                    }
#endif
                }
            }

            stopped?.Invoke();
        }
    }
}
