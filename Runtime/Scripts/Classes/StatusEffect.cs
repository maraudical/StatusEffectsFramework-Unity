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
using Unity.VisualScripting.YamlDotNet.Core.Tokens;

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
        public int Stack { get => m_Stack; set { m_PreviousStack = m_Stack; m_Stack = value; } }

        [SerializeField] private float m_Duration;
        [SerializeField] private int m_Stack;

        private int m_PreviousStack;
        private int? m_InstanceId;
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
            m_Stack = stack;
            m_ModulesEnabled = false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public int GetInstanceID()
        {
            if (!m_InstanceId.HasValue)
                SetInstanceID(Guid.NewGuid().GetHashCode());

            return m_InstanceId.Value;
        }

        internal void SetInstanceID(int value)
        {
            m_InstanceId = value;
        }

        internal void InvokeStackUpdate()
        {
            OnStackUpdate?.Invoke(m_PreviousStack, m_Stack);
        }

        internal void EnableModules(StatusManager manager)
        {
            if (Data.Modules == null || m_ModulesEnabled)
                return;
#if UNITASK || UNITY_2023_1_OR_NEWER
            CancellationTokenSource effectTokenSource;

            foreach (var container in Data.Modules)
            {
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

            foreach (var container in m_ModuleTokenSources)
                container?.Cancel();
#else
            if (m_EffectCoroutines == null)
                return;

            Coroutine coroutine;

            foreach (var container in m_EffectCoroutines)
            {
                container.Module.DisableModule(manager, this, container.ModuleInstance);
                coroutine = m_EffectCoroutines[m_EffectCoroutines.Count - 1];
                if (coroutine != null)
                    manager.StopCoroutine(coroutine);
                m_EffectCoroutines.RemoveAt(m_EffectCoroutines.Count - 1);
            }
#endif

            m_ModuleTokenSources?.Clear();
            m_ModulesEnabled = false;
            Stopped?.Invoke();
        }
    }
}
