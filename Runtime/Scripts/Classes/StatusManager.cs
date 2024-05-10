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

namespace StatusEffects
{
    public class StatusManager : MonoBehaviour
    {
        public static StatusEffectSettings settings => StatusEffectSettings.GetOrCreateSettings();

        private static event System.Action<float> s_globalTimeEvent;
        private static event System.Action s_unsetGlobalTimeEvent;
        private static bool _overrideGlobalTime = false;

        /// <summary>
        /// This <see cref="Action"/> is invoked when <see cref="StatusEffect"/>s are added or removed.
        /// </summary>
        [HideInInspector] public event System.Action<StatusEffect, bool, int> onStatusEffect;
        /// <summary>
        /// This is invoked to update <see cref="StatusVariable"/>s just before the onStatusEffect event is called.
        /// </summary>
        [HideInInspector] public event System.Action<StatusEffect> valueUpdate;
        /// <summary>
        /// Cannot directly edit this <see cref="IReadOnlyList{T}"/>! Please call 
        /// <see cref="AddStatusEffect(StatusEffectData, float, int)"/> or  <see cref="RemoveStatusEffect(StatusEffect)"/>.
        /// </summary>
        public IReadOnlyList<StatusEffect> effects =>_effects.AsReadOnly();

        [SerializeField] private List<StatusEffect> _effects;

