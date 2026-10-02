using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Applies <see cref="StatusEffectRequests"/> directly to a <see cref="StatusEffects"/> buffer so
    /// every request sees the result of the ones before it. Removed effects are only marked with 0
    /// stacks so buffer indices stay valid, the caller is expected to compact the buffer afterwards.
    /// </summary>
    /// <remarks>
    /// Must be kept in a local variable and called on directly, since processing mutates
    /// <see cref="Manager"/>. Copy <see cref="Manager"/> back to the component once done.
    /// </remarks>
    internal struct StatusEffectRequestProcessor
    {
        public DynamicBuffer<StatusEffects> Buffer;
        public StatusManagerComponent Manager;
        public UnmanagedStatusRegistry Registry;
        /// <summary>
        /// Scratch list reused by every removal.
        /// </summary>
        public NativeList<IndexedStatusEffects> RemovalCandidates;
#if NETCODE
        public NetworkTick CurrentTick;
        public float CurrentTickFraction;
        public ClientServerTickRate TickRate;
#else
        public double ElapsedTime;
#endif

        public void Process(StatusEffectRequests request)
        {
            if (request.Type is StatusEffectRequestType.Add)
                Add(request);
            else
                Remove(request);
        }

        // Existing effects are compared by the time they have left, not the duration they were added with.
        private float TimeRemaining(in StatusEffects statusEffect)
        {
            return statusEffect.TimeRemaining(
#if NETCODE
                CurrentTick, CurrentTickFraction, TickRate
#else
                ElapsedTime
#endif
                );
        }

        // Copied from regular private StatusManager.AddStatusEffect() with burstable types and math.
        private void Add(StatusEffectRequests request)
        {
            if (request.Stacks <= 0)
                return;

            // If the duration given is less than zero it won't be applied.
            if (request.Timing is not StatusEffectTiming.Infinite && request.Duration < 0)
                return;

            ref var data = ref Registry.GetStatusEffectDataOrNullRefDebug(request.Id, out bool exists);
            if (!exists)
                return;

            // Declared here so refs from the old effect's data can be assigned back to data.
            bool oldDataExists;
            // Buffer indices of an effect being replaced and an infinite effect to merge into.
            int flagForRemoval = -1;
            int mergeIndex = -1;

            // If stacking is allowed then check for the max stacks and possible stack merging.
            if (data.AllowEffectStacking)
            {
                int stackCount = 0;
                // Go through all similar effects and count the stacks and if there is an
                // infinite effect.
                for (int x = 0; x < Buffer.Length; x++)
                {
                    var existingEffect = Buffer[x];
                    if (existingEffect.Stacks <= 0 || existingEffect.Id != data.Id)
                        continue;

                    if (existingEffect.Timing is StatusEffectTiming.Infinite)
                        mergeIndex = x;

                    stackCount += existingEffect.Stacks;
                }
                // Check if the current stacked amount is already max.
                if (data.MaxStacks >= 0)
                {
                    if (stackCount >= data.MaxStacks)
                        return;
                    // Otherwise cut the given stack if it would've been over the max.
                    if (stackCount + request.Stacks > data.MaxStacks)
                        request.Stacks = data.MaxStacks - stackCount;
                }
                // We can only safely merge infinite stacks because merging things with
                // duration or predicates we either can't or its too difficult to determine
                // their similarity.
                if (request.Timing is not StatusEffectTiming.Infinite)
                    mergeIndex = -1;
            }
            // Non-stackable: Check to delete the effect if it already exists to prevent duplicates.
            else
            {
                request.Stacks = 1;
                int oldStatusEffectIndex = -1;

                for (int x = 0; x < Buffer.Length; x++)
                {
                    var existingEffect = Buffer[x];
                    if (existingEffect.Stacks <= 0)
                        continue;

                    bool isSameEffect;
                    if (data.ComparableName != StatusRegistry.NullId)
                    {
                        ref var existingData = ref Registry.GetStatusEffectDataOrNullRefDebug(existingEffect.Id, out bool existingDataExists);
                        isSameEffect = existingDataExists && existingData.ComparableName == data.ComparableName;
                    }
                    else
                        isSameEffect = existingEffect.Id == data.Id;

                    if (isSameEffect)
                    {
                        oldStatusEffectIndex = x;
                        break;
                    }
                }

                if (oldStatusEffectIndex >= 0)
                {
                    var oldStatusEffect = Buffer[oldStatusEffectIndex];
                    // Always exists since it was matched above, but checked in case that ever changes.
                    ref var oldData = ref Registry.GetStatusEffectDataOrNullRefDebug(oldStatusEffect.Id, out oldDataExists);
                    if (!oldDataExists)
                        return;
                    float oldTimeRemaining = TimeRemaining(oldStatusEffect);

                    switch (data.NonStackingBehaviour)
                    {
                        case NonStackingBehaviour.MatchHighestValue:
                            // Durations can only be combined when both effects count down the same
                            // way. Otherwise the timing rank decides which effect is kept.
                            if (data.BaseValue == oldData.BaseValue
                                || request.Timing != oldStatusEffect.Timing
                                || request.Timing is StatusEffectTiming.Infinite or StatusEffectTiming.Predicate
                                || (request.Timing is StatusEffectTiming.Event && request.EventId != oldStatusEffect.EventId))
                                goto case NonStackingBehaviour.TakeHighestDuration;

                            // The new duration divides by base value, so neither can be 0.
                            if (data.BaseValue == 0 || oldData.BaseValue == 0)
                            {
                                UnityEngine.Debug.LogError($"Dropped a request to add the status effect with id \"{data.Id}\" because {nameof(NonStackingBehaviour.MatchHighestValue)} can't combine with a {nameof(StatusEffectData)} that has a base value of 0.");
                                return;
                            }

                            float baseValue = math.abs(data.BaseValue);
                            float oldBaseValue = math.abs(oldData.BaseValue);
                            // Find which effect is highest value.
                            ref var highestValueData = ref baseValue < oldBaseValue ? ref oldData : ref data;
                            float highestValueDuration = baseValue < oldBaseValue ? oldTimeRemaining : request.Duration;
                            ref var lowestValueData = ref baseValue < oldBaseValue ? ref data : ref oldData;
                            float lowestValueDuration = baseValue < oldBaseValue ? request.Duration : oldTimeRemaining;
                            // Calculate the new duration = d1 + d2 / (v1 / v2).
                            request.Duration = highestValueDuration + lowestValueDuration / (math.abs(highestValueData.BaseValue) / math.abs(lowestValueData.BaseValue));
                            data = ref highestValueData;
                            flagForRemoval = oldStatusEffectIndex;
                            break;
                        case NonStackingBehaviour.TakeHighestDuration:
                            if (!ShouldReplace(request, oldStatusEffect, math.abs(data.BaseValue), math.abs(oldData.BaseValue), oldTimeRemaining))
                                return;
                            flagForRemoval = oldStatusEffectIndex;
                            break;
                        case NonStackingBehaviour.TakeHighestValue:
                            float oldValue = math.abs(oldData.BaseValue);
                            float newValue = math.abs(data.BaseValue);
                            if (newValue == oldValue)
                                goto case NonStackingBehaviour.TakeHighestDuration;
                            if (newValue < oldValue)
                                return;
                            else
                                flagForRemoval = oldStatusEffectIndex;
                            break;
                        case NonStackingBehaviour.TakeNewest:
                            flagForRemoval = oldStatusEffectIndex;
                            break;
                        case NonStackingBehaviour.TakeOldest:
                            return;
                    }
                }
            }

            // Check for conditions.
            bool preventStatusEffect = false;

            for (int c = 0; c < data.Conditions.Length; c++)
            {
                UnmanagedCondition condition = data.Conditions[c];
                // If the condition is checking for existence and it doesn't exist or if
                // its checking non-existence and does exist then skip this condition.
                if (condition.Exists != ConditionTargetExists(Buffer, condition, Registry))
                    continue;

                int conditionalStacks = condition.Add || condition.UseStacks ? condition.Stacks * (condition.Scaled ? request.Stacks : 1) : -1;

                if (condition.Add)
                {
                    // No data to add was assigned.
                    if (condition.ActionData == StatusRegistry.NullId)
                        continue;

                    StatusEffectRequests conditionalRequest = condition.Timing switch
                    {
                        ConditionalTiming.Duration => StatusEffectRequests.AddWithDuration(condition.ActionData, condition.Duration, conditionalStacks),
                        ConditionalTiming.Inherited => new StatusEffectRequests
                        {
                            Type = StatusEffectRequestType.Add,
                            Id = condition.ActionData,
                            Timing = request.Timing,
                            Duration = request.Duration,
                            Interval = request.Interval,
                            Stacks = conditionalStacks,
                            EventId = request.EventId
                        },
                        _ => StatusEffectRequests.Add(condition.ActionData, conditionalStacks),
                    };
                    Add(conditionalRequest);
                }
                // Special case where the configurable which is the
                // current data to be added is tagged for removal.
                else if (condition.ActionConfigurable is ConditionalConfigurable.Data && condition.ActionData == data.Id)
                {
                    if (!condition.UseStacks || conditionalStacks >= request.Stacks)
                    {
                        preventStatusEffect = true;
                        if (condition.UseStacks)
                            conditionalStacks -= request.Stacks;
                        request.Stacks = 0;
                        // In the case where other status effects of this Id exist we need to request to remove them.
                        if ((!condition.UseStacks || conditionalStacks > 0) && TryConditionalRemoval(condition, conditionalStacks, out var removal))
                            Remove(removal);
                    }
                    else
                        request.Stacks -= conditionalStacks;
                }
                // Otherwise we remove effects.
                else if (TryConditionalRemoval(condition, conditionalStacks, out var conditionalRemoval))
                    Remove(conditionalRemoval);
            }

            if (preventStatusEffect)
                return;

            if (flagForRemoval >= 0)
                Buffer.ElementAt(flagForRemoval).Stacks = 0;

            // Merge into the existing infinite effect unless a condition removed it.
            if (mergeIndex >= 0 && Buffer[mergeIndex].Stacks > 0)
            {
                Buffer.ElementAt(mergeIndex).Stacks += request.Stacks;
                return;
            }

            // Add the status effect. Always append with the next AvailableId, the buffer must
            // stay sorted by InstanceId for InterpolatedStatusEffectEventsSystem.
            Buffer.Add(new StatusEffects()
            {
#if NETCODE
                TickAdded = CurrentTick,
                TickUpdated = CurrentTick,
#else
                TimeAdded = ElapsedTime,
#endif
                InstanceId = Manager.AvailableId++,
                Id = data.Id,
                Timing = request.Timing,
                Duration = request.Duration,
                Interval = request.Interval,
                Stacks = request.Stacks,
                EventId = request.EventId,
            });
        }

        private void Remove(in StatusEffectRequests request)
        {
            RemovalCandidates.Clear();

            for (int x = 0; x < Buffer.Length; x++)
            {
                var statusEffect = Buffer[x];
                if (statusEffect.Stacks <= 0 || !MatchesRemoval(statusEffect, request, Registry))
                    continue;

                // Effects without data can still be removed, they just sort as the weakest value.
                ref var candidateData = ref Registry.GetStatusEffectDataOrNullRefDebug(statusEffect.Id, out bool candidateDataExists);
                float baseValue = candidateDataExists ? math.abs(candidateData.BaseValue) : 0f;
                RemovalCandidates.Add(new IndexedStatusEffects(x, statusEffect, baseValue, TimeRemaining(statusEffect)));
            }
            // Sort by value and duration.
            RemovalCandidates.Sort(new IndexedStatusEffectComparer(false));
            // Remove until the requested stack count is reached.
            int removedCount = 0;
            for (int x = 0; x < RemovalCandidates.Length; x++)
            {
                if (request.Stacks >= 0 && removedCount >= request.Stacks)
                    break;

                ref var statusEffect = ref Buffer.ElementAt(RemovalCandidates[x].Index);
                int removing = request.Stacks >= 0 ? math.min(statusEffect.Stacks, request.Stacks - removedCount) : statusEffect.Stacks;
                statusEffect.Stacks -= removing;
                removedCount += removing;
            }
        }

        private static bool ConditionTargetExists(in DynamicBuffer<StatusEffects> buffer,
                                                  in UnmanagedCondition condition,
                                                  in UnmanagedStatusRegistry registry)
        {
            for (int x = 0; x < buffer.Length; x++)
            {
                var statusEffect = buffer[x];
                if (statusEffect.Stacks <= 0)
                    continue;

                // Only data searches can match without looking up the effect's data.
                if (condition.SearchableConfigurable is ConditionalConfigurable.Data)
                {
                    if (statusEffect.Id == condition.SearchableData)
                        return true;
                    continue;
                }

                ref var data = ref registry.GetStatusEffectDataOrNullRefDebug(statusEffect.Id, out bool exists);
                if (!exists)
                    continue;

                switch (condition.SearchableConfigurable)
                {
                    case ConditionalConfigurable.AllGroups:
                        if ((data.Group & condition.SearchableGroup) == condition.SearchableGroup)
                            return true;
                        break;
                    case ConditionalConfigurable.AnyGroups:
                        if ((data.Group & condition.SearchableGroup) != 0)
                            return true;
                        break;
                    case ConditionalConfigurable.Name:
                        if (condition.SearchableComparableName != StatusRegistry.NullId && data.ComparableName == condition.SearchableComparableName)
                            return true;
                        break;
                }
            }

            return false;
        }

        // Whether an incoming non-stacking effect should replace the existing one. The timing
        // rank decides first (Infinite > Predicate > Event > Duration), then base value, then time left.
        private static bool ShouldReplace(in StatusEffectRequests request,
                                          in StatusEffects oldStatusEffect,
                                          float baseValue,
                                          float oldBaseValue,
                                          float oldTimeRemaining)
        {
            int rank = IndexedStatusEffectComparer.TimingRank(request.Timing);
            int oldRank = IndexedStatusEffectComparer.TimingRank(oldStatusEffect.Timing);
            if (rank != oldRank)
                return rank > oldRank;
            // Events counting down on different ids can't be compared, so the newest wins.
            if (request.Timing is StatusEffectTiming.Event && request.EventId != oldStatusEffect.EventId)
                return true;
            if (baseValue != oldBaseValue)
                return baseValue > oldBaseValue;
            // Infinite and predicate effects have no meaningful time left, so the newest wins.
            if (request.Timing is StatusEffectTiming.Infinite or StatusEffectTiming.Predicate)
                return true;
            // The newest wins ties.
            return request.Duration >= oldTimeRemaining;
        }

        private static bool MatchesRemoval(in StatusEffects statusEffect,
                                           in StatusEffectRequests request,
                                           in UnmanagedStatusRegistry registry)
        {
            switch (request.RemovalType)
            {
                case StatusEffectRemovalType.InstanceId:
                    return statusEffect.InstanceId == request.InstanceId;
                case StatusEffectRemovalType.Id:
                    return statusEffect.Id == request.Id;
                case StatusEffectRemovalType.Any:
                    return true;
                case StatusEffectRemovalType.ComparableName:
                case StatusEffectRemovalType.AnyGroups:
                case StatusEffectRemovalType.AllGroups:
                    break;
                default:
                    return false;
            }

            // The remaining removal types need the effect's data.
            ref var data = ref registry.GetStatusEffectDataOrNullRefDebug(statusEffect.Id, out bool exists);
            if (!exists)
                return false;

            return request.RemovalType switch
            {
                StatusEffectRemovalType.ComparableName => data.ComparableName != StatusRegistry.NullId && data.ComparableName == request.Id,
                StatusEffectRemovalType.AnyGroups => (data.Group & request.Group) != 0,
                _ => (data.Group & request.Group) == request.Group,
            };
        }

        private static bool TryConditionalRemoval(in UnmanagedCondition condition, int stacks, out StatusEffectRequests request)
        {
            request = default;

            // Data and name removals with nothing assigned can't match anything.
            switch (condition.ActionConfigurable)
            {
                case ConditionalConfigurable.Data:
                    if (condition.ActionData == StatusRegistry.NullId)
                        return false;
                    request = StatusEffectRequests.RemoveWithId(condition.ActionData, stacks);
                    return true;
                case ConditionalConfigurable.Name:
                    if (condition.ActionComparableName == StatusRegistry.NullId)
                        return false;
                    request = StatusEffectRequests.RemoveWithComparableName(condition.ActionComparableName, stacks);
                    return true;
                case ConditionalConfigurable.AllGroups:
                    request = StatusEffectRequests.RemoveWithGroup(condition.ActionGroup, stacks, true);
                    return true;
                case ConditionalConfigurable.AnyGroups:
                    request = StatusEffectRequests.RemoveWithGroup(condition.ActionGroup, stacks, false);
                    return true;
                default:
                    UnityEngine.Debug.LogError($"Dropped a conditional removal because {nameof(ConditionalConfigurable)} has an invalid value of {(int)condition.ActionConfigurable}.");
                    return false;
            }
        }
    }
}
