using System.Collections.Generic;
using System.Linq;
using UnityEngine;
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
        event Action<StatusEffect, StatusEffectAction, int, int> IStatusManager.StatusEffectAction
        {
            add => StatusEffectAction += value;
            remove => StatusEffectAction -= value;
        }
        /// <inheritdoc/>
        public event Action<StatusEffect, StatusEffectAction, int, int> StatusEffectAction;

        IEnumerable<StatusEffect> IStatusManager.StatusEffects => StatusEffects;
        /// <inheritdoc/>
        public IEnumerable<StatusEffect> StatusEffects => m_StatusEffects?.Values ?? Enumerable.Empty<StatusEffect>();
        
        private Dictionary<uint, StatusEffect> m_StatusEffects;
        
        private uint m_AvailableId;

#if UNITY_EDITOR
        [SerializeField] private List<StatusEffect> m_EditorOnlyEffects;

#endif
        // Conditions can add status effects that have conditions of their own, so a cycle (such as an
        // effect that adds itself while it is absent) would never end. Adds stop at this depth.
        private const int k_MaxConditionDepth = 16;
        // Set in OnDestroy so modules and Stopped handlers can't add effects that would never be stopped.
        private bool m_Destroying;

        private void Awake()
        {
            m_AvailableId = 0;
            m_StatusEffects = new();
        }

        private void OnDestroy()
        {
            // Nothing else stops timers or disables modules when the manager goes away. Each effect is stopped on its
            // own so one that throws doesn't leave the rest running. Removal isn't reported, since the manager is gone.
            m_Destroying = true;

            if (m_StatusEffects == null)
                return;

            // A copy, since a module can remove other effects while it is disabled.
            foreach (var statusEffect in m_StatusEffects.Values.ToList())
            {
                try
                {
                    statusEffect.Stop(this);
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception, this);
                }
            }

            m_StatusEffects.Clear();
#if UNITY_EDITOR
            m_EditorOnlyEffects.Clear();
#endif
        }

        #region Public Methods
        /// <inheritdoc/>
        public bool TryGetStatusEffect(uint id, out StatusEffect statusEffect)
        {
            return m_StatusEffects.TryGetValue(id, out statusEffect);
        }
        
        /// <inheritdoc/>
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

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, int stacks = 1)
        {
            return AddStatusEffect(statusEffectData, null, null, stacks, 0);
        }

        /// <inheritdoc/>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, int stacks = 1)
        {
            return AddStatusEffect(statusEffectData, new DurationStatusTimer(), duration, stacks, 0);
        }

        /// <inheritdoc/>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, StatusEvent statusEvent, int stacks = 1)
        {
            // The timer throws for a null event before anything is added.
            return AddStatusEffect(statusEffectData, new EventStatusTimer(statusEvent), duration, stacks, 0);
        }

        /// <inheritdoc/>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, Func<bool> predicate, int stacks = 1)
        {
            // The timer throws for a null predicate before anything is added.
            return AddStatusEffect(statusEffectData, new PredicateStatusTimer(predicate), null, stacks, 0);
        }

        /// <summary>
        /// Adds a status effect that ends when <paramref name="timer"/> runs out, or never when it is null.
        /// Pass a new timer for each call.
        /// </summary>
        /// <param name="duration">Stored in <see cref="StatusEffect.Duration"/> for the timer to use. Leave it null when the timer has none.</param>
        /// <returns>The added or stacked status effect, or null if it wasn't added.</returns>
