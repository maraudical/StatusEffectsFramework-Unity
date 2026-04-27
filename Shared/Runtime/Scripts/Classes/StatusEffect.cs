#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
#elif UNITY_2023_1_OR_NEWER
using System.Threading;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;

namespace StatusEffectFramework
{
    [Serializable]
    public class StatusEffect
    {
        public event Action Started;
        public event Action Stopped;
        public event Action<float> OnDurationUpdate;
        public event Action<int, int> OnStackUpdate;

        public StatusEffectData Data;
        public StatusEffectTiming Timing;
        public uint Id => m_Id;
        public double TimeAdded => m_TimeAdded;
        public float Duration { get => m_Duration; set { m_Duration = value; OnDurationUpdate?.Invoke(value); } }
        public int Stacks { get => m_Stacks; set { m_PreviousStacks = m_Stacks; m_Stacks = value; OnStackUpdate?.Invoke(m_PreviousStacks, m_Stacks); } }

        internal uint m_Id;
#if UNITY_EDITOR
        [SerializeField]
#endif
        private double m_TimeAdded;
#if UNITY_EDITOR
        [SerializeField]
#endif
        private float m_Duration;
#if UNITY_EDITOR
        [SerializeField]
#endif
        private int m_Stacks;

        private int m_PreviousStacks;
        private bool m_ModulesEnabled;

        internal DynamicFloat[] DynamicFloats;
        internal DynamicInt[] DynamicInts;
        internal DynamicBool[] DynamicBools;

#if UNITASK || UNITY_2023_1_OR_NEWER
        private List<CancellationTokenSource> m_ModuleTokenSources;
        public CancellationTokenSource TimedTokenSource;
#else
        private List<Coroutine> m_EffectCoroutines;
        public Coroutine TimedCoroutine;
#endif

        public StatusEffect(StatusManager manager, uint id, StatusEffectData data, StatusEffectTiming timing, double timeAdded, float duration, int stack)
        {
            m_Id = id;
            Data = data;
            Timing = timing;
            m_TimeAdded = timeAdded;
            m_Duration = duration;
            m_Stacks = stack;
            m_ModulesEnabled = false;

            var dynamicFloatsList = new List<DynamicFloat>();
            var dynamicIntsList = new List<DynamicInt>();
            var dynamicBoolsList = new List<DynamicBool>();

            foreach (var effect in data.Effects)
            {
                if (effect.ValueSource is not ValueSource.DynamicValue)
                    continue;

                switch (effect.StatusName)
                {
                    case StatusNameFloat:
                        if (effect.DynamicFloatEffect)
                            dynamicFloatsList.Add(effect.DynamicFloatEffect.ValueEvent(manager, this, effect));
                        break;
                    case StatusNameInt:
                        if (effect.DynamicIntEffect)
                            dynamicIntsList.Add(effect.DynamicIntEffect.ValueEvent(manager, this, effect));
                        break;
                    case StatusNameBool:
                        if (effect.DynamicBoolEffect)
                            dynamicBoolsList.Add(effect.DynamicBoolEffect.ValueEvent(manager, this, effect));
                        break;
                }
            }

            DynamicFloats = dynamicFloatsList.ToArray();
            DynamicInts = dynamicIntsList.ToArray();
            DynamicBools = dynamicBoolsList.ToArray();
        }

        public float TimeRemaining(double elapsedTime)
        {
            return Timing switch
            {
                StatusEffectTiming.Infinite => -1f,
                StatusEffectTiming.Event or StatusEffectTiming.Predicate => Duration,
                _ => Mathf.Max(0f, Duration - (float)(elapsedTime - TimeAdded))
            };
        }

        internal void Start(StatusManager manager)
        {
            if (Data.Modules == null || m_ModulesEnabled)
                goto Start;

#if UNITASK || UNITY_2023_1_OR_NEWER
            CancellationTokenSource effectTokenSource;

            foreach (var container in Data.Modules)
            {
                if (!container.Module)
                    continue;
#if UNITASK
                effectTokenSource = CancellationTokenSource.CreateLinkedTokenSource(manager.GetCancellationTokenOnDestroy());
                container.Module.EnableModule(manager, this, container.ModuleInstance, effectTokenSource.Token).Forget();
#else
                effectTokenSource = CancellationTokenSource.CreateLinkedTokenSource(manager.destroyCancellationToken);
                _ = container.Module.EnableModule(manager, this, container.ModuleInstance, effectTokenSource.Token);
#endif

                if (m_ModuleTokenSources == null)
                    m_ModuleTokenSources = new();
                m_ModuleTokenSources.Add(effectTokenSource);
            }
#else
            foreach (var container in Data.Modules)
            {
                if (!container.Module)
                    continue;

                if (m_EffectCoroutines == null)
                    m_EffectCoroutines = new();
                m_EffectCoroutines.Add(manager.StartCoroutine(container.Module.EnableModule(manager, this, container.ModuleInstance)));
            }
#endif

            m_ModulesEnabled = true;
        Start:
            Started?.Invoke();
        }

#nullable enable
        internal void Stop(StatusManager manager)
#nullable disable
        {
            if (Data.Modules == null || !m_ModulesEnabled)
                goto Stop;

#if UNITASK || UNITY_2023_1_OR_NEWER
            if (m_ModuleTokenSources == null)
                goto Stop;

            foreach (var tokenSources in m_ModuleTokenSources)
                tokenSources?.Cancel();

            m_ModuleTokenSources?.Clear();
#else
            if (m_EffectCoroutines == null)
                goto Stop;
            
            foreach (var container in Data.Modules)
                container.Module?.DisableModule(manager, this, container.ModuleInstance);

            foreach (var coroutine in m_EffectCoroutines)
                if (coroutine != null)
                    manager.StopCoroutine(coroutine);

            m_EffectCoroutines.Clear();
#endif

            m_ModulesEnabled = false;
        Stop:
            Stopped?.Invoke();
        }

        internal void SetStacks(int stacks)
        {
            Stacks = stacks;
        }

        internal void InvokeStackUpdate(int previousStacks, int currentStacks)
        { 
            OnStackUpdate?.Invoke(previousStacks, currentStacks);
        }
    }
}
