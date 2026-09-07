#if UNITASK
using Cysharp.Threading.Tasks;
using System.Threading;
#elif UNITY_2023_1_OR_NEWER
using System.Threading;
#else
using System.Collections;
#endif
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using System.Runtime.CompilerServices;
using System;

namespace StatusEffectsFramework
{
    /// <summary>
    /// A component that manages currently active Status Effects.
    /// </summary>
    [DisallowMultipleComponent]
    [AddComponentMenu("Status Effect/Status Manager")]
    public class StatusManager : MonoBehaviour, IStatusManager
    {
        event Action<StatusEffect, StatusEffectAction, int, int> IStatusManager.OnStatusEffect
        {
            add => OnStatusEffect += value;
            remove => OnStatusEffect -= value;
        }
        public event Action<StatusEffect, StatusEffectAction, int, int> OnStatusEffect;

        IEnumerable<StatusEffect> IStatusManager.StatusEffects => StatusEffects;
        public IEnumerable<StatusEffect> StatusEffects => m_StatusEffects?.Values ?? Enumerable.Empty<StatusEffect>();
        
        [SerializeField] private Dictionary<uint, StatusEffect> m_StatusEffects;
        
        internal uint AvailableId;

#if UNITY_EDITOR
        [SerializeField] private List<StatusEffect> m_EditorOnlyEffects;
#endif
        private void Awake()
        {
            AvailableId = 0;
            m_StatusEffects = new();
        }

        #region Public Methods
        public bool GetStatusEffect(uint id, out StatusEffect statusEffect)
        {
            return m_StatusEffects.TryGetValue(id, out statusEffect);
        }
        
#nullable enable
        public IEnumerable<StatusEffect> GetStatusEffects(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null, bool matchAllGroups = true)
#nullable disable
        {
            // Return the effects for a given monobehaviour, if given a group
            // or name to match only return effects within those categories.
            return StatusEffects.Where(se => (name  == null || se.Data.ComparableName  == name) 
                                   && (group == null || (matchAllGroups ? (se.Data.Group & group) == group : (se.Data.Group & group) != 0))
                                   && (data  == null || se.Data == data));
        }

#nullable enable
        public StatusEffect GetFirstStatusEffect(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null, bool matchAllGroups = true)
#nullable disable
        {
            // Return the effects for a given monobehaviour, if given a group
            // or name to match only return effects within those categories.
            return StatusEffects.FirstOrDefault(se => (name  == null || se.Data.ComparableName == name)
                                            && (group == null || (matchAllGroups ? (se.Data.Group & group) == group : (se.Data.Group & group) != 0))
                                            && (data  == null || se.Data == data));
        }

        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, int stacks = 1)
        {
            return AddStatusEffect(statusEffectData, StatusEffectTiming.Infinite, null, null, null, null, stacks);
        }

        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, int stacks = 1)
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");

            StatusEffect statusEffect = AddStatusEffect(statusEffectData, StatusEffectTiming.Duration, duration, null, null, null, stacks);

            if (statusEffect == null)
                return null;
            // Begin a timer on the monobehaviour.
            CreateTimer(statusEffect);

            return statusEffect;
        }
        
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
        
        public void RemoveStatusEffect(StatusEffect statusEffect)
        {
            if (statusEffect == null)
                return;
            // Stop the timer
#if UNITASK || UNITY_2023_1_OR_NEWER
            statusEffect.TimedTokenSource?.Cancel();
#else
            if (statusEffect.TimedCoroutine != null)
                StopCoroutine(statusEffect.TimedCoroutine);
#endif

            // Remove the effects for a given monobehaviour.
            m_StatusEffects.Remove(statusEffect.Id);
#if UNITY_EDITOR
            m_EditorOnlyEffects.Remove(statusEffect);
#endif

            // If a module exists it will be stopped.
            statusEffect.Stop(this);
            
            OnStatusEffect?.Invoke(statusEffect, StatusEffectAction.RemovedStatusEffect, statusEffect.Stacks, 0);
        }
        
#nullable enable
        public void RemoveStatusEffect(StatusEffectData statusEffectData, int? stacks = null)
