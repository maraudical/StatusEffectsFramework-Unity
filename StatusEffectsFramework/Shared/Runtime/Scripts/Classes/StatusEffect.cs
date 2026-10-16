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
        /// How this status effect ends.
        /// </summary>
        public StatusEffectTiming Timing;
        /// <summary>
        /// The identifier of this status effect, which is unique within its <see cref="StatusManager"/>.
        /// </summary>
        public uint Id => m_Id;
        /// <summary>
        /// The <see cref="StatusManager.CurrentTime"/> when this status effect was added.
        /// </summary>
        public double TimeAdded => m_TimeAdded;
        /// <summary>
        /// The duration in seconds, counted from <see cref="TimeAdded"/> for <see cref="StatusEffectTiming.Duration"/>. For
        /// <see cref="StatusEffectTiming.Event"/> it is what is left, reduced by each invoke of the <see cref="StatusEvent"/>.
        /// Setting it raises <see cref="DurationUpdate"/>. Use <see cref="TimeRemaining"/> for the time left.
        /// </summary>
        public float Duration { get => m_Duration; set { m_Duration = value; DurationUpdate?.Invoke(value); } }
        /// <summary>
        /// The number of stacks. Setting it raises <see cref="StackUpdate"/>.
        /// </summary>
        public int Stacks { get => m_Stacks; set { int previousStacks = m_Stacks; m_Stacks = value; StackUpdate?.Invoke(previousStacks, m_Stacks); } }

        internal uint m_Id;
        /// <summary>
        /// The event an <see cref="StatusEffectTiming.Event"/> effect counts down on, otherwise null.
        /// Only used to tell whether two effects count down on the same event.
        /// </summary>
        internal StatusEvent StatusEvent;
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

        private bool m_ModulesEnabled;

        internal DynamicFloat[] DynamicFloats;
        internal DynamicInt[] DynamicInts;
        internal DynamicBool[] DynamicBools;

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

                // The status name's type decides which kind of value the effect changes.
                switch (effect.StatusName.Resolve())
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

        /// <summary>
        /// The time left in seconds, given the <see cref="StatusManager.CurrentTime"/>. Infinite status effects return -1, and
        /// status effects that count down on an event or end on a predicate return their <see cref="Duration"/>.
        /// </summary>
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
            if (m_ModulesEnabled)
                return;
            
            try
            {
                foreach (var container in Data.Modules)
                    container.Module?.EnableModule(manager, this, container.ModuleInstance);
            }
            finally
            {
                m_ModulesEnabled = true;
                Started?.Invoke();
            }
        }

        internal void Stop(StatusManager manager)
        {
            if (!m_ModulesEnabled)
                return;

            try
            {
                foreach (var container in Data.Modules)
                    container.Module?.DisableModule(manager, this, container.ModuleInstance);
            }
            finally
            {
                m_ModulesEnabled = false;
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
