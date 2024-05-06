#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
#elif UNITY_2023_1_OR_NEWER
using System.Threading;
#endif
using System;
using System.Collections.Generic;
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

#if UNITASK || UNITY_2023_1_OR_NEWER
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

        public void EnableModules(StatusManager manager, int stack)
        {
            if (stack <= 0)
                return;

            if (data.modules != null)
            {
                for (int i = 0; i < stack; i++)
                {
#if UNITASK || UNITY_2023_1_OR_NEWER
                    CancellationTokenSource effectTokenSource;

                    foreach (var container in data.modules)
                    {
#if UNITASK
                        effectTokenSource = CancellationTokenSource.CreateLinkedTokenSource(manager.GetCancellationTokenOnDestroy());
#else
                        effectTokenSource = CancellationTokenSource.CreateLinkedTokenSource(manager.destroyCancellationToken);
#endif
                        container.module.EnableModule(manager, this, container.moduleInstance, effectTokenSource.Token);

                        if (moduleTokenSources == null)
                            moduleTokenSources = new();
                        moduleTokenSources.Add(effectTokenSource);
                    }
#else
                    foreach (var container in data.modules)
                    {
                        if (effectCoroutines == null)
                            effectCoroutines = new();
                        effectCoroutines.Add(manager.StartCoroutine(container.module.EnableModule(manager, this, container.moduleInstance)));
                    }
#endif
                }
            }
            
            started?.Invoke();
        }

#nullable enable
        public void DisableModules(StatusManager manager, int? stack = null)
#nullable disable
        {
            if (stack.HasValue && stack <= 0)
                return;
            
            if (data.modules != null)
            {
#if UNITASK || UNITY_2023_1_OR_NEWER
                if (moduleTokenSources == null)
                    return;

                for (int i = 0; (!stack.HasValue || i < stack) && moduleTokenSources.Count > 0; i++)
                {
                    foreach (var container in data.modules)
                    {
                        CancellationTokenSource cancellationTokenSource = moduleTokenSources[moduleTokenSources.Count - 1];
                        cancellationTokenSource?.Cancel();
                        cancellationTokenSource?.Dispose();
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
                        container.module.DisableModule(manager, this, container.moduleInstance);
                        coroutine = effectCoroutines[effectCoroutines.Count - 1];
                        if (coroutine != null)
                            manager.StopCoroutine(coroutine);
                        effectCoroutines.RemoveAt(effectCoroutines.Count - 1);
                    }
#endif
                }
            }

            stopped?.Invoke();
        }
    }
}