#nullable disable
        {
            if (statusEffectData == null)
                return;

            if (stacks.HasValue && stacks.Value <= 0)
                return;
            
            IterateRemoval(OrderStatusEffects(m_StatusEffects.Values.Where(se => se.Data == statusEffectData)), stacks);
        }
        
        public void RemoveStatusEffect(ComparableName name, int? stacks = null)
        {
            if (stacks.HasValue && stacks.Value <= 0)
                return;
            
            IterateRemoval(OrderStatusEffects(m_StatusEffects.Values.Where(se => se.Data.ComparableName == name)), stacks);
        }
        
        public void RemoveStatusEffect(StatusEffectGroup group, int? stacks = null, bool matchAllGroups = true)
        {
            if (stacks.HasValue && stacks.Value <= 0)
                return;
            
            IterateRemoval(OrderStatusEffects(m_StatusEffects.Values.Where(se => matchAllGroups ? (se.Data.Group & group) == group : (se.Data.Group & group) != 0)), stacks);
        }
        
        public void RemoveAllStatusEffects()
        {
            // From the end of the list iterate through and remove all.
            for (int i = m_StatusEffects.Count - 1; i >= 0; i--)
                RemoveStatusEffect(m_StatusEffects.ElementAt(i).Value);
        }
        #endregion

        #region Private Methods
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private IEnumerable<StatusEffect> OrderStatusEffects(IEnumerable<StatusEffect> statusEffects)
        {
            return statusEffects.OrderBy(se => se.Data.BaseValue)
                                .ThenBy(se => se.Timing is StatusEffectTiming.Infinite or StatusEffectTiming.Predicate ? float.PositiveInfinity : se.Duration);
        }

        private void IterateRemoval(IEnumerable<StatusEffect> statusEffectsToRemove, int? stacks)
        {
            int removedCount = 0;
            int currentStacks;
            int previousStacks;

            foreach (var statusEffect in statusEffectsToRemove)
            {
                if (stacks.HasValue)
                {
                    previousStacks = statusEffect.Stacks;

                    if (removedCount + previousStacks > stacks)
                    {
                        if (removedCount == stacks)
                            break;

                        currentStacks = previousStacks - stacks.Value + removedCount;
                        statusEffect.SetStacks(currentStacks);
                        OnStatusEffect?.Invoke(statusEffect, StatusEffectAction.RemovedStacks, previousStacks, currentStacks);
                        statusEffect.InvokeStackUpdate(previousStacks, currentStacks);
                        break;
                    }
                    removedCount += previousStacks;
                }
                // If it got to this point we can remove the effect. Either
                // we removed all its stacks or there was no stack count.
                RemoveStatusEffect(statusEffect);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void CreateTimer(StatusEffect statusEffect)
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
                // Basic decreasing timer.
                while (statusEffect.TimeRemaining(Time.timeAsDouble) > 0
#if UNITASK || UNITY_2023_1_OR_NEWER
                   && !token.IsCancellationRequested
#endif
                   )
#if UNITASK
                    await UniTask.NextFrame(token);
#elif UNITY_2023_1_OR_NEWER
                    await Awaitable.NextFrameAsync(token);
#else
                    yield return null;
#endif
                // Once it has ended remove the given effect.
#if UNITASK || UNITY_2023_1_OR_NEWER
                if (!token.IsCancellationRequested)
#endif
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
                bool exists = condition.SearchableConfigurable is ConditionalConfigurable.AllGroups ? GetFirstStatusEffect(group: condition.SearchableGroup) != null
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
                else if (condition.ActionConfigurable is ConditionalConfigurable.Data && condition.ActionData == statusEffectData)
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
                        case ConditionalConfigurable.AllGroups:
                            RemoveStatusEffect(condition.ActionGroup, stacks, true);
                            break;
                        case ConditionalConfigurable.AnyGroups:
                            RemoveStatusEffect(condition.ActionGroup, stacks, false);
                            break;
                    }
                }
            }
            
            if (preventStatusEffect)
                return statusEffect;

            RemoveStatusEffect(flagForRemoval);

            if (stacks == 0)
                return null;

            int previousStacks = 0;
            int currentStacks = stacks;
            // Add the status effect
            if (addedToStack)
            {
                previousStacks = statusEffect.Stacks;
                currentStacks += statusEffect.Stacks;
                statusEffect.SetStacks(currentStacks);
                action = currentStacks > previousStacks ? StatusEffectAction.AddedStacks : StatusEffectAction.RemovedStacks;
                OnStatusEffect?.Invoke(statusEffect, action, previousStacks, currentStacks);
                statusEffect.InvokeStackUpdate(previousStacks, currentStacks);
            }
            else
            {
                // Create a new status effect instance.
                statusEffect = new StatusEffect(this, AvailableId, statusEffectData, timing, Time.timeAsDouble, durationValue, stacks);
                AvailableId++;
                // Add the effect for a given monobehaviour. This also is the first time
                // initializing so we need to initialize all of the Status Variables
                m_StatusEffects.Add(statusEffect.Id, statusEffect);
#if UNITY_EDITOR
                m_EditorOnlyEffects.Add(statusEffect);
#endif
                OnStatusEffect?.Invoke(statusEffect, action, previousStacks, currentStacks);
                // If a module exists it will be started.
                statusEffect.Start(this);
            }
            
            // Return the effect in case it is wanted for other reference.
            return statusEffect;
        }
        #endregion
    }
}