        /// <summary>
        /// This is only necessary for games that don't have realtime <see cref="StatusEffect"/>s. 
        /// </summary>
        public static void SetGlobalTimeOverride(UnityEvent unityEvent, float inteval = 1)
        {
            if (_overrideGlobalTime)
                return;
            // Unset any previous override
            s_unsetGlobalTimeEvent?.Invoke();
            // Subscribe to events
            unityEvent.AddListener(InvokeEvent);
            s_unsetGlobalTimeEvent += Desubscribe;

            _overrideGlobalTime = true;

            void InvokeEvent()
            {
                s_globalTimeEvent?.Invoke(inteval);
            }

            void Desubscribe()
            {
                unityEvent.RemoveListener(InvokeEvent);
                s_unsetGlobalTimeEvent -= Desubscribe;
            }
        }
        /// <summary>
        /// If you have overridden the global time then you can reset it with this method. 
        /// </summary>
        public static void UnsetGlobalTimeOverride()
        {
            if (!_overrideGlobalTime)
                return;
            // Unset the previous override
            s_unsetGlobalTimeEvent?.Invoke();

            _overrideGlobalTime = false;
        }
        /// <summary>
        /// Returns the listed <see cref="StatusEffect"/>s in a <see cref="List{}"/> for the <see cref="StatusManager"/>.
        /// </summary>
#nullable enable
        public IReadOnlyList<StatusEffect> GetStatusEffects(StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null)
#nullable disable
        {
            if (_effects == null)
                return null;
            // Return the effects for a given monobehaviour, if given a group
            // or name to match only return effects within those categories.
            return _effects.Where(e => (name  == null || e.data.comparableName  == name) 
                                    && (group == null || (e.data.group & group) != 0)
                                    && (data == null || e.data == data))
                          .ToList()
                          .AsReadOnly();
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to this <see cref="StatusManager"/>. Returns null if no <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, int stack = 1)
        {
            return AddStatusEffect(statusEffectData, StatusEffectTiming.Infinite, null, stack);
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to the <see cref="StatusManager"/>. 
        /// The given <see cref="float"/> time will limit the duration of the 
        /// effect in seconds. Returns null if no <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, int stack = 1)
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");

            StatusEffect statusEffect = AddStatusEffect(statusEffectData, StatusEffectTiming.Duration, duration, stack);

            if (statusEffect == null)
                return null;
            // Begin a timer on the monobehaviour.
#if UNITASK
            statusEffect.timedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            TimedEffect(statusEffect.timedTokenSource.Token).Forget();
#elif UNITY_2023_1_OR_NEWER
            statusEffect.timedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _ = TimedEffect(statusEffect.timedTokenSource.Token);
#else
            statusEffect.timedCoroutine = StartCoroutine(TimedEffect());
#endif

            return statusEffect;

#if UNITASK
            async UniTask TimedEffect(CancellationToken token)
#elif UNITY_2023_1_OR_NEWER
            async Awaitable TimedEffect(CancellationToken token)
#else
            IEnumerator TimedEffect()
#endif
            {
                ResetTimer:
                // Basic decreasing timer.
                while (statusEffect.duration > 0
                   && !_overrideGlobalTime
#if UNITASK || !UNITY_2023_1_OR_NEWER
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
                    statusEffect.duration -= Time.deltaTime;
                }
                // If a global time override is set
                if (statusEffect.duration > 0 && _overrideGlobalTime)
                {
                    s_globalTimeEvent += SubtractInterval;
                    // Wait until duration has reached 0 due to action event calls.
#if UNITASK
                    await UniTask.WaitUntil(() => statusEffect.duration <= 0 || !_overrideGlobalTime, cancellationToken: token);
#elif UNITY_2023_1_OR_NEWER
                    await AwaitableExtensions.WaitUntil(() => statusEffect.duration <= 0 || !_overrideGlobalTime, token);
#else
                    yield return new WaitUntil(() => statusEffect.duration <= 0);
#endif

                    s_globalTimeEvent -= SubtractInterval;

                    if (!_overrideGlobalTime)
                        goto ResetTimer;

                    void SubtractInterval(float interval) { statusEffect.duration -= interval; }
                }
#if UNITASK
                if (!token.IsCancellationRequested)
#endif
                // Once it has ended remove the given effect.
                RemoveStatusEffect(statusEffect);
            }
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to the <see cref="StatusManager"/>. 
        /// The given <see cref="float"/> time will limit the duration of the 
        /// effect where each invocation of the <see cref="UnityEvent"/> 
        /// will reduce the duration by the given interval. Returns null if no 
        /// <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, float duration, UnityEvent unityEvent, int interval = 1, int stack = 1)
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");
            if (unityEvent == null)
                Debug.LogError("The given Unity Event is null!");
            // Create the effect
            StatusEffect statusEffect = AddStatusEffect(statusEffectData, StatusEffectTiming.Event, (float?)duration, stack);
            // Check for null or 0 duration effect
            if (statusEffect == null)
                return null;

            if (statusEffect.duration <= 0)
            {
                RemoveStatusEffect(statusEffect);
                return null;
            }
            // Subscribe to the decrement method.
            unityEvent.AddListener(Decrement);
            statusEffect.stopped += Unsubscribe;
            
            return statusEffect;

            void Decrement()
            {
                statusEffect.duration -= interval;
                
                if (statusEffect.duration <= 0)
                {
                    RemoveStatusEffect(statusEffect);
                    Unsubscribe();
                }
            }

            void Unsubscribe()
            {
                unityEvent.RemoveListener(Decrement);
                statusEffect.stopped -= Unsubscribe;
            }
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to a <see cref="StatusManager"/>. 
        /// The StatusEffect will be removed when the given 
        /// <see cref="System.Func{bool}"/> is true. Returns null if no 
        /// <see cref="StatusEffect"/> was added.
        /// </summary>
        public StatusEffect AddStatusEffect(StatusEffectData statusEffectData, System.Func<bool> predicate, int stack = 1)
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");
            if (predicate == null)
                Debug.LogError("The given predicate is null!");

            StatusEffect statusEffect = AddStatusEffect(statusEffectData, StatusEffectTiming.Predicate, null, stack);

            if (statusEffect == null)
                return null;
            // Begin a timer on the monobehaviour.
#if UNITASK
            statusEffect.timedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(this.GetCancellationTokenOnDestroy());
            TimedEffect(statusEffect.timedTokenSource.Token).Forget();
#elif UNITY_2023_1_OR_NEWER
            statusEffect.timedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(destroyCancellationToken);
            _ = TimedEffect(statusEffect.timedTokenSource.Token);
#else
            statusEffect.timedCoroutine = StartCoroutine(TimedEffect());
#endif

            return statusEffect;

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
                RemoveStatusEffect(statusEffect);
            }
        }
        /// <summary>
        /// Removes a <see cref="StatusEffect"/> from a <see cref="MonoBehaviour"/>.
        /// </summary>
        public void RemoveStatusEffect(StatusEffect statusEffect)
        {
            if (statusEffect == null || _effects == null)
                return;
            // Stop the timer
#if UNITASK || UNITY_2023_1_OR_NEWER
            statusEffect.timedTokenSource?.Cancel();
            statusEffect.timedTokenSource?.Dispose();
#else
            if (statusEffect.timedCoroutine != null)
                StopCoroutine(statusEffect.timedCoroutine);
#endif
            // Remove the effects for a given monobehaviour.
            _effects.Remove(statusEffect);

            valueUpdate?.Invoke(statusEffect);
            // If a module exists it will be stopped.
            statusEffect.DisableModules(this);
            
            onStatusEffect?.Invoke(statusEffect, false, statusEffect.stack);
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/> from a <see cref="MonoBehaviour"/>. If a stack count is given it will remove only the specified amount.
        /// </summary>
#nullable enable
        public void RemoveStatusEffect(StatusEffectData statusEffectData, int? stack = null)
#nullable disable
        {
            if (statusEffectData == null || _effects == null)
                return;

            if (stack <= 0)
                return;

            int removedCount = 0;
            int currentRemoveCount;
            int currentStackCount;
            StatusEffect statusEffect;
            // From the end of the list iterate through and if the given data is tagged remove the effect.
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                statusEffect = _effects[i];

                if (statusEffect.data == statusEffectData)
                {
                    if (stack != null)
                    {
                        currentStackCount = statusEffect.stack;

                        if (removedCount + currentStackCount > stack)
                        {
                            currentRemoveCount = (int)(currentStackCount - (removedCount + currentStackCount - stack));
                            statusEffect.stack -= currentRemoveCount;

                            valueUpdate?.Invoke(statusEffect);

                            statusEffect.DisableModules(this, stack);
                            
                            onStatusEffect?.Invoke(statusEffect, false, currentRemoveCount);

                            break;
                        }
                        removedCount += currentStackCount;
                    }
                    RemoveStatusEffect(statusEffect);
                }
            }
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/> that 
        /// have the same <see cref="string"/> name.
        /// </summary>
        public void RemoveStatusEffect(ComparableName name, int? stack = null)
        {
            if (_effects == null)
                return;

            if (stack <= 0)
                return;

            int removedCount = 0;
            int currentRemoveCount;
            int currentStackCount;
            StatusEffect statusEffect;
            // From the end of the list iterate through and if the given name is tagged remove the effect.
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                statusEffect = _effects[i];

                if (statusEffect.data.comparableName == name)
                {
                    if (stack != null)
                    {
                        currentStackCount = statusEffect.stack;

                        if (removedCount + currentStackCount > stack)
                        {
                            currentRemoveCount = (int)(currentStackCount - (removedCount + currentStackCount - stack));
                            statusEffect.stack -= currentRemoveCount;

                            valueUpdate?.Invoke(statusEffect);

                            statusEffect.DisableModules(this, stack);
                            
                            onStatusEffect?.Invoke(statusEffect, false, currentRemoveCount);

                            break;
                        }
                        removedCount += currentStackCount;
                    }
                    RemoveStatusEffect(statusEffect);
                }
            }
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/>.
        /// </summary>
        public void RemoveAllStatusEffects()
        {
            if (_effects == null)
                return;
            // From the end of the list iterate through and remove all.
            for (int i = _effects.Count - 1; i >= 0; i--)
                RemoveStatusEffect(_effects[i]);
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/> that 
        /// are part of the given <see cref="string"/> group. See <see cref="GroupStringAttribute"/>.
        /// </summary>
        public void RemoveAllStatusEffects(StatusEffectGroup group)
        {
            if (_effects == null)
                return;
            StatusEffect statusEffect;
            // From the end of the list iterate through and if the given group is tagged remove the effect.
            for (int i = _effects.Count - 1; i >= 0; i--)
            {
                statusEffect = _effects[i];

                if ((statusEffect.data.group & group) != 0)
                    RemoveStatusEffect(statusEffect);
            }
        }

        #region Private Methods
#nullable enable
        private StatusEffect AddStatusEffect(StatusEffectData statusEffectData, StatusEffectTiming timing, float? duration, int stack)
#nullable disable
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Please only add Status Effects in play mode!");
                return null;
            }
#endif
            if (stack <= 0)
                return null;
            
            if (!statusEffectData)
                throw new System.Exception($"Attempted to add a null {typeof(StatusEffect).Name} " +
                                           $"to a {typeof(MonoBehaviour).Name}. This is not allowed.");
            // First determine correct duration.
            float durationValue = duration.HasValue ? duration.Value : -1;
            // If the duration given is less than zero it won't be applied.
            if (duration.HasValue && duration.Value < 0)
                return null;
            // Check for conditions.
            List<StatusEffectData> removeEffects = new();
            List<ComparableName> removeNameEffects = new();
            List<StatusEffectGroup> removeGroupEffects = new();
            bool preventStatusEffect = false;

            foreach (Condition condition in statusEffectData.conditions)
            {
                bool exists = GetStatusEffects(name: condition.searchable.comparableName).Count > 0;
                // If the condition is checking for existence and it doesn't exist or if
                // its checking non-existence and does exist then skip this condition.
                if ((condition.exists && !exists) 
                || (!condition.exists && exists))
                    continue;

                if (condition.add)
                    switch (condition.timing)
                    {
                        case ConditionalTiming.Duration:
                            AddStatusEffect(condition.data, condition.duration);
                            break;
                        case ConditionalTiming.Inherited:
                            if (duration.HasValue)
                                AddStatusEffect(condition.data, durationValue);
                            else
                                goto default;
                            break;
                        default:
                            AddStatusEffect(condition.data);
                            break;
                    }
                // Special case where the configurable which is the
                // current data to be added is tagged for removal.
                else if (condition.data == statusEffectData)
                    preventStatusEffect = true;
                // Otherwise we remove effects.
                else
                    switch (condition.configurable)
                    {
                        case ConditionalConfigurable.Data:
                            removeEffects.Add(condition.data);
                            break;
                        case ConditionalConfigurable.Name:
                            removeNameEffects.Add(condition.comparableName);
                            break;
                        case ConditionalConfigurable.Group:
                            removeGroupEffects.Add(condition.group);
                            break;
                    }
            }

            foreach (var data in removeEffects)
                RemoveStatusEffect(data);

            foreach (var name in removeNameEffects)
                RemoveStatusEffect(name);

            foreach (var group in removeGroupEffects)
                RemoveAllStatusEffects(group);

            if (preventStatusEffect)
                return null;
            // Declare here to use later.
            StatusEffect statusEffect;
            // If stacking is allowed then check for the max stack and possible stack merging.
            if (statusEffectData.allowEffectStacking)
            {
                List<StatusEffect> statusEffects = GetStatusEffects(data: statusEffectData);

                if (statusEffects == null || statusEffects.Count <= 0)
                    goto NothingToStack;

                StatusEffect infiniteEffect = null;
                int stackCount = 0;
                // Go through all similar effects and count the stacks and if there is an
                // infinite effect.
                foreach (var existentEffect in statusEffects)
                {
                    if (existentEffect.timing == StatusEffectTiming.Infinite)
                        infiniteEffect = existentEffect;

                    stackCount += existentEffect.stack;
                }
                // Check if the current stacked amount is already max.
                if (statusEffectData.maxStack >= 0 && stackCount >= statusEffectData.maxStack)
                    return null;
                // Otherwise cut the given stack if it would've been over the max.
                else if (statusEffectData.maxStack >= 0 && stackCount + stack > statusEffectData.maxStack)
                    stack = stackCount + stack - statusEffectData.maxStack;
                // We can only safely merge infinite stacks because merging things with
                // duration or predicates we either can't or its too difficult to determine
                // their similarity.
                if (timing == StatusEffectTiming.Infinite && infiniteEffect != null)
                {
                    infiniteEffect.stack += stack;
                    statusEffect = infiniteEffect;
                    goto AddedToStack;
                }
            }
            // Check to delete the effect if it already exists to prevent duplicates.
            else
            {
                stack = 1;

                StatusEffect oldStatusEffect = GetStatusEffects(name: statusEffectData.comparableName)?.FirstOrDefault();

                if (oldStatusEffect == null)
                    goto NothingToStack;

                switch (statusEffectData.nonStackingBehaviour)
                {
                    case NonStackingBehaviour.MatchHighestValue:
                        // WARNING: There is an extremely special case here where
                        // a player may either have or try to apply an effect which
                        // has an infinite duration (-1). In this situation, attempt
                        // to take the higest value, and if they are the same take
                        // the infinite duration effect.
                        if (durationValue < 0 || oldStatusEffect.duration < 0)
                        {
                            if (statusEffectData.baseValue < oldStatusEffect.data.baseValue) { return null; }
                            else if (statusEffectData.baseValue > oldStatusEffect.data.baseValue || durationValue < 0)
                            {
                                RemoveStatusEffect(statusEffectData.comparableName);
                                break;
                            }
                            else { return null; }
                        }
                        // Find which effect is highest value.
                        StatusEffectData higestValueData = statusEffectData.baseValue < oldStatusEffect.data.baseValue ? oldStatusEffect.data : statusEffectData;
                        float highestValueDuration = statusEffectData.baseValue < oldStatusEffect.data.baseValue ? oldStatusEffect.duration : durationValue;
                        StatusEffectData lowestValueData = statusEffectData.baseValue < oldStatusEffect.data.baseValue ? statusEffectData : oldStatusEffect.data;
                        float lowestValueDuration = statusEffectData.baseValue < oldStatusEffect.data.baseValue ? durationValue : oldStatusEffect.duration;
                        // Calculate the new duration = d1 + d2 / (v1 / v2). Note this assumes neither base value will ever be 0.
                        if (higestValueData.baseValue == 0 || lowestValueData.baseValue == 0)
                            Debug.LogError($"{(higestValueData.baseValue == 0 ? higestValueData : lowestValueData)} has a base value of 0! This will cause an error!");

                        durationValue = highestValueDuration + lowestValueDuration / (Mathf.Abs(higestValueData.baseValue) / Mathf.Abs(lowestValueData.baseValue));
                        statusEffectData = higestValueData;
                        RemoveStatusEffect(statusEffectData.comparableName);
                        break;
                    case NonStackingBehaviour.TakeHighestDuration:
                        if (!(durationValue < 0) && (durationValue < oldStatusEffect.duration || oldStatusEffect.duration < 0))
                            return null;
                        else
                            RemoveStatusEffect(statusEffectData.comparableName);
                        break;
                    case NonStackingBehaviour.TakeHighestValue:
                        float oldValue = Mathf.Abs(oldStatusEffect.data.baseValue);
                        float newValue = Mathf.Abs(statusEffectData.baseValue);
                        if (newValue == oldValue)
                            goto case NonStackingBehaviour.TakeHighestDuration;
                        if (newValue < oldValue)
                            return null;
                        else
                            RemoveStatusEffect(statusEffectData.comparableName);
                        break;
                    case NonStackingBehaviour.TakeNewest:
                        RemoveStatusEffect(statusEffectData.comparableName);
                        break;
                    case NonStackingBehaviour.TakeOldest:
                        return null;
                }
            }

            NothingToStack:
            // Create a new status effect instance.
            statusEffect = new StatusEffect(statusEffectData, timing, durationValue, stack);
            // Add the effect for a given monobehaviour. This also is the first time
            // initializing so we need to initialize all of the Status Variables
            if (_effects == null)
                _effects = new List<StatusEffect> { statusEffect };
            else
                _effects.Add(statusEffect);

            AddedToStack:

            valueUpdate?.Invoke(statusEffect);
            // If a module exists it will be started.
            statusEffect.EnableModules(this, stack);
            
            onStatusEffect?.Invoke(statusEffect, true, stack);
            // Return the effect in case it is wanted for other reference.
            return statusEffect;
        }
        #endregion
    }
}