#nullable enable
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, StatusTimer? timer, float? duration = null, int stacks = 1)
#nullable disable
        {
            return AddStatusEffect(statusEffectData, timer, duration, stacks, 0);
        }

        /// <inheritdoc/>
        public void RemoveStatusEffect(StatusEffect statusEffect)
        {
            // A timer that outlived its manager can still end; there is nothing left to remove from.
            if (!this)
                return;
            // Already removed effects are ignored so removal is only reported once.
            if (statusEffect == null || !m_StatusEffects.TryGetValue(statusEffect.Id, out var existingEffect) || existingEffect != statusEffect)
                return;
            // Remove the effects for a given monobehaviour.
            m_StatusEffects.Remove(statusEffect.Id);
#if UNITY_EDITOR
            m_EditorOnlyEffects.Remove(statusEffect);
#endif

            // Reported before stopping so status variables have already dropped the effect when modules are
            // disabled and Stopped is raised, the reverse of adding, where they include it before modules are enabled.
            StatusEffectAction?.Invoke(statusEffect, StatusEffectsFramework.StatusEffectAction.RemovedStatusEffect, statusEffect.Stacks, 0);

            // If a module exists it will be stopped.
            statusEffect.Stop(this);
        }
        
        /// <inheritdoc/>
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
        
        /// <inheritdoc/>
        public void RemoveStatusEffect(ComparableName name, int? stacks = null)
        {
            if (name == null)
                return;

            if (stacks.HasValue && stacks.Value <= 0)
                return;
            
            IterateRemoval(OrderStatusEffects(m_StatusEffects.Values.Where(se => se.Data.ComparableName == name)), stacks);
        }
        
        /// <inheritdoc/>
        public void RemoveStatusEffect(StatusEffectGroup group, int? stacks = null, bool matchAllGroups = true)
        {
            if (stacks.HasValue && stacks.Value <= 0)
                return;
            
            IterateRemoval(OrderStatusEffects(m_StatusEffects.Values.Where(se => matchAllGroups ? (se.Data.Group & group) == group : (se.Data.Group & group) != 0)), stacks);
        }
        
        /// <inheritdoc/>
        public void RemoveAllStatusEffects()
        {
            // From the end of the list iterate through and remove all.
            for (int i = m_StatusEffects.Count - 1; i >= 0; i--)
                RemoveStatusEffect(m_StatusEffects.ElementAt(i).Value);
        }
        #endregion

        #region Internal Methods
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> with a given instance id without checking stacking rules or conditions.
        /// Used to mirror the effects of an authoritative manager, such as a server. Only the authority removes the
        /// effect. Returns the existing effect if one with that id is already present.
        /// </summary>
        /// <param name="timer">A timer that reports the authority's timing and time left without ending the effect,
        /// such as each netcode module's NetworkMirroredStatusTimer, or null for infinite effects.</param>
        internal StatusEffect ForceAddStatusEffect(uint instanceId, StatusEffectData statusEffectData, StatusTimer timer, float duration, int stacks)
        {
            if (m_Destroying || !statusEffectData || stacks <= 0)
                return null;

            if (m_StatusEffects.TryGetValue(instanceId, out var existingEffect))
                return existingEffect;

            var statusEffect = new StatusEffect(this, instanceId, statusEffectData, timer, duration, stacks);
            m_StatusEffects.Add(instanceId, statusEffect);
#if UNITY_EDITOR
            m_EditorOnlyEffects.Add(statusEffect);
#endif
            StatusEffectAction?.Invoke(statusEffect, StatusEffectsFramework.StatusEffectAction.AddedStatusEffect, 0, stacks);
            // Not listening to Ended: the authority decides when a mirrored effect ends.
            statusEffect.Start(this);

            return statusEffect;
        }

        /// <summary>
        /// Changes the stack count of a <see cref="StatusEffect"/> and raises the same notifications as a normal stack change.
        /// </summary>
        internal void SetStatusEffectStacks(StatusEffect statusEffect, int stacks)
        {
            int previousStacks = statusEffect.Stacks;

            if (previousStacks == stacks)
                return;

            statusEffect.SetStacks(stacks);
            StatusEffectAction?.Invoke(statusEffect, stacks > previousStacks ? StatusEffectsFramework.StatusEffectAction.AddedStacks : StatusEffectsFramework.StatusEffectAction.RemovedStacks, previousStacks, stacks);
            statusEffect.InvokeStackUpdate(previousStacks, stacks);
        }
        #endregion

        #region Private Methods
        // Copied from IndexedStatusEffectComparer. Orders weakest first so removals take them first.
        private List<StatusEffect> OrderStatusEffects(IEnumerable<StatusEffect> statusEffects)
        {
            var ordered = statusEffects.ToList();
            ordered.Sort((x, y) =>
            {
                // Compare timing rank.
                int comparison = TimingRank(x.Timing).CompareTo(TimingRank(y.Timing));
                if (comparison != 0)
                    return comparison;
                // Then compare base value.
                comparison = Mathf.Abs(x.Data.BaseValue).CompareTo(Mathf.Abs(y.Data.BaseValue));
                if (comparison != 0)
                    return comparison;
                // Then compare remaining time, when both timers count it the same way. Events counting down on
                // different events can't be compared and infinite and predicate effects have no meaningful time left.
                if (x.Timer != null && x.Timer.CanCompareTimeRemaining(y.Timer))
                {
                    comparison = x.TimeRemaining().CompareTo(y.TimeRemaining());
                    if (comparison != 0)
                        return comparison;
                }
                // Order oldest first so the newest is kept.
                return x.Id.CompareTo(y.Id);
            });
            return ordered;
        }

        /// <summary>
        /// Ranks how long an effect lasts regardless of its duration:
        /// Infinite > Predicate > Event > Duration.
        /// </summary>
        private static int TimingRank(StatusEffectTiming timing) => timing switch
        {
            StatusEffectTiming.Infinite => 3,
            StatusEffectTiming.Predicate => 2,
            StatusEffectTiming.Event => 1,
            _ => 0,
        };

        // Copied from StatusEffectRequestProcessor.ShouldReplace(). Whether an incoming non-stacking effect should
        // replace the existing one. The timing rank decides first, then base value, then time left.
        private static bool ShouldReplace(StatusTimer timer,
                                          float duration,
                                          StatusEffect oldStatusEffect,
                                          float baseValue,
                                          float oldBaseValue,
                                          float oldTimeRemaining)
        {
            StatusEffectTiming timing = timer?.Timing ?? StatusEffectTiming.Infinite;
            int rank = TimingRank(timing);
            int oldRank = TimingRank(oldStatusEffect.Timing);
            if (rank != oldRank)
                return rank > oldRank;

            bool comparable = timer != null && timer.CanCompareTimeRemaining(oldStatusEffect.Timer);
            // Events counting down on different events can't be compared, so the newest wins.
            if (timing is StatusEffectTiming.Event && !comparable)
                return true;
            if (baseValue != oldBaseValue)
                return baseValue > oldBaseValue;
            // Infinite and predicate effects have no meaningful time left, so the newest wins.
            if (!comparable)
                return true;
            // The newest wins ties.
            return duration >= oldTimeRemaining;
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
                        StatusEffectAction?.Invoke(statusEffect, StatusEffectsFramework.StatusEffectAction.RemovedStacks, previousStacks, currentStacks);
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

        /// <summary>
        /// Adds the status effect. Starting the effect starts its timer, which removes it when it runs out.
        /// </summary>
        /// <param name="timer">Decides when the effect ends, or null for an infinite effect.</param>
        /// <param name="depth">How many conditional adds deep this add is. 0 for a call from the public methods.</param>
#nullable enable
        private StatusEffect AddStatusEffect(StatusEffectData statusEffectData, StatusTimer? timer, float? duration, int stacks, int depth)
#nullable disable
        {
            if (depth >= k_MaxConditionDepth)
            {
                Debug.LogError($"Dropped adding {statusEffectData} because its conditions add other status effects more than {k_MaxConditionDepth} levels deep. Check the conditions for status effects that add each other.");
                return null;
            }

            return AddStatusEffectInternal(statusEffectData, timer, duration, stacks, depth);
        }

#nullable enable
        private StatusEffect AddStatusEffectInternal(StatusEffectData statusEffectData, StatusTimer? timer, float? duration, int stacks, int depth)
#nullable disable
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Please only add Status Effects in play mode!");
                return null;
            }
