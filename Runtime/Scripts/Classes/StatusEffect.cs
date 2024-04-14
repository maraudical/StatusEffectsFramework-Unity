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

        public void EnableModules<T>(T monoBehaviour, int stack) where T : MonoBehaviour, IStatus
        {
            if (stack <= 0)
                return;

            if (data.modules != null)
            {
                for (int i = 0; i < stack; i++)
                {
#if UNITASK
                    CancellationTokenSource effectTokenSource;

                    foreach (var container in data.modules)
                    {
                        effectTokenSource = CancellationTokenSource.CreateLinkedTokenSource(monoBehaviour.GetCancellationTokenOnDestroy());
                        container.module.EnableModule(monoBehaviour, this, container.moduleInstance, effectTokenSource.Token);

                        if (moduleTokenSources == null)
                            moduleTokenSources = new();
                        moduleTokenSources.Add(effectTokenSource);
                    }
#else
                    foreach (var container in data.modules)
                    {
                        if (effectCoroutines == null)
                            effectCoroutines = new();
                        effectCoroutines.Add(monoBehaviour.StartCoroutine(container.module.EnableModule(monoBehaviour, this, container.moduleInstance)));
                    }
#endif
                }
            }
            
            started?.Invoke();
        }

#nullable enable
        public void DisableModules<T>(T monoBehaviour, int? stack = null) where T : MonoBehaviour, IStatus
#nullable disable
        {
            if (stack.HasValue && stack <= 0)
                return;
            
            if (data.modules != null)
            {
#if UNITASK
                if (moduleTokenSources == null)
                    return;

                for (int i = 0; (!stack.HasValue || i < stack) && moduleTokenSources.Count > 0; i++)
                {
                    foreach (var container in data.modules)
                    {
                        moduleTokenSources[moduleTokenSources.Count - 1]?.Cancel();
                        moduleTokenSources.RemoveAt(moduleTokenSources.Count - 1);
                    }
#else
                if (effectCoroutines == null)
                    return;

                for (int i = 0; (!stack.HasValue || i < stack) && effectCoroutines.Count > 0; i++)
                {
                    Coroutine coroutine;

                    foreach (var container in data.modules)
                    {
                        container.module.DisableModule(monoBehaviour, this, container.moduleInstance);
                        coroutine = effectCoroutines[effectCoroutines.Count - 1];
                        if (coroutine != null)
                            monoBehaviour.StopCoroutine(coroutine);
                        effectCoroutines.RemoveAt(effectCoroutines.Count - 1);
                    }
#endif
                }
            }

            stopped?.Invoke();
        }
    }
}
