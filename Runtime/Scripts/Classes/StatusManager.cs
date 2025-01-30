#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
#elif UNITY_2023_1_OR_NEWER
using StatusEffects.Extensions;
using System.Threading;
#else
using System.Collections;
#endif
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Runtime.CompilerServices;

namespace StatusEffects
{
    /// <summary>
    /// A component that manages currently active Status Effects.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Status Effect/Status Manager")]
    public class StatusManager : MonoBehaviour, IStatusManager
    {
        [HideInInspector] public event System.Action<StatusEffect, StatusEffectAction, int> OnStatusEffect;
        event System.Action<StatusEffect> IStatusManager.ValueUpdate
        {
            add => ValueUpdate += value;
            remove => ValueUpdate -= value;
        }
        [HideInInspector] internal event System.Action<StatusEffect> ValueUpdate;

        public IEnumerable<StatusEffect> Effects => m_Effects?.Values ?? Enumerable.Empty<StatusEffect>();

        internal System.Action<StatusEffect, bool> TimerOverride;

        [SerializeField] private Dictionary<int, StatusEffect> m_Effects;

#if UNITY_EDITOR
        [SerializeField] private List<StatusEffect> m_EditorOnlyEffects;

#endif
        #region Public Methods
        /// <summary>
        /// Gets a <see cref="StatusEffect"/>s by its <see cref="StatusEffect.GetInstanceID"/>.
        /// </summary>
        public bool GetStatusEffect(int instanceId, out StatusEffect statusEffect)
        {
            if (m_Effects == null)
            {
                statusEffect = null;
                return false;
            }
            
            return m_Effects.TryGetValue(instanceId, out statusEffect);
        }
        /// <summary>
        /// Returns the listed <see cref="StatusEffect"/>s in a <see cref="List{}"/> for the <see cref="StatusManager"/>.
        /// </summary>
#nullable enable
        public IEnumerable<StatusEffect> GetStatusEffects(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null)
#nullable disable
        {
            // Return the effects for a given monobehaviour, if given a group
            // or name to match only return effects within those categories.
            return Effects.Where(e => (name  == null || e.Data.ComparableName  == name) 
                                   && (group == null ||(e.Data.Group & group) != 0)
                                   && (data  == null || e.Data == data));
        }
        /// <summary>
        /// Returns the first <see cref="StatusEffect"/>s that matches the given parameters. If none are found returns null.
        /// </summary>
#nullable enable
        public StatusEffect GetFirstStatusEffect(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null)
#nullable disable
        {
            if (Effects == null)
                return null;
            // Return the effects for a given monobehaviour, if given a group
            // or name to match only return effects within those categories.
            return Effects.FirstOrDefault(e => (name  == null || e.Data.ComparableName == name)
                                            && (group == null ||(e.Data.Group & group) != 0)
                                            && (data  == null || e.Data == data));
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to this <see cref="StatusManager"/>. Returns null if no <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, int stacks = 1)
        {
            return AddStatusEffect(statusEffectData, StatusEffectTiming.Infinite, null, null, null, null, stacks);
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to the <see cref="StatusManager"/>. 
        /// The given <see cref="float"/> time will limit the duration of the 
        /// effect in seconds. Returns null if no <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, int stacks = 1)
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");

            StatusEffect statusEffect = AddStatusEffect(statusEffectData, StatusEffectTiming.Duration, duration, null, null, null, stacks);

            if (statusEffect == null)
                return null;
            // Begin a timer on the monobehaviour.
            if (TimerOverride != null) TimerOverride.Invoke(statusEffect, true); else CreateTimer(statusEffect);

            return statusEffect;
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to the <see cref="StatusManager"/>. 
        /// The given <see cref="float"/> time will limit the duration of the 
        /// effect where each invocation of the <see cref="UnityEvent"/> 
        /// will reduce the duration by the given interval. Returns null if no 
        /// <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, UnityEvent unityEvent, float interval = 1, int stacks = 1)
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");
            if (unityEvent == null)
                Debug.LogError("The given Unity Event is null!");
            // Create the effect
            StatusEffect statusEffect = AddStatusEffect(statusEffectData, StatusEffectTiming.Event, duration, null, unityEvent, interval, stacks);
            // Check for null or 0 duration effect
            if (statusEffect == null)
                return null;
            // Begin a unity event on the monobehaviour.
            CreateUnityEvent(statusEffect, unityEvent, interval);

            return statusEffect;
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to a <see cref="StatusManager"/>. 
        /// The StatusEffect will be removed when the given 
        /// <see cref="System.Func{bool}"/> is true. Returns null if no 
        /// <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, System.Func<bool> predicate, int stacks = 1)
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");
            if (predicate == null)
                Debug.LogError("The given predicate is null!");

            StatusEffect statusEffect = AddStatusEffect(statusEffectData, StatusEffectTiming.Predicate, null, predicate, null, null, stacks);

            if (statusEffect == null)
                return null;
            // Begin a predicate on the monobehaviour.
            CreatePredicate(statusEffect, predicate);

            return statusEffect;
        }
        /// <summary>
        /// Removes a <see cref="StatusEffect"/> from a <see cref="MonoBehaviour"/>.
        /// </summary>
        public void RemoveStatusEffect(StatusEffect statusEffect)
        {
            if (statusEffect == null || m_Effects == null)
                return;
            // Stop the timer
#if UNITASK || UNITY_2023_1_OR_NEWER
            statusEffect.TimedTokenSource?.Cancel();
#else
            if (statusEffect.TimedCoroutine != null)
                StopCoroutine(statusEffect.TimedCoroutine);
#endif
            // Remove the effects for a given monobehaviour.
            m_Effects.Remove(statusEffect.GetInstanceID());
#if UNITY_EDITOR
            m_EditorOnlyEffects.Remove(statusEffect);
#endif

            ValueUpdate?.Invoke(statusEffect);
            // If a module exists it will be stopped.
            statusEffect.DisableModules(this);
            
            OnStatusEffect?.Invoke(statusEffect, StatusEffectAction.RemovedStatusEffect, statusEffect.Stacks);
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/> from a <see cref="MonoBehaviour"/>. If a stack count is given it will remove only the specified amount.
        /// </summary>
#nullable enable
        public void RemoveStatusEffect(StatusEffectData statusEffectData, int? stacks = null)
#nullable disable
        {
            if (statusEffectData == null || m_Effects == null)
                return;

            if (stacks.HasValue && stacks.Value <= 0)
                return;
            
            IEnumerable<StatusEffect> leastToMostValueThenDuration = m_Effects.Values.Where(se => se.Data == statusEffectData)
                                                                                     .OrderBy(se => se.Data.BaseValue)
                                                                                     .ThenBy(se => se.Timing is StatusEffectTiming.Infinite or StatusEffectTiming.Predicate ? float.PositiveInfinity : se.Duration);
            IterateRemoval(leastToMostValueThenDuration, stacks);
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/> that 
        /// have the same <see cref="ComparableName"/>. If a stacks count is given it will remove only the specified amount.
        /// </summary>
        public void RemoveStatusEffect(ComparableName name, int? stacks = null)
        {
            if (m_Effects == null)
                return;

            if (stacks.HasValue && stacks.Value <= 0)
                return;
            
            IEnumerable<StatusEffect> leastToMostValueThenDuration = m_Effects.Values.Where(se => se.Data.ComparableName == name)
                                                                                     .OrderBy(se => se.Data.BaseValue)
                                                                                     .ThenBy(se => se.Timing is StatusEffectTiming.Infinite or StatusEffectTiming.Predicate ? float.PositiveInfinity : se.Duration);
            IterateRemoval(leastToMostValueThenDuration, stacks);
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/> that 
        /// are part of the given <see cref="StatusEffectGroup"/> group. If a stacks count is given it will remove only the specified amount.
        /// </summary>
        public void RemoveStatusEffect(StatusEffectGroup group, int? stacks = null)
        {
            if (m_Effects == null)
                return;

            if (stacks.HasValue && stacks.Value <= 0)
                return;

            IEnumerable<StatusEffect> leastToMostValueThenDuration = m_Effects.Values.Where(se => (se.Data.Group & group) != 0)
                                                                                     .OrderBy(se => se.Data.BaseValue)
                                                                                     .ThenBy(se => se.Timing is StatusEffectTiming.Infinite or StatusEffectTiming.Predicate ? float.PositiveInfinity : se.Duration);
            IterateRemoval(leastToMostValueThenDuration, stacks);
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/>.
        /// </summary>
        public void RemoveAllStatusEffects()
        {
            if (m_Effects == null)
                return;
            // From the end of the list iterate through and remove all.
            for (int i = m_Effects.Count - 1; i >= 0; i--)
                RemoveStatusEffect(m_Effects.ElementAt(i).Value);
        }
        /// <summary>
        /// Forcibly adds a <see cref="StatusEffect"/> regardless of whether it can or can't.
        /// </summary>
        internal StatusEffect ForceAddStatusEffect(int instanceId, StatusEffectData statusEffectData, StatusEffectTiming timing, float duration, int stacks)
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");

            StatusEffect statusEffect = new StatusEffect(statusEffectData, timing, duration, stacks);
            statusEffect.SetInstanceID(instanceId);
            // Add the effect for a given monobehaviour. This also is the first time
            // initializing so we need to initialize all of the Status Variables
            if (m_Effects == null)
            {
                m_Effects = new Dictionary<int, StatusEffect> { { statusEffect.GetInstanceID(), statusEffect } };
#if UNITY_EDITOR
                m_EditorOnlyEffects = new List<StatusEffect> { statusEffect };
#endif
            }
            else
            {
                m_Effects.Add(statusEffect.GetInstanceID(), statusEffect);
#if UNITY_EDITOR
                m_EditorOnlyEffects.Add(statusEffect);
#endif
            }

            ValueUpdate?.Invoke(statusEffect);
            // If a module exists it will be started.
            statusEffect.EnableModules(this);

            OnStatusEffect?.Invoke(statusEffect, StatusEffectAction.AddedStatusEffect, stacks);

            // Begin a timer on the monobehaviour if it is a realtime timer.
            if (timing == StatusEffectTiming.Duration)
                if (TimerOverride != null) TimerOverride.Invoke(statusEffect, false); else CreateTimer(statusEffect, false);
            // Return the effect in case it is wanted for other reference.
            return statusEffect;
        }

        internal void InvokeValueUpdate(StatusEffect statusEffect)
        {
            ValueUpdate?.Invoke(statusEffect);
        }

        internal void InvokeOnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int stacks)
        {
            OnStatusEffect?.Invoke(statusEffect, action, stacks);
        }
        #endregion

        #region Private Methods
        private void IterateRemoval(IEnumerable<StatusEffect> statusEffectsToRemove, int? stacks)
        {
            int removedCount = 0;
            int currentRemoveCount;
            int currentStackCount;

            foreach (var statusEffect in statusEffectsToRemove)
            {
                if (stacks != null)
                {
                    currentStackCount = statusEffect.Stacks;

                    if (removedCount + currentStackCount > stacks)
                    {
                        currentRemoveCount = (int)(currentStackCount - (removedCount + currentStackCount - stacks));
                        statusEffect.Stacks -= currentRemoveCount;

                        ValueUpdate?.Invoke(statusEffect);
                        statusEffect.InvokeStackUpdate();
                        OnStatusEffect?.Invoke(statusEffect, StatusEffectAction.RemovedStacks, currentRemoveCount);

                        break;
                    }
                    removedCount += currentStackCount;
                }
                // If it got to this point we can remove the effect. Either
                // we removed all its stacks or there was no stack count.
                RemoveStatusEffect(statusEffect);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateTimer(StatusEffect statusEffect, bool remove = true)
        {
#if UNITASK
            statusEffect.TimedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            TimedEffect(statusEffect.TimedTokenSource.Token).Forget();
#elif UNITY_2023_1_OR_NEWER
            statusEffect.TimedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _ = TimedEffect(statusEffect.TimedTokenSource.Token);
#else
            statusEffect.TimedCoroutine = StartCoroutine(TimedEffect());
#endif
            // Timer method
#if UNITASK
            async UniTask TimedEffect(CancellationToken token)
#elif UNITY_2023_1_OR_NEWER
            async Awaitable TimedEffect(CancellationToken token)
#else
            IEnumerator TimedEffect()
#endif
            {
                float startTime = Time.time;
                float startDuration = statusEffect.Duration;
                // Basic decreasing timer.
                while (statusEffect.Duration > 0
#if UNITASK || UNITY_2023_1_OR_NEWER
                   && !token.IsCancellationRequested
#endif
                   )
                {
#if UNITASK
                    await UniTask.NextFrame(token);
#elif UNITY_2023_1_OR_NEWER
                    await Awaitable.NextFrameAsync(token);
#else
                    yield return null;
#endif
                    statusEffect.Duration = startDuration - Time.time + startTime;
                }
#if UNITASK || UNITY_2023_1_OR_NEWER
                if (!token.IsCancellationRequested)
#endif
                // Once it has ended remove the given effect.
                if (remove)
                    RemoveStatusEffect(statusEffect);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateUnityEvent(StatusEffect statusEffect, UnityEvent unityEvent, float interval, bool remove = true)
        {
            // Check for 0 duration effect
            if (statusEffect.Duration <= 0)
            {
                if (remove)
                    RemoveStatusEffect(statusEffect);
                return;
            }
            // Subscribe to the decrement method.
            unityEvent.AddListener(Decrement);
            statusEffect.Stopped += Unsubscribe;

            void Decrement()
            {
                statusEffect.Duration -= interval;

                if (statusEffect.Duration <= 0)
                {
                    if (remove)
                        RemoveStatusEffect(statusEffect);
                    Unsubscribe();
                }
            }

            void Unsubscribe()
            {
                unityEvent.RemoveListener(Decrement);
                statusEffect.Stopped -= Unsubscribe;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreatePredicate(StatusEffect statusEffect, System.Func<bool> predicate, bool remove = true)
        {
#if UNITASK
            statusEffect.TimedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            TimedEffect(statusEffect.TimedTokenSource.Token).Forget();
#elif UNITY_2023_1_OR_NEWER
            statusEffect.TimedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _ = TimedEffect(statusEffect.TimedTokenSource.Token);
#else
            statusEffect.TimedCoroutine = StartCoroutine(TimedEffect());
#endif
            // Timer method
#if UNITASK
            async UniTask TimedEffect(CancellationToken token)
#elif UNITY_2023_1_OR_NEWER
            async Awaitable TimedEffect(CancellationToken token)
#else
            IEnumerator TimedEffect()
#endif
            {
                // Wait until the predicate is true.
#if UNITASK
                await UniTask.WaitUntil(predicate, cancellationToken: token);
#elif UNITY_2023_1_OR_NEWER
                await AwaitableExtensions.WaitUntil(predicate, token);
#else
                yield return new WaitUntil(predicate);
#endif
                // Remove the given effect.
                if (remove)
                    RemoveStatusEffect(statusEffect);
            }
        }

#nullable enable
        private StatusEffect AddStatusEffect(StatusEffectData statusEffectData, StatusEffectTiming timing, float? duration, System.Func<bool> predicate, UnityEvent unityEvent, float? interval, int stacks)
#nullable disable
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Please only add Status Effects in play mode!");
                return null;
            }
#endif
            if (stacks <= 0)
                return null;
            
            if (!statusEffectData)
                throw new System.Exception($"Attempted to add a null {typeof(StatusEffect).Name} " +
                                           $"to a {typeof(MonoBehaviour).Name}. This is not allowed.");
            // If the duration given is less than zero it won't be applied.
            if (duration.HasValue && duration.Value < 0)
                return null;
            // First determine correct duration.
            float durationValue = duration.HasValue ? duration.Value : -1;

            StatusEffectAction action = StatusEffectAction.AddedStatusEffect;
            // Declare here to use later.
            StatusEffect flagForRemoval = null;
            StatusEffect statusEffect = null;
            bool addedToStack = false;
            // If stacking is allowed then check for the max stacks and possible stack merging.
            if (statusEffectData.AllowEffectStacking)
            {
                IEnumerable<StatusEffect> statusEffects = GetStatusEffects(data: statusEffectData);
                int stackCount = 0;

                if (statusEffects == null || statusEffects.Count() <= 0)
                {
                    if (!TryToLimitStacks())
                        return null;
                    goto CheckConditionals;
                }

                StatusEffect infiniteEffect = null;
                // Go through all similar effects and count the stacks and if there is an
                // infinite effect.
                foreach (var existentEffect in statusEffects)
                {
                    if (existentEffect.Timing is StatusEffectTiming.Infinite)
                        infiniteEffect = existentEffect;

                    stackCount += existentEffect.Stacks;
                }

                if (!TryToLimitStacks())
                    return null;
                // We can only safely merge infinite stacks because merging things with
                // duration or predicates we either can't or its too difficult to determine
                // their similarity.
                if (timing is StatusEffectTiming.Infinite && infiniteEffect != null)
                {
                    statusEffect = infiniteEffect;
                    addedToStack = true;
                    goto CheckConditionals;
                }
                /// <summary>False when stack count is already max, otherwise true.</summary>
                bool TryToLimitStacks()
                {
                    // Check if the current stacked amount is already max.
                    if (statusEffectData.MaxStacks >= 0)
                        if (stackCount >= statusEffectData.MaxStacks)
                            return false;
                        // Otherwise cut the given stack if it would've been over the max.
                        else if (stackCount + stacks > statusEffectData.MaxStacks)
                            stacks = statusEffectData.MaxStacks - stackCount;

                    return true;
                }
            }
            // Non-stackable: Check to delete the effect if it already exists to prevent duplicates.
            else
            {
                stacks = 1;

                StatusEffect oldStatusEffect = GetFirstStatusEffect(name: statusEffectData.ComparableName, data: statusEffectData.ComparableName ? null : statusEffectData);
                
                if (oldStatusEffect == null)
                    goto CheckConditionals;

                switch (statusEffectData.NonStackingBehaviour)
                {
                    case NonStackingBehaviour.MatchHighestValue:
                        if (statusEffectData.BaseValue == oldStatusEffect.Data.BaseValue)
                            goto case NonStackingBehaviour.TakeHighestDuration;

                        float baseValue = Mathf.Abs(statusEffectData.BaseValue);
                        float oldBaseValue = Mathf.Abs(statusEffectData.BaseValue);
                        // WARNING: There is an extremely special case here where
                        // a player may either have or try to apply an effect which
                        // has an infinite duration (-1). In this situation, attempt
                        // to take the higest value, and if they are the same take
                        // the infinite duration effect.
                        if (timing is StatusEffectTiming.Infinite || oldStatusEffect.Timing is StatusEffectTiming.Infinite)
                        {
                            if (baseValue < oldStatusEffect.Data.BaseValue) 
                                return null;
                            else if (baseValue > oldStatusEffect.Data.BaseValue || oldStatusEffect.Timing is not StatusEffectTiming.Infinite)
                            {
                                flagForRemoval = oldStatusEffect;
                                break;
                            }
                            else
                                return null;
                        }
                        // Find which effect is highest value.
                        StatusEffectData higestValueData = baseValue < oldBaseValue ? oldStatusEffect.Data : statusEffectData;
                        float highestValueDuration = baseValue < oldBaseValue ? oldStatusEffect.Duration : durationValue;
                        StatusEffectData lowestValueData = baseValue < oldBaseValue ? statusEffectData : oldStatusEffect.Data;
                        float lowestValueDuration = baseValue < oldBaseValue ? durationValue : oldStatusEffect.Duration;
                        // Calculate the new duration = d1 + d2 / (v1 / v2). Note this assumes neither base value will ever be 0.
                        if (higestValueData.BaseValue == 0 || lowestValueData.BaseValue == 0)
                            Debug.LogError($"{(higestValueData.BaseValue == 0 ? higestValueData : lowestValueData)} has a base value of 0! This will cause an error!");

                        durationValue = highestValueDuration + lowestValueDuration / (Mathf.Abs(higestValueData.BaseValue) / Mathf.Abs(lowestValueData.BaseValue));
                        statusEffectData = higestValueData;
                        flagForRemoval = oldStatusEffect;
                        break;
                    case NonStackingBehaviour.TakeHighestDuration:
                        if (oldStatusEffect.Timing is StatusEffectTiming.Infinite || (durationValue < oldStatusEffect.Duration && timing is not StatusEffectTiming.Infinite))
                            return null;
                        else
                            flagForRemoval = oldStatusEffect;
                        break;
                    case NonStackingBehaviour.TakeHighestValue:
                        float oldValue = Mathf.Abs(oldStatusEffect.Data.BaseValue);
                        float newValue = Mathf.Abs(statusEffectData.BaseValue);
                        if (newValue == oldValue)
                            goto case NonStackingBehaviour.TakeHighestDuration;
                        if (newValue < oldValue)
                            return null;
                        else
                            flagForRemoval = oldStatusEffect;
                        break;
                    case NonStackingBehaviour.TakeNewest:
                        flagForRemoval = oldStatusEffect;
                        break;
                    case NonStackingBehaviour.TakeOldest:
                        return null;
                }
            }

            CheckConditionals:
            // Check for conditions.
            bool preventStatusEffect = false;

            foreach (Condition condition in statusEffectData.Conditions)
            {
                bool exists = condition.SearchableConfigurable is ConditionalConfigurable.Group ? GetFirstStatusEffect(group: condition.SearchableGroup) != null
                            : condition.SearchableConfigurable is ConditionalConfigurable.Name  ? GetFirstStatusEffect(name: condition.SearchableComparableName) != null
                                                                                                : GetFirstStatusEffect(data: condition.SearchableData) != null;
                // If the condition is checking for existence and it doesn't exist or if
                // its checking non-existence and does exist then skip this condition.
                if ((condition.Exists && !exists)
                || (!condition.Exists && exists))
                    continue;

                if (condition.Add)
                    switch (condition.Timing)
                    {
                        case ConditionalTiming.Duration:
                            AddStatusEffect(condition.ActionData, condition.Duration, condition.Stacks * (condition.Scaled ? stacks : 1));
                            break;
                        case ConditionalTiming.Inherited:
                            switch (timing)
                            {
                                case StatusEffectTiming.Duration:
                                    AddStatusEffect(condition.ActionData, durationValue, condition.Stacks * (condition.Scaled ? stacks : 1));
                                    break;
                                case StatusEffectTiming.Event:
                                    AddStatusEffect(condition.ActionData, durationValue, unityEvent, interval.Value, condition.Stacks * (condition.Scaled ? stacks : 1));
                                    break;
                                case StatusEffectTiming.Predicate:
                                    AddStatusEffect(condition.ActionData, predicate, condition.Stacks * (condition.Scaled ? stacks : 1));
                                    break;
                                default:
                                    AddStatusEffect(condition.ActionData, condition.Stacks * (condition.Scaled ? stacks : 1));
                                    break;
                            }
                            break;
                        default:
                            AddStatusEffect(condition.ActionData, condition.Stacks * (condition.Scaled ? stacks : 1));
                            break;
                    }
                // Special case where the configurable which is the
                // current data to be added is tagged for removal.
                else if (condition.ActionData == statusEffectData)
                {
                    int? conditionalStacks = condition.UseStacks ? condition.Stacks * (condition.Scaled ? stacks : 1) : null;
                    if (!conditionalStacks.HasValue || conditionalStacks.Value >= stacks)
                    {
                        preventStatusEffect = true;
                        if (conditionalStacks.HasValue)
                            conditionalStacks -= stacks;
                        stacks = 0;
                        if (!conditionalStacks.HasValue || conditionalStacks.Value > 0)
                            RemoveBasedOnConfigurable(conditionalStacks);
                    }
                    else
                        stacks -= conditionalStacks.Value;
                }
                // Otherwise we remove effects.
                else
                    RemoveBasedOnConfigurable(condition.UseStacks ? condition.Stacks * (condition.Scaled ? stacks : 1) : null);

                void RemoveBasedOnConfigurable(int? stacks)
                {
                    switch (condition.ActionConfigurable)
                    {
                        case ConditionalConfigurable.Data:
                            RemoveStatusEffect(condition.ActionData, stacks);
                            break;
                        case ConditionalConfigurable.Name:
                            RemoveStatusEffect(condition.ActionComparableName, stacks);
                            break;
                        case ConditionalConfigurable.Group:
                            RemoveStatusEffect(condition.ActionGroup, stacks);
                            break;
                    }
                }
            }
            
            if (preventStatusEffect)
                return null;

            RemoveStatusEffect(flagForRemoval);
            // Add the status effect
            if (addedToStack)
            {
                statusEffect.Stacks += stacks;
                ValueUpdate?.Invoke(statusEffect);
                statusEffect.InvokeStackUpdate();
                action = StatusEffectAction.AddedStacks;
            }
            else
            {
                // Create a new status effect instance.
                statusEffect = new StatusEffect(statusEffectData, timing, durationValue, stacks);
                // Add the effect for a given monobehaviour. This also is the first time
                // initializing so we need to initialize all of the Status Variables
                if (m_Effects == null)
                {
                    m_Effects = new Dictionary<int, StatusEffect> { { statusEffect.GetInstanceID(), statusEffect } };
#if UNITY_EDITOR
                    m_EditorOnlyEffects = new List<StatusEffect> { statusEffect };
#endif
                }
                else
                {
                    m_Effects.Add(statusEffect.GetInstanceID(), statusEffect);
#if UNITY_EDITOR
                    m_EditorOnlyEffects.Add(statusEffect);
#endif
                }

                ValueUpdate?.Invoke(statusEffect);
            }
            // If a module exists it will be started.
            statusEffect.EnableModules(this);
            
            OnStatusEffect?.Invoke(statusEffect, action, stacks);
            // Return the effect in case it is wanted for other reference.
            return statusEffect;
        }
        #endregion
    }
}
