using System;
using System.Collections.Generic;
using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// A running instance of a <see cref="StatusEffectData"/> on a <see cref="StatusManager"/>.
    /// </summary>
    [Serializable]
    public class StatusEffect
    {
        /// <summary>
        /// Raised when the status effect starts, after its modules are enabled.
        /// </summary>
        public event Action Started;
        /// <summary>
        /// Raised when the status effect ends, after its modules are disabled.
        /// </summary>
        public event Action Stopped;
        /// <summary>
        /// Raised with the new duration when <see cref="Duration"/> is set.
        /// </summary>
        public event Action<float> DurationUpdate;
        /// <summary>
        /// Raised when the stack count changes, with the previous and the current stack count.
        /// </summary>
        public event Action<int, int> StackUpdate;

        /// <summary>
        /// The data this status effect was created from.
        /// </summary>
        public StatusEffectData Data;
        /// <summary>
        /// Decides when this status effect ends. Null for <see cref="StatusEffectTiming.Infinite"/> effects.
        /// </summary>
        public StatusTimer Timer => m_Timer;
        /// <summary>
        /// How this status effect ends.
        /// </summary>
        public StatusEffectTiming Timing => m_Timer?.Timing ?? StatusEffectTiming.Infinite;
        /// <summary>
        /// The identifier of this status effect, which is unique within its <see cref="StatusManager"/>.
        /// </summary>
        public uint Id => m_Id;
        /// <summary>
        /// The duration in seconds. A <see cref="DurationStatusTimer"/> counts it down from its
        /// <see cref="DurationStatusTimer.StartTime"/>, and an <see cref="EventStatusTimer"/> reduces it by each invoke of
        /// its <see cref="StatusEvent"/>. Setting it raises <see cref="DurationUpdate"/>. Use <see cref="TimeRemaining"/> for the time left.
        /// </summary>
        public float Duration { get => m_Duration; set { m_Duration = value; DurationUpdate?.Invoke(value); } }
        /// <summary>
        /// The number of stacks. Setting it raises <see cref="StackUpdate"/>.
        /// </summary>
        public int Stacks { get => m_Stacks; set { int previousStacks = m_Stacks; m_Stacks = value; StackUpdate?.Invoke(previousStacks, m_Stacks); } }

        internal uint m_Id;
#if UNITY_EDITOR
        [SerializeReference]
#endif
        private StatusTimer m_Timer;
#if UNITY_EDITOR
        [SerializeField]
#endif
        private float m_Duration;
#if UNITY_EDITOR
        [SerializeField]
#endif
        private int m_Stacks;

        private bool m_Started;

        internal DynamicFloat[] DynamicFloats;
        internal DynamicInt[] DynamicInts;
        internal DynamicBool[] DynamicBools;

        public StatusEffect(StatusManager manager, uint id, StatusEffectData data, StatusTimer timer, float duration, int stack)
        {
            m_Id = id;
            Data = data;
            m_Timer = timer;
            m_Duration = duration;
            m_Stacks = stack;
            m_Started = false;

            var dynamicFloatsList = new List<DynamicFloat>();
            var dynamicIntsList = new List<DynamicInt>();
            var dynamicBoolsList = new List<DynamicBool>();

            foreach (var effect in data.Effects)
            {
                if (effect.ValueSource is not ValueSource.DynamicValue)
                    continue;

                // The value type decides which kind of value the effect changes.
                switch (effect.ValueType)
                {
                    case ValueType.Float:
                        if (effect.DynamicFloat)
                            dynamicFloatsList.Add(effect.DynamicFloat.ValueEvent(manager, this, effect));
                        break;
                    case ValueType.Int:
                        if (effect.DynamicInt)
                            dynamicIntsList.Add(effect.DynamicInt.ValueEvent(manager, this, effect));
                        break;
                    case ValueType.Bool:
                        if (effect.DynamicBool)
                            dynamicBoolsList.Add(effect.DynamicBool.ValueEvent(manager, this, effect));
                        break;
                }
            }

            DynamicFloats = dynamicFloatsList.ToArray();
            DynamicInts = dynamicIntsList.ToArray();
            DynamicBools = dynamicBoolsList.ToArray();
        }

        /// <summary>
        /// The time left in seconds, as the <see cref="Timer"/> counts it. Infinite status effects return -1, and
        /// status effects that count down on an event or end on a predicate return their <see cref="Duration"/>.
        /// </summary>
        public float TimeRemaining()
        {
            return m_Timer?.TimeRemaining(this) ?? -1f;
        }

        internal void Start(StatusManager manager)
        {
            if (m_Started)
                return;

            try
            {
                foreach (var container in Data.Modules)
                    container.Module?.EnableModule(manager, this, container.ModuleInstance);
            }
            finally
            {
                m_Started = true;

                try
                {
                    Started?.Invoke();
                }
                finally
                {
                    // Started last, since a timer with nothing left ends the effect straight away
                    // and Stopped shouldn't be raised before Started.
                    m_Timer?.Start(this);
                }
            }
        }

        internal void Stop(StatusManager manager)
        {
            if (!m_Started)
                return;

            try
            {
                // Stopped first so it can't end the effect while modules are being disabled.
                try
                {
                    m_Timer?.Stop(this);
                }
                finally
                {
                    foreach (var container in Data.Modules)
                        container.Module?.DisableModule(manager, this, container.ModuleInstance);
                }
            }
            finally
            {
                m_Started = false;
                Stopped?.Invoke();
            }
        }

        internal void SetStacks(int stacks)
        {
            m_Stacks = stacks;
        }

        internal void InvokeStackUpdate(int previousStacks, int currentStacks)
        {
            StackUpdate?.Invoke(previousStacks, currentStacks);
        }
    }
}
