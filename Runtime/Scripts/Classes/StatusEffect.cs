#if UNITASK
using Cysharp.Threading.Tasks;
#endif
using System.Threading;
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
        
        private List<CancellationTokenSource> m_ModuleTokenSources;
        [HideInInspector] public CancellationTokenSource TimedTokenSource;

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

            m_ModulesEnabled = true;
            Started?.Invoke();
        }

#nullable enable
        internal void DisableModules(StatusManager manager)
#nullable disable
        {
            if (Data.Modules == null || !m_ModulesEnabled)
                return;
            
            if (m_ModuleTokenSources == null)
                return;

            foreach (var tokenSources in m_ModuleTokenSources)
                tokenSources?.Cancel();

            m_ModuleTokenSources?.Clear();

            m_ModulesEnabled = false;
            Stopped?.Invoke();
        }
    }
}