#endif
            if (m_Destroying || stacks <= 0)
                return null;

            if (!statusEffectData)
                throw new ArgumentNullException(nameof(statusEffectData), $"Attempted to add a null {nameof(StatusEffectData)} to a {nameof(StatusManager)}.");
            // Checked before adding, otherwise the timer would throw once the effect is already added.
            if (timer != null && timer.HasStarted)
                throw new ArgumentException($"This {timer.GetType().Name} was already started for a status effect. Create a new timer for each status effect.", nameof(timer));
            // If the duration given is less than zero it won't be applied.
            if (duration.HasValue && duration.Value < 0)
                return null;
            // First determine correct duration.
            float durationValue = duration.HasValue ? duration.Value : -1;
            StatusEffectTiming timing = timer?.Timing ?? StatusEffectTiming.Infinite;

            StatusEffectAction action = StatusEffectsFramework.StatusEffectAction.AddedStatusEffect;
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
                // False when stack count is already max, otherwise true.
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

                var comparableName = statusEffectData.ComparableName.Resolve();
                StatusEffect oldStatusEffect = GetFirstStatusEffect(name: comparableName, data: comparableName ? null : statusEffectData);
                
                if (oldStatusEffect == null)
                    goto CheckConditionals;

                StatusEffectData oldData = oldStatusEffect.Data;
                // Existing effects are compared by the time they have left, not the duration they were added with.
                float oldTimeRemaining = oldStatusEffect.TimeRemaining();

                switch (statusEffectData.NonStackingBehaviour)
                {
                    case NonStackingBehaviour.MatchHighestValue:
                        // Durations can only be combined when both timers count time the same
                        // way. Otherwise the timing rank decides which effect is kept.
                        if (statusEffectData.BaseValue == oldData.BaseValue
                            || timer == null
                            || !timer.CanCompareTimeRemaining(oldStatusEffect.Timer))
                            goto case NonStackingBehaviour.TakeHighestDuration;

                        // The new duration divides by base value, so neither can be 0.
                        if (statusEffectData.BaseValue == 0 || oldData.BaseValue == 0)
                        {
                            Debug.LogError($"Dropped adding {statusEffectData} because {nameof(NonStackingBehaviour.MatchHighestValue)} can't combine with a {nameof(StatusEffectData)} that has a base value of 0.");
                            return null;
                        }

                        float baseValue = Mathf.Abs(statusEffectData.BaseValue);
                        float oldBaseValue = Mathf.Abs(oldData.BaseValue);
                        // Find which effect is highest value.
                        StatusEffectData highestValueData = baseValue < oldBaseValue ? oldData : statusEffectData;
                        float highestValueDuration = baseValue < oldBaseValue ? oldTimeRemaining : durationValue;
                        StatusEffectData lowestValueData = baseValue < oldBaseValue ? statusEffectData : oldData;
                        float lowestValueDuration = baseValue < oldBaseValue ? durationValue : oldTimeRemaining;
                        // Calculate the new duration = d1 + d2 / (v1 / v2).
                        durationValue = highestValueDuration + lowestValueDuration / (Mathf.Abs(highestValueData.BaseValue) / Mathf.Abs(lowestValueData.BaseValue));
                        statusEffectData = highestValueData;
                        flagForRemoval = oldStatusEffect;
                        break;
                    case NonStackingBehaviour.TakeHighestDuration:
                        if (!ShouldReplace(timer, durationValue, oldStatusEffect, Mathf.Abs(statusEffectData.BaseValue), Mathf.Abs(oldData.BaseValue), oldTimeRemaining))
                            return null;
                        flagForRemoval = oldStatusEffect;
                        break;
                    case NonStackingBehaviour.TakeHighestValue:
                        float oldValue = Mathf.Abs(oldData.BaseValue);
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
                bool exists = condition.SearchableConfigurable switch
                {
                    ConditionalConfigurable.AllGroups => GetFirstStatusEffect(group: condition.SearchableGroup, matchAllGroups: true) != null,
                    ConditionalConfigurable.AnyGroups => GetFirstStatusEffect(group: condition.SearchableGroup, matchAllGroups: false) != null,
                    ConditionalConfigurable.Name => condition.SearchableComparableName.TryResolve(out var searchableComparableName)
                                                    && GetFirstStatusEffect(name: searchableComparableName) != null,
                    _ => condition.SearchableData.TryResolve(out var searchableData)
                         && GetFirstStatusEffect(data: searchableData) != null,
                };
                // If the condition is checking for existence and it doesn't exist or if
                // its checking non-existence and does exist then skip this condition.
                if ((condition.IsPresent && !exists)
                || (!condition.IsPresent && exists))
                    continue;

                if (condition.IsAdded)
                {
                    // No data to add was assigned, or it isn't registered.
                    if (!condition.ActionData.TryResolve(out var actionData))
                        continue;

                    int conditionStacks = condition.Stacks * (condition.IsScaled ? stacks : 1);

                    switch (condition.Timing)
                    {
                        case ConditionalTiming.Duration:
                            AddStatusEffect(actionData, new DurationStatusTimer(), condition.Duration, conditionStacks, depth + 1);
                            break;
                        case ConditionalTiming.Inherited:
                            // A new timer that ends the same way: the same event, the same predicate, or none.
                            // Infinite and predicate effects have no duration (-1), which would be dropped as negative.
                            AddStatusEffect(actionData, timer?.CreateInherited(), durationValue >= 0 ? durationValue : null, conditionStacks, depth + 1);
                            break;
                        default:
                            AddStatusEffect(actionData, null, null, conditionStacks, depth + 1);
                            break;
                    }
                }
                // Special case where the configurable which is the
                // current data to be added is tagged for removal.
                else if (condition.ActionConfigurable is ConditionalConfigurable.Data && condition.ActionData == statusEffectData)
                {
                    int? conditionalStacks = condition.UseStacks ? condition.Stacks * (condition.IsScaled ? stacks : 1) : null;
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
                    RemoveBasedOnConfigurable(condition.UseStacks ? condition.Stacks * (condition.IsScaled ? stacks : 1) : null);

                void RemoveBasedOnConfigurable(int? stacks)
                {
                    switch (condition.ActionConfigurable)
                    {
                        case ConditionalConfigurable.Data:
                            if (condition.ActionData.TryResolve(out var removedData))
                                RemoveStatusEffect(removedData, stacks);
                            break;
                        case ConditionalConfigurable.Name:
                            if (condition.ActionComparableName.TryResolve(out var removedComparableName))
                                RemoveStatusEffect(removedComparableName, stacks);
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
            
            // Merge into the existing infinite effect unless a condition removed it.
            if (addedToStack && !m_StatusEffects.ContainsKey(statusEffect.Id))
            {
                addedToStack = false;
                statusEffect = null;
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
                action = currentStacks > previousStacks ? StatusEffectsFramework.StatusEffectAction.AddedStacks : StatusEffectsFramework.StatusEffectAction.RemovedStacks;
                StatusEffectAction?.Invoke(statusEffect, action, previousStacks, currentStacks);
                statusEffect.InvokeStackUpdate(previousStacks, currentStacks);
            }
            else
            {
                // Create a new status effect instance.
                statusEffect = new StatusEffect(this, m_AvailableId, statusEffectData, timer, durationValue, stacks);
                m_AvailableId++;
                // Add the effect for a given monobehaviour. This also is the first time
                // initializing so we need to initialize all of the Status Variables
                m_StatusEffects.Add(statusEffect.Id, statusEffect);
#if UNITY_EDITOR
                m_EditorOnlyEffects.Add(statusEffect);
#endif
                StatusEffectAction?.Invoke(statusEffect, action, previousStacks, currentStacks);
                // Subscribed before starting, since a timer with nothing left ends while it starts.
                if (timer != null)
                {
                    var addedEffect = statusEffect;
                    timer.Ended += () => RemoveStatusEffect(addedEffect);
                }
                // Enables modules, then starts the timer.
                statusEffect.Start(this);
            }
            
            // Return the effect in case it is wanted for other reference.
            return statusEffect;
        }
        #endregion
    }
}
