#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
#elif UNITY_2023_1_OR_NEWER
using System.Threading;
#endif
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Runtime.CompilerServices;

namespace StatusEffects
{
    [Serializable]
    public class StatusEffect
    {
        [HideInInspector] public event Action Started;
        [HideInInspector] public event Action Stopped;
        [HideInInspector] public event Action<float> OnDurationUpdate;
        [HideInInspector] public event Action<int, int> OnStackUpdate;

        public StatusEffectData Data;
        public StatusEffectTiming Timing;
        public float Duration { get => m_Duration; set { m_Duration = value; OnDurationUpdate?.Invoke(value); } }
        public int Stacks { get => m_Stacks; set { m_PreviousStack = m_Stacks; m_Stacks = value; } }

        [SerializeField] private float m_Duration;
        [SerializeField] private int m_Stacks;

        private int m_PreviousStack;
        private Hash128 m_InstanceId;
        private bool m_ModulesEnabled;

#if UNITASK || UNITY_2023_1_OR_NEWER
        private List<CancellationTokenSource> m_ModuleTokenSources;
        [HideInInspector] public CancellationTokenSource TimedTokenSource;
#else
        private List<Coroutine> m_EffectCoroutines;
        [HideInInspector] public Coroutine TimedCoroutine;
#endif
        public StatusEffect(StatusEffectData data, StatusEffectTiming timing, float duration, int stack)
        {
            Data = data;
            Timing = timing;
            m_Duration = duration;
            m_Stacks = stack;
            m_ModulesEnabled = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Hash128 GetInstanceID()
        {
            if (!m_InstanceId.isValid)
                SetInstanceID();

            return m_InstanceId;
        }

        internal void SetInstanceID() => SetInstanceID(Hash128.Compute(Guid.NewGuid().ToString("N")));

        internal void SetInstanceID(Hash128 value)
        {
            m_InstanceId = value;
        }

        internal void InvokeStackUpdate()
        {
            OnStackUpdate?.Invoke(m_PreviousStack, m_Stacks);
        }

        internal void EnableModules(StatusManager manager)
        {
            if (Data.Modules == null || m_ModulesEnabled)
                return;
#if UNITASK || UNITY_2023_1_OR_NEWER
            CancellationTokenSource effectTokenSource;

            foreach (var container in Data.Modules)
            {
                if (!container.Module)
                    continue;
#if UNITASK
                effectTokenSource = CancellationTokenSource.CreateLinkedTokenSource(manager.GetCancellationTokenOnDestroy());
#else
                effectTokenSource = CancellationTokenSource.CreateLinkedTokenSource(manager.destroyCancellationToken);
#endif
                container.Module.EnableModule(manager, this, container.ModuleInstance, effectTokenSource.Token);

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
            Started?.Invoke();
        }

#nullable enable
        internal void DisableModules(StatusManager manager)
#nullable disable
        {
            if (Data.Modules == null || !m_ModulesEnabled)
                return;

#if UNITASK || UNITY_2023_1_OR_NEWER
            if (m_ModuleTokenSources == null)
                return;

            foreach (var tokenSources in m_ModuleTokenSources)
                tokenSources?.Cancel();

            m_ModuleTokenSources?.Clear();
#else
            if (m_EffectCoroutines == null)
                return;
            
            foreach (var container in Data.Modules)
                container.Module?.DisableModule(manager, this, container.ModuleInstance);

            foreach (var coroutine in m_EffectCoroutines)
                if (coroutine != null)
                    manager.StopCoroutine(coroutine);

            m_EffectCoroutines.Clear();
#endif

            m_ModulesEnabled = false;
            Stopped?.Invoke();
        }
    }
}
