#if ENTITIES
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
#if NETCODE
using Unity.NetCode;
#endif

namespace StatusEffects.Entities
{
    /// <summary>
    /// Custom systems that decrement <see cref="StatusEffectTiming.Event"/> 
    /// and <see cref="StatusEffectTiming.Predicate"/> <see cref="StatusEffects"/> 
    /// buffers should update in the <see cref="StatusEffectSystemGroup"/>. 
    /// Any module related systems should most likely run in the 
    /// <see cref="SimulationSystemGroup"/>.
    /// </summary>
    [UpdateInGroup(typeof(StatusEffectSystemGroup), OrderLast = true)]
    [BurstCompile]
    public partial struct StatusManagerSystem : ISystem
    {
        [BurstCompile]
        public void OnCreate(ref SystemState state)
        {
            state.RequireForUpdate<StatusEffects>();
            state.RequireForUpdate<StatusReferences>();
            state.RequireForUpdate<ModulePrefabs>();
        }
        
        [BurstCompile]
        public void OnUpdate(ref SystemState state)
        {
            state.CompleteDependency(); // Modules may have been written to.

            var moduleLookup = SystemAPI.GetComponentLookup<Module>();

            var references = SystemAPI.GetSingleton<StatusReferences>();
            var modulePrefabs = SystemAPI.GetSingletonBuffer<ModulePrefabs>();
            var commandBuffer = new EntityCommandBuffer(Allocator.TempJob);
            var commandBufferParallel = commandBuffer.AsParallelWriter();

            // Handle any add/remove requests.
            new StatusEffectRequestJob
            {
                CommandBuffer = commandBufferParallel,
                ModuleLookup = moduleLookup,
                References = references,
                ModulePrefabs = modulePrefabs
            }.ScheduleParallel();

            // Update and check durations.
            new StatusEffectUpdateJob
            {
                CommandBuffer = commandBufferParallel,
                ModuleLookup = moduleLookup,
                TimeDelta = SystemAPI.Time.DeltaTime
            }.ScheduleParallel();

            state.CompleteDependency();

            commandBuffer.Playback(state.EntityManager);
            commandBuffer.Dispose();
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct StatusEffectRequestJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [ReadOnly]
            public ComponentLookup<Module> ModuleLookup;
            [ReadOnly]
            public StatusReferences References;
            [ReadOnly]
            public DynamicBuffer<ModulePrefabs> ModulePrefabs;

            void Execute([EntityIndexInQuery] int sortKey, Entity entity, ref DynamicBuffer<StatusEffectRequests> statusEffectRequests, ref DynamicBuffer<StatusEffects> statusEffects, in DynamicBuffer<Modules> modules)
            {
                // If nothing to change then continue.
                if (statusEffectRequests.Length <= 0)
                    return;
                
                NativeArray<StatusEffects>.ReadOnly unsortedStatusEffects = statusEffects.AsNativeArray().AsReadOnly();
                NativeList<IndexedStatusEffect> statusEffectStackUpdates = new NativeList<IndexedStatusEffect>(unsortedStatusEffects.Length, Allocator.Temp);

                for (int i = statusEffectRequests.Length - 1; i >= 0; i--)
                {
                    EvaluateRequest(ref statusEffects, statusEffectRequests.ElementAt(i), References);

                    void EvaluateRequest(ref DynamicBuffer<StatusEffects> statusEffectBuffer,
                                         StatusEffectRequests request, 
                                         in StatusReferences references)
                    {
                        // Copied from regular private StatusManager.AddStatusEffect() with burstable types and math.
                        if (request.Type is StatusEffectRequestType.Add)
                        {
                            if (request.Stacks <= 0)
                                return;
                            
                            if (!references.BlobAsset.Value.TryGetValue(request.Id, out var reference))
                                return;
                            
                            // If the duration given is less than zero it won't be applied.
                            if (request.Timing is not StatusEffectTiming.Infinite && request.Duration < 0)
                                return;
                            // Declare here to use later.
                            ref StatusEffectData statusEffectData = ref reference.Value;
                            IndexedStatusEffect flagForRemoval = new IndexedStatusEffect(-1, default);
                            IndexedStatusEffect indexedStatusEffect = new IndexedStatusEffect()
                            {
                                Index = -1,
                                HasModule = statusEffectData.ModulePrefabIndex >= 0,
                                Id = statusEffectData.Id,
                                Timing = request.Timing,
                                Interval = request.Interval,
                                EventId = request.EventId
                            };
                            // If stacking is allowed then check for the max stacks and possible stack merging.
                            if (statusEffectData.AllowEffectStacking)
                            {
                                if (request.Timing is not StatusEffectTiming.Infinite)
                                    goto CheckConditionals;

                                NativeList<IndexedStatusEffect>  sortedStatusEffects = new NativeList<IndexedStatusEffect>(unsortedStatusEffects.Length, Allocator.Temp);
                                int stackCount = 0;

                                for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                    if (unsortedStatusEffects[x].Id == request.Id)
                                        sortedStatusEffects.Add(new IndexedStatusEffect(x, unsortedStatusEffects[x]));

                                if (sortedStatusEffects.Length <= 0)
                                {
                                    sortedStatusEffects.Dispose();
                                    if (!TryToLimitStacks(ref request.Stacks, statusEffectData.MaxStacks))
                                        return;
                                    goto CheckConditionals;
                                }

                                int infiniteEffectIndex = -1;
                                // Go through all similar effects and count the stacks and if there is an
                                // infinite effect.
                                foreach (var existentEffect in sortedStatusEffects)
                                {
                                    if (existentEffect.Timing is StatusEffectTiming.Infinite)
                                        infiniteEffectIndex = existentEffect.Index;

                                    stackCount += existentEffect.Stacks;
                                }
                                // Iterate again for currently updating ones.
                                foreach (var updatingEffect in statusEffectStackUpdates)
                                {
                                    if (updatingEffect.Id != statusEffectData.Id)
                                        continue;

                                    stackCount += updatingEffect.Stacks;
                                }

                                if (!TryToLimitStacks(ref request.Stacks, statusEffectData.MaxStacks))
                                {
                                    sortedStatusEffects.Dispose();
                                    return;
                                }
                                
                                // We can only safely merge infinite stacks because merging things with
                                // duration or predicates we either can't or its too difficult to determine
                                // their similarity.
                                if (request.Timing is StatusEffectTiming.Infinite && infiniteEffectIndex >= 0)
                                {
                                    // Setup dummy update so that we can add normally later.
                                    indexedStatusEffect.Index = infiniteEffectIndex;
                                    sortedStatusEffects.Dispose();
                                    goto CheckConditionals;
                                }
                                // False when stack count is already max, otherwise true.
                                bool TryToLimitStacks(ref int stacks, int maxStacks)
                                {
                                    // Check if the current stacked amount is already max.
                                    if (maxStacks >= 0)
                                        if (stackCount >= maxStacks)
                                            return false;
                                        // Otherwise cut the given stack if it would've been over the max.
                                        else if (stackCount + stacks > maxStacks)
                                            stacks = maxStacks - stackCount;

                                    return true;
                                }

                                sortedStatusEffects.Dispose();
                            }
                            // Non-stackable: Check to delete the effect if it already exists to prevent duplicates.
                            else
                            {
                                request.Stacks = 1;
                                int oldStatusEffectIndex = -1;

                                if (statusEffectData.ComparableName != default)
                                {
                                    for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                    {
                                        if (!references.BlobAsset.Value.TryGetValue(unsortedStatusEffects[x].Id, out var unsortedReferences))
                                            continue;
                                        ref StatusEffectData unsortedData = ref unsortedReferences.Value;
                                        if (unsortedData.ComparableName == statusEffectData.ComparableName)
                                        {
                                            oldStatusEffectIndex = x;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    for (int y = 0; y < unsortedStatusEffects.Length; y++)
                                    {
                                        if (unsortedStatusEffects[y].Id == statusEffectData.Id)
                                        {
                                            oldStatusEffectIndex = y;
                                            break;
                                        }
                                    }
                                }

                                if (oldStatusEffectIndex < 0)
                                    goto CheckConditionals;

                                IndexedStatusEffect oldStatusEffect = new IndexedStatusEffect(oldStatusEffectIndex, statusEffectBuffer.ElementAt(oldStatusEffectIndex));
                                var oldReference = references.BlobAsset.Value[oldStatusEffect.Id];

                                switch (statusEffectData.NonStackingBehaviour)
                                {
                                    case NonStackingBehaviour.MatchHighestValue:
                                        if (statusEffectData.BaseValue == oldReference.Value.BaseValue)
                                            goto case NonStackingBehaviour.TakeHighestDuration;

                                        float baseValue = math.abs(statusEffectData.BaseValue);
                                        float oldBaseValue = math.abs(statusEffectData.BaseValue);
                                        // WARNING: There is an extremely special case here where
                                        // a player may either have or try to apply an effect which
                                        // has an infinite duration (-1). In this situation, attempt
                                        // to take the higest value, and if they are the same take
                                        // the infinite duration effect.
                                        if (request.Timing is StatusEffectTiming.Infinite || oldStatusEffect.Timing is StatusEffectTiming.Infinite)
                                        {
                                            if (baseValue < oldReference.Value.BaseValue)
                                                return;
                                            else if (baseValue > oldReference.Value.BaseValue || oldStatusEffect.Timing is not StatusEffectTiming.Infinite)
                                            {
                                                flagForRemoval = oldStatusEffect;
                                                break;
                                            }
                                            else
                                                return;
                                        }
                                        // Find which effect is highest value.
                                        var highestValueReference = baseValue < oldBaseValue ? oldReference : reference;
                                        ref var highestValueData = ref highestValueReference.Value;
                                        float highestValueDuration = baseValue < oldBaseValue ? oldStatusEffect.Duration : request.Duration;
                                        var lowestValueReference = baseValue < oldBaseValue ? reference : oldReference;
                                        ref var lowestValueData = ref lowestValueReference.Value;
                                        float lowestValueDuration = baseValue < oldBaseValue ? request.Duration : oldStatusEffect.Duration;
                                        // Calculate the new duration = d1 + d2 / (v1 / v2). Note this assumes neither base value will ever be 0.
                                        if (highestValueData.BaseValue == 0 || lowestValueData.BaseValue == 0)
                                            throw new ArgumentException("A StatusEffectData has a base value of 0! This will cause an error!");

                                        request.Duration = highestValueDuration + lowestValueDuration / (math.abs(highestValueData.BaseValue) / math.abs(lowestValueData.BaseValue));
                                        reference = highestValueReference;
                                        statusEffectData = highestValueData;
                                        flagForRemoval = oldStatusEffect;
                                        break;
                                    case NonStackingBehaviour.TakeHighestDuration:
                                        if (oldStatusEffect.Timing is StatusEffectTiming.Infinite || (request.Duration < oldStatusEffect.Duration && request.Timing is not StatusEffectTiming.Infinite))
                                            return;
                                        else
                                            flagForRemoval = oldStatusEffect;
                                        break;
                                    case NonStackingBehaviour.TakeHighestValue:
                                        float oldValue = math.abs(oldReference.Value.BaseValue);
                                        float newValue = math.abs(statusEffectData.BaseValue);
                                        if (newValue == oldValue)
                                            goto case NonStackingBehaviour.TakeHighestDuration;
                                        if (newValue < oldValue)
                                            return;
                                        else
                                            flagForRemoval = oldStatusEffect;
                                        break;
                                    case NonStackingBehaviour.TakeNewest:
                                        flagForRemoval = oldStatusEffect;
                                        break;
                                    case NonStackingBehaviour.TakeOldest:
                                        return;
                                }
                            }

                            CheckConditionals:
                            // Check for conditions.
                            bool preventStatusEffect = false;

                            for (int c = 0; c < statusEffectData.Conditions.Length; c++)
                            {
                                Condition condition = statusEffectData.Conditions[c];
                                bool exists = false;

                                switch (condition.SearchableConfigurable)
                                {
                                    case ConditionalConfigurable.Group:
                                        for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                        {
                                            if (!references.BlobAsset.Value.TryGetValue(unsortedStatusEffects[x].Id, out var unsortedReference))
                                                continue;
                                            ref StatusEffectData unsortedData = ref unsortedReference.Value;
                                            if ((unsortedData.Group & condition.SearchableGroup) != 0)
                                            {
                                                int alreadyUpdatingIndex = statusEffectStackUpdates.IndexOf(x);
                                                if (alreadyUpdatingIndex >= 0)
                                                {
                                                    if (unsortedStatusEffects[x].Stacks + statusEffectStackUpdates[alreadyUpdatingIndex].Stacks > 0)
                                                        exists = true;
                                                }
                                                else
                                                    exists = true;
                                                break;
                                            }
                                        }
                                        if (!exists)
                                            foreach (var indexedUpdate in statusEffectStackUpdates)
                                            {
                                                if (indexedUpdate.Stacks <= 0)
                                                    continue;

                                                if (references.BlobAsset.Value.TryGetValue(indexedUpdate.Id, out var indexedReference) 
                                                && (indexedReference.Value.Group & condition.SearchableGroup) != 0)
                                                    exists = true;
                                            }
                                        break;
                                    case ConditionalConfigurable.Name:
                                        for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                        {
                                            if (!references.BlobAsset.Value.TryGetValue(unsortedStatusEffects[x].Id, out var unsortedReference))
                                                continue;
                                            ref StatusEffectData unsortedData = ref unsortedReference.Value;
                                            if (unsortedData.ComparableName == condition.SearchableComparableName)
                                            {
                                                int alreadyUpdatingIndex = statusEffectStackUpdates.IndexOf(x);
                                                if (alreadyUpdatingIndex >= 0)
                                                {
                                                    if (unsortedStatusEffects[x].Stacks + statusEffectStackUpdates[alreadyUpdatingIndex].Stacks > 0)
                                                        exists = true;
                                                }
                                                else
                                                    exists = true;
                                                break;
                                            }
                                        }
                                        if (!exists)
                                            foreach (var indexedUpdate in statusEffectStackUpdates)
                                            {
                                                if (indexedUpdate.Stacks <= 0)
                                                    continue;

                                                if (references.BlobAsset.Value.TryGetValue(indexedUpdate.Id, out var indexedReference)
                                                && (indexedReference.Value.ComparableName == condition.SearchableComparableName))
                                                    exists = true;
                                            }
                                        break;
                                    case ConditionalConfigurable.Data:
                                        for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                        {
                                            if (unsortedStatusEffects[x].Id == condition.SearchableData)
                                            {
                                                int alreadyUpdatingIndex = statusEffectStackUpdates.IndexOf(x);
                                                if (alreadyUpdatingIndex >= 0)
                                                {
                                                    if (unsortedStatusEffects[x].Stacks + statusEffectStackUpdates[alreadyUpdatingIndex].Stacks > 0)
                                                        exists = true;
                                                }
                                                else
                                                    exists = true;
                                                break;
                                            }
                                        }
                                        if (!exists)
                                            foreach (var indexedUpdate in statusEffectStackUpdates)
                                            {
                                                if (indexedUpdate.Stacks <= 0)
                                                    continue;

                                                if (indexedUpdate.Id == condition.SearchableData)
                                                    exists = true;
                                            }
                                        break;
                                }
                                // If the condition is checking for existence and it doesn't exist or if
                                // its checking non-existence and does exist then skip this condition.
                                if ((condition.Exists && !exists)
                                || (!condition.Exists && exists))
                                    continue;

                                StatusEffectRequests conditionalRequest;

                                if (condition.Add)
                                {
                                    switch (condition.Timing)
                                    {
                                        case ConditionalTiming.Duration:
                                            conditionalRequest = new StatusEffectRequests
                                            {
                                                Type = StatusEffectRequestType.Add,
                                                Id = condition.ActionData,
                                                Timing = StatusEffectTiming.Duration,
                                                Duration = condition.Duration,
                                                Stacks = condition.Stacks * (condition.Scaled ? request.Stacks : 1),
                                            };
                                            break;
                                        case ConditionalTiming.Inherited:
                                            conditionalRequest = new StatusEffectRequests
                                            {
                                                Type = StatusEffectRequestType.Add,
                                                Id = condition.ActionData,
                                                Timing = request.Timing,
                                                Duration = request.Duration,
                                                Interval = request.Interval,
                                                Stacks = condition.Stacks * (condition.Scaled ? request.Stacks : 1),
                                                EventId = request.EventId
                                            };
                                            break;
                                        default:
                                            conditionalRequest = new StatusEffectRequests
                                            {
                                                Type = StatusEffectRequestType.Add,
                                                Id = condition.ActionData,
                                                Timing = StatusEffectTiming.Infinite,
                                                Duration = -1,
                                                Stacks = condition.Stacks * (condition.Scaled ? request.Stacks : 1),
                                            };
                                            break;
                                    }
                                    EvaluateRequest(ref statusEffectBuffer, conditionalRequest, references);
                                }
                                // Special case where the configurable which is the
                                // current data to be added is tagged for removal.
                                else if (condition.ActionData == statusEffectData.Id)
                                {
                                    int conditionalStacks = condition.Stacks * (condition.Scaled ? request.Stacks : 1);
                                    if (!condition.UseStacks || conditionalStacks >= request.Stacks)
                                    {
                                        preventStatusEffect = true;
                                        if (condition.UseStacks)
                                            conditionalStacks -= request.Stacks;
                                        request.Stacks = 0;
                                        conditionalRequest = new StatusEffectRequests
                                        {
                                            Type = condition.UseStacks ? StatusEffectRequestType.Remove : StatusEffectRequestType.RemoveAll,
                                            RemovalType = (StatusEffectRemovalType)condition.ActionConfigurable,
                                            Group = condition.ActionGroup,
                                            Id = condition.ActionConfigurable is ConditionalConfigurable.Name ? condition.ActionComparableName : condition.ActionData,
                                            Stacks = conditionalStacks,
                                        };
                                        EvaluateRequest(ref statusEffectBuffer, conditionalRequest, references);
                                    }
                                    else
                                        request.Stacks -= conditionalStacks;
                                }
                                // Otherwise we remove effects.
                                else
                                {
                                    conditionalRequest = new StatusEffectRequests
                                    {
                                        Type = condition.UseStacks ? StatusEffectRequestType.Remove : StatusEffectRequestType.RemoveAll,
                                        RemovalType = (StatusEffectRemovalType)condition.ActionConfigurable,
                                        Group = condition.ActionGroup,
                                        Id = condition.ActionConfigurable is ConditionalConfigurable.Name ? condition.ActionComparableName : condition.ActionData,
                                        Stacks = condition.Stacks * (condition.Scaled ? request.Stacks : 1)
                                    };
                                    EvaluateRequest(ref statusEffectBuffer, conditionalRequest, references);
                                }
                            }

                            if (preventStatusEffect)
                                return;

                            if (flagForRemoval.Index >= 0)
                                RemoveStatusEffect(flagForRemoval, removeAll: true);

                            // Add the status effect
                            indexedStatusEffect.Duration = request.Duration;
                            AddStatusEffect(indexedStatusEffect, request.Stacks);
                        }
                        // If we aren't adding we are removing.
                        else
                        {
                            NativeList<IndexedStatusEffect> sortedStatusEffects = new NativeList<IndexedStatusEffect>(unsortedStatusEffects.Length, Allocator.Temp);

                            switch (request.RemovalType)
                            {
                                case StatusEffectRemovalType.Group:
                                    for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                    {
                                        StatusEffects statusEffect = unsortedStatusEffects[x];
                                        ref StatusEffectData statusEffectData = ref references.BlobAsset.Value[statusEffect.Id].Value;
                                        if ((statusEffectData.Group & request.Group) != 0)
                                            sortedStatusEffects.Add(new IndexedStatusEffect(x, statusEffect));
                                    }
                                    break;
                                case StatusEffectRemovalType.Data:
                                    for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                    {
                                        StatusEffects statusEffect = unsortedStatusEffects[x];
                                        if (statusEffect.Id == request.Id)
                                            sortedStatusEffects.Add(new IndexedStatusEffect(x, statusEffect));
                                    }
                                    break;
                                case StatusEffectRemovalType.Name:
                                    for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                    {
                                        StatusEffects statusEffect = unsortedStatusEffects[x];
                                        ref StatusEffectData statusEffectData = ref references.BlobAsset.Value[statusEffect.Id].Value;
                                        if (statusEffectData.ComparableName == request.Id)
                                            sortedStatusEffects.Add(new IndexedStatusEffect(x, statusEffect));
                                    }
                                    break;
                                case StatusEffectRemovalType.Any:
                                    for (int x = 0; x < unsortedStatusEffects.Length; x++)
                                        sortedStatusEffects.Add(new IndexedStatusEffect(x, unsortedStatusEffects[x]));
                                    break;
                            }
                            // Sort by value and duration.
                            sortedStatusEffects.Sort(new IndexedStatusEffectComparer(references, false));
                            // Remove until max stack limit reached.
                            int removedCount = 0;
                            bool removeAll = request.Type == StatusEffectRequestType.RemoveAll;
                            for (int x = 0; x < sortedStatusEffects.Length; x++)
                            {
                                IndexedStatusEffect indexedStatusEffect = sortedStatusEffects.ElementAt(x);
                                removedCount += RemoveStatusEffect(indexedStatusEffect, request.Stacks - removedCount, removeAll);
                            }

                            sortedStatusEffects.Dispose();
                        }
                    }
                    
                    void AddStatusEffect(IndexedStatusEffect indexedStatusEffect, int stacks = 1)
                    {
                        // Check to see if we are updating the index already.
                        int alreadyUpdatingIndex = statusEffectStackUpdates.IndexOf(indexedStatusEffect.Index);
                        if (alreadyUpdatingIndex >= 0)
                        {
                            ref IndexedStatusEffect indexedUpdate = ref statusEffectStackUpdates.ElementAt(alreadyUpdatingIndex);
                            indexedUpdate.Stacks += stacks;
                            return;
                        }
                        // Check to see if we are adding an infinite effect already.
                        if (indexedStatusEffect.Timing is StatusEffectTiming.Infinite)
                        {
                            for (int x = 0; x < statusEffectStackUpdates.Length; x++)
                            {
                                ref IndexedStatusEffect indexedUpdate = ref statusEffectStackUpdates.ElementAt(x);
                                if (indexedUpdate.Timing is StatusEffectTiming.Infinite && indexedUpdate.Id == indexedStatusEffect.Id)
                                {
                                    indexedUpdate.Stacks += stacks;
                                    return;
                                }
                            }
                        }
                        // Otherwise create a new stack.
                        indexedStatusEffect.Stacks = stacks;
                        statusEffectStackUpdates.Add(indexedStatusEffect);
                    }

                    // Returns the amount removed
                    int RemoveStatusEffect(IndexedStatusEffect indexedStatusEffect, int stacks = 1, bool removeAll = false)
                    {
                        int alreadyUpdatingIndex = statusEffectStackUpdates.IndexOf(indexedStatusEffect.Index);
                        if (alreadyUpdatingIndex >= 0)
                        {
                            ref IndexedStatusEffect indexedUpdate = ref statusEffectStackUpdates.ElementAt(alreadyUpdatingIndex);
                            if (!removeAll)
                            {
                                int currentStackCount = indexedStatusEffect.Stacks + indexedUpdate.Stacks;

                                if (currentStackCount > stacks)
                                {
                                    indexedUpdate.Stacks -= stacks;
                                    return stacks;
                                }
                            }
                            // If it got to this point we can remove the effect. Either
                            // we removed all its stacks or there was no stack count.
                            indexedUpdate.Stacks = -indexedStatusEffect.Stacks;
                        }
                        else
                        {
                            if (!removeAll)
                            {
                                if (indexedStatusEffect.Stacks > stacks)
                                {
                                    indexedStatusEffect.Stacks = -stacks;
                                    statusEffectStackUpdates.Add(indexedStatusEffect);
                                    return stacks;
                                }
                            }
                            // If it got to this point we can remove the effect. Either
                            // we removed all its stacks or there was no stack count.
                            indexedStatusEffect.Stacks = -indexedStatusEffect.Stacks;
                            statusEffectStackUpdates.Add(indexedStatusEffect);
                        }
                        return indexedStatusEffect.Stacks;
                    }

                    statusEffectRequests.RemoveAt(i);
                }
                // Remove and add status effects from buffer.
                statusEffectStackUpdates.Sort(new IndexedStatusEffectComparer(References, true));
                // Iterate in reverse to not skip any that get removed.
                for (int v = statusEffectStackUpdates.Length - 1; v >= 0; v--)
                {
                    IndexedStatusEffect indexedUpdate = statusEffectStackUpdates[v];
                    
                    if (indexedUpdate.Stacks == 0)
                        continue;
                    
                    // Check if we add a new status effect.
                    if (indexedUpdate.Index < 0)
                    {
                        CommandBuffer.SetComponentEnabled<StatusVariableUpdate>(sortKey, entity, true);
                        Entity moduleEntity = Entity.Null;
                        
                        if (indexedUpdate.HasModule && References.BlobAsset.Value.TryGetValue(indexedUpdate.Id, out var reference))
                        {
                            moduleEntity = CommandBuffer.Instantiate(sortKey, ModulePrefabs[reference.Value.ModulePrefabIndex].Entity);
                            CommandBuffer.AppendToBuffer(sortKey, entity, new Modules { Value = moduleEntity });
                            CommandBuffer.SetComponent(sortKey, moduleEntity, new Module
                            {
                                Parent = entity,
                                BaseValue = reference.Value.BaseValue,
                                Stacks = indexedUpdate.Stacks,
                                PreviousStacks = 0,
                                IsBeingDestroyed = false,
                                IsBeingUpdated = false
                            });
                            CommandBuffer.SetComponentEnabled<ModuleUpdateTag>(sortKey, moduleEntity, true);
                            CommandBuffer.SetComponentEnabled<ModuleDestroyTag>(sortKey, moduleEntity, false);
                            CommandBuffer.SetComponentEnabled<ModuleCleanupTag>(sortKey, moduleEntity, false);
                        }
                        CommandBuffer.AppendToBuffer(sortKey, entity, new StatusEffects()
                        {
                            Module = moduleEntity,
                            Id = indexedUpdate.Id,
                            Timing = indexedUpdate.Timing,
                            Duration = indexedUpdate.Duration,
                            Interval = indexedUpdate.Interval,
                            Stacks = indexedUpdate.Stacks,
                            EventId = indexedUpdate.EventId,
                        });
                        continue;
                    }

                    ref var updatingStatusEffectRef = ref statusEffects.ElementAt(indexedUpdate.Index);
                    // If all the stacks are removed we remove the effect.
                    if (updatingStatusEffectRef.Stacks + indexedUpdate.Stacks <= 0)
                    {
                        CommandBuffer.SetComponentEnabled<StatusVariableUpdate>(sortKey, entity, true);
                        if (updatingStatusEffectRef.Module != Entity.Null)
                        {
                            Module module = ModuleLookup[updatingStatusEffectRef.Module];
                            module.PreviousStacks = module.Stacks;
                            module.IsBeingUpdated = true;
                            module.IsBeingDestroyed = true;
                            CommandBuffer.SetComponent(sortKey, updatingStatusEffectRef.Module, module);
                            CommandBuffer.SetComponentEnabled<ModuleUpdateTag>(sortKey, updatingStatusEffectRef.Module, true);
                            CommandBuffer.SetComponentEnabled<ModuleDestroyTag>(sortKey, updatingStatusEffectRef.Module, true);
                        }
                        statusEffects.RemoveAt(indexedUpdate.Index);
                    }
                    // Otherwise just update the stack count.
                    else
                    {
                        CommandBuffer.SetComponentEnabled<StatusVariableUpdate>(sortKey, entity, true);
                        updatingStatusEffectRef.Stacks += indexedUpdate.Stacks;
                        if (updatingStatusEffectRef.Module != Entity.Null)
                        {
                            Module module = ModuleLookup[updatingStatusEffectRef.Module];
                            module.PreviousStacks = module.Stacks;
                            module.Stacks = updatingStatusEffectRef.Stacks;
                            module.IsBeingUpdated = true;
                            CommandBuffer.SetComponent(sortKey, updatingStatusEffectRef.Module, module);
                            CommandBuffer.SetComponentEnabled<ModuleUpdateTag>(sortKey, updatingStatusEffectRef.Module, true);
#if NETCODE
                            var request = CommandBuffer.CreateEntity(sortKey);
                            CommandBuffer.AddComponent(sortKey, request, new ModuleReplicationCommand() 
                            { 
                                Entity = updatingStatusEffectRef.Module,
                                Stacks = module.Stacks,
                                PreviousStacks = module.PreviousStacks
                            });
                            CommandBuffer.AddComponent(sortKey, request, new SendRpcCommandRequest { TargetConnection = Entity.Null });
#endif
                        }
                    }
                }
                statusEffectStackUpdates.Dispose();
            }
        }

        [BurstCompile(OptimizeFor = OptimizeFor.Performance)]
        public partial struct StatusEffectUpdateJob : IJobEntity
        {
            public EntityCommandBuffer.ParallelWriter CommandBuffer;
            [ReadOnly]
            public ComponentLookup<Module> ModuleLookup;
            [ReadOnly]
            public float TimeDelta;

            void Execute([EntityIndexInQuery] int sortKey, Entity entity, ref DynamicBuffer<StatusEffects> statusEffects)
            {
                // If nothing to change then continue.
                if (statusEffects.Length <= 0)
                    return;
                
                // Iterate in reverse to not skip any that get removed.
                for (int i = statusEffects.Length - 1; i >= 0 ; i--)
                {
                    ref var statusEffect = ref statusEffects.ElementAt(i);

                    switch (statusEffect.Timing)
                    {
                        case StatusEffectTiming.Infinite:
                            continue;
                        case StatusEffectTiming.Duration:
                            statusEffect.Duration -= TimeDelta;
                            if (statusEffect.Duration <= 0)
                                RemoveStatusEffect(ref statusEffect, ref statusEffects, entity, i, sortKey);
                            break;
                        default: 
                            // Event and Predicate timings only check if duration has
                            // run out because user created systems should handle
                            // decrementing those StatusEffects.
                            if (statusEffect.Duration <= 0)
                                RemoveStatusEffect(ref statusEffect, ref statusEffects, entity, i, sortKey);
                            break;
                    }
                }
            }

            void RemoveStatusEffect(ref StatusEffects statusEffect, ref DynamicBuffer<StatusEffects> statusEffectBuffer, Entity entity, int index, int sortKey)
            {
                CommandBuffer.SetComponentEnabled<StatusVariableUpdate>(sortKey, entity, true);
                if (statusEffect.Module != Entity.Null)
                {
                    Module module = ModuleLookup[statusEffect.Module];
                    module.PreviousStacks = module.Stacks;
                    module.IsBeingUpdated = true;
                    module.IsBeingDestroyed = true;
                    CommandBuffer.SetComponent(sortKey, statusEffect.Module, module);
                    CommandBuffer.SetComponentEnabled<ModuleUpdateTag>(sortKey, statusEffect.Module, true);
                    CommandBuffer.SetComponentEnabled<ModuleDestroyTag>(sortKey, statusEffect.Module, true);
                }
                statusEffectBuffer.RemoveAt(index);
            }
        }
    }
}
#endif