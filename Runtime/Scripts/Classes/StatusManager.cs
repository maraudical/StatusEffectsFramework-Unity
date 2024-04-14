#if UNITASK
using Cysharp.Threading.Tasks;
using System.Collections;
using System.Threading;
#else
using System.Collections;
#endif
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Events;

namespace StatusEffects
{
    public static class StatusManager
    {
        public static StatusEffectSettings settings => StatusEffectSettings.GetOrCreateSettings();

        private static event System.Action<float> s_globalTimeEvent;
        private static event System.Action s_unsetGlobalTimeEvent;
        private static bool _overrideGlobalTime = false;
        
        private const BindingFlags _bindingFlags = BindingFlags.Public |
                                                  BindingFlags.NonPublic |
                                                  BindingFlags.Instance |
                                                  BindingFlags.Static;
        
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
        /// Returns the listed <see cref="StatusEffect"/>s in a <see cref="List{}"/> for a given <see cref="MonoBehaviour"/>.
        /// </summary>
#nullable enable
        public static List<StatusEffect> GetStatusEffects<T>(this T monoBehaviour, StatusEffectGroup? group = null, ComparableName? name = null, StatusEffectData? data = null) where T : MonoBehaviour, IStatus
#nullable disable
        {
            if (!monoBehaviour || monoBehaviour.effects == null)
                return null;
            // Return the effects for a given monobehaviour, if given a group
            // or name to match only return effects within those categories.
            return monoBehaviour.effects.Where(e => (name  == null || e.data.comparableName  == name) 
                                                 && (group == null || (e.data.group & group) != 0)
                                                 && (data == null || e.data == data))
                                        .ToList();
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to a <see cref="MonoBehaviour"/>. Returns null if no <see cref="StatusEffect"/> was added.
        /// </summary>
        public static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, int stack = 1) where T : MonoBehaviour, IStatus
        {
            return AddStatusEffect(monoBehaviour, statusEffectData, StatusEffectTiming.Infinite, null, stack);
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to a <see cref="MonoBehaviour"/>. 
        /// The given <see cref="float"/> time will limit the duration of the 
        /// effect in seconds. Returns null if no <see cref="StatusEffect"/> was added.
        /// </summary>
        public static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, float duration, int stack = 1) where T : MonoBehaviour, IStatus
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");

            StatusEffect statusEffect = AddStatusEffect(monoBehaviour, statusEffectData, StatusEffectTiming.Duration, duration, stack);

            if (statusEffect == null)
                return null;
            // Begin a timer on the monobehaviour.
#if UNITASK
            statusEffect.timedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(monoBehaviour.GetCancellationTokenOnDestroy());
            TimedEffect(statusEffect.timedTokenSource.Token).Forget();
#else
            statusEffect.timedCoroutine = monoBehaviour.StartCoroutine(TimedEffect(monoBehaviour, statusEffect));
#endif

            return statusEffect;

#if UNITASK
            async UniTask TimedEffect(CancellationToken token)
#else
            IEnumerator TimedEffect(T monoBehaviour, StatusEffect statusEffect)
#endif
            {
                ResetTimer:
                // Basic decreasing timer.
                while (statusEffect.duration > 0
                   && !_overrideGlobalTime
#if UNITASK
                   && !token.IsCancellationRequested
#endif
                   )
                {
#if UNITASK
                    await UniTask.NextFrame();
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
                RemoveStatusEffect(monoBehaviour, statusEffect);
            }
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to a <see cref="MonoBehaviour"/>. 
        /// The given <see cref="float"/> time will limit the duration of the 
        /// effect where each invocation of the <see cref="UnityEvent"/> 
        /// will reduce the duration by the given interval. Returns null if no 
        /// <see cref="StatusEffect"/> was added.
        /// </summary>
        public static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, float duration, UnityEvent unityEvent, int interval = 1, int stack = 1) where T : MonoBehaviour, IStatus
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");
            if (unityEvent == null)
                Debug.LogError("The given Unity Event is null!");
            // Create the effect
            StatusEffect statusEffect = AddStatusEffect(monoBehaviour, statusEffectData, StatusEffectTiming.Event, (float?)duration, stack);
            // Check for null or 0 duration effect
            if (statusEffect == null)
                return null;

            if (statusEffect.duration <= 0)
            {
                RemoveStatusEffect(monoBehaviour, statusEffect);
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
                    RemoveStatusEffect(monoBehaviour, statusEffect);
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
        /// Adds a <see cref="StatusEffect"/> to a <see cref="MonoBehaviour"/>. 
        /// The StatusEffect will be removed when the given 
        /// <see cref="System.Func{bool}"/> is true. Returns null if no 
        /// <see cref="StatusEffect"/> was added.
        /// </summary>
        public static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, System.Func<bool> predicate, int stack = 1) where T : MonoBehaviour, IStatus
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");
            if (predicate == null)
                Debug.LogError("The given predicate is null!");

            StatusEffect statusEffect = AddStatusEffect(monoBehaviour, statusEffectData, StatusEffectTiming.Predicate, null, stack);

            if (statusEffect == null)
                return null;
            // Begin a timer on the monobehaviour.
#if UNITASK
            statusEffect.timedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(monoBehaviour.GetCancellationTokenOnDestroy());
            TimedEffect(statusEffect.timedTokenSource.Token).Forget();
#else
            statusEffect.timedCoroutine = monoBehaviour.StartCoroutine(TimedEffect());
#endif

            return statusEffect;

#if UNITASK
            async UniTask TimedEffect(CancellationToken token)
#else
            IEnumerator TimedEffect()
#endif
            {
                // Wait until the predicate is true.
#if UNITASK
                await UniTask.WaitUntil(predicate, cancellationToken: token);
#else
                yield return new WaitUntil(predicate);
#endif
                // Remove the given effect.
                RemoveStatusEffect(monoBehaviour, statusEffect);
            }
        }
        /// <summary>
        /// Removes a <see cref="StatusEffect"/> from a <see cref="MonoBehaviour"/>.
        /// </summary>
        public static void RemoveStatusEffect<T>(this T monoBehaviour, StatusEffect statusEffect) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || statusEffect == null || monoBehaviour.effects == null)
                return;
            // Stop the timer
#if UNITASK
            statusEffect.timedTokenSource?.Cancel();
#else
            if (statusEffect.timedCoroutine != null)
                monoBehaviour.StopCoroutine(statusEffect.timedCoroutine);
#endif
            // Remove the effects for a given monobehaviour.
            monoBehaviour.effects.Remove(statusEffect);
            // If a custom effect exists it will be stopped.
            statusEffect.DisableModules(monoBehaviour);
            // Call the method on the inherited interface.
            monoBehaviour.OnStatusEffect(statusEffect, false, statusEffect.stack);
#if UNITY_EDITOR
            UpdateReferences(monoBehaviour);
#endif
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/> from a <see cref="MonoBehaviour"/>. If a stack count is given it will remove only the specified amount.
        /// </summary>
#nullable enable
        public static void RemoveStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, int? stack = null) where T : MonoBehaviour, IStatus
#nullable disable
        {
            if (!monoBehaviour || statusEffectData == null || monoBehaviour.effects == null)
                return;
            if (stack <= 0)
                return;

            int removedCount = 0;
            int currentRemoveCount;
            int currentStackCount;
            // From the end of the list iterate through and if the given data is tagged remove the effect.
            for (int i = monoBehaviour.effects.Count - 1; i >= 0; i--)
                if (monoBehaviour.effects.ElementAt(i).data == statusEffectData)
                {
                    if (stack != null)
                    {
                        currentStackCount = monoBehaviour.effects.ElementAt(i).stack;

                        if (removedCount + currentStackCount > stack)
                        {
                            currentRemoveCount = (int)(currentStackCount - (removedCount + currentStackCount - stack));
                            monoBehaviour.effects.ElementAt(i).stack -= currentRemoveCount;
                            monoBehaviour.effects.ElementAt(i).DisableModules(monoBehaviour, stack);
                            monoBehaviour.OnStatusEffect(monoBehaviour.effects.ElementAt(i), false, currentRemoveCount);
#if UNITY_EDITOR
                            UpdateReferences(monoBehaviour);
#endif
                            break;
                        }
                        removedCount += currentStackCount;
                    }
                    RemoveStatusEffect(monoBehaviour, monoBehaviour.effects.ElementAt(i));
                }
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/> that 
        /// have the same <see cref="string"/> name.
        /// </summary>
        public static void RemoveStatusEffect<T>(this T monoBehaviour, ComparableName name, int? stack = null) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || monoBehaviour.effects == null)
                return;
            if (stack <= 0)
                return;

            int removedCount = 0;
            int currentRemoveCount;
            int currentStackCount;
            // From the end of the list iterate through and if the given name is tagged remove the effect.
            for (int i = monoBehaviour.effects.Count - 1; i >= 0; i--)
                if (monoBehaviour.effects.ElementAt(i).data.comparableName == name)
                {
                    if (stack != null)
                    {
                        currentStackCount = monoBehaviour.effects.ElementAt(i).stack;

                        if (removedCount + currentStackCount > stack)
                        {
                            currentRemoveCount = (int)(currentStackCount - (removedCount + currentStackCount - stack));
                            monoBehaviour.effects.ElementAt(i).stack -= currentRemoveCount;
                            monoBehaviour.effects.ElementAt(i).DisableModules(monoBehaviour, stack);
                            monoBehaviour.OnStatusEffect(monoBehaviour.effects.ElementAt(i), false, currentRemoveCount);
#if UNITY_EDITOR
                            UpdateReferences(monoBehaviour);
#endif
                            break;
                        }
                        removedCount += currentStackCount;
                    }
                    RemoveStatusEffect(monoBehaviour, monoBehaviour.effects.ElementAt(i));
                }
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/>.
        /// </summary>
        public static void RemoveAllStatusEffects<T>(this T monoBehaviour) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || monoBehaviour.effects == null)
                return;
            // From the end of the list iterate through and remove all.
            for (int i = monoBehaviour.effects.Count - 1; i >= 0; i--)
                RemoveStatusEffect(monoBehaviour, monoBehaviour.effects.ElementAt(i));
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/> that 
        /// are part of the given <see cref="string"/> group. See <see cref="GroupStringAttribute"/>.
        /// </summary>
        public static void RemoveAllStatusEffects<T>(this T monoBehaviour, StatusEffectGroup group) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || monoBehaviour.effects == null)
                return;
            // From the end of the list iterate through and if the given group is tagged remove the effect.
            for (int i = monoBehaviour.effects.Count - 1; i >= 0; i--)
                if ((monoBehaviour.effects.ElementAt(i).data.group & group) != 0)
                    RemoveStatusEffect(monoBehaviour, monoBehaviour.effects.ElementAt(i));
        }

        #region Private Methods
#nullable enable
        private static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, StatusEffectTiming timing, float? duration, int stack) where T : MonoBehaviour, IStatus
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
            if (!monoBehaviour)
                throw new System.Exception($"Attempted to add a {typeof(StatusEffect).Name} to a " +
                                           $"null {typeof(MonoBehaviour).Name}. This is not allowed.");
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
                bool exists = monoBehaviour.GetStatusEffects(name: condition.searchable.comparableName).Count > 0;
                // If the condition is checking for existence and it doesn't exist or if
                // its checking non-existence and does exist then skip this condition.
                if ((condition.exists && !exists) 
                || (!condition.exists && exists))
                    continue;

                if (condition.add)
                    switch (condition.timing)
                    {
                        case ConditionalTiming.Duration:
                            monoBehaviour.AddStatusEffect(condition.data, condition.duration);
                            break;
                        case ConditionalTiming.Inherited:
                            if (duration.HasValue)
                                monoBehaviour.AddStatusEffect(condition.data, durationValue);
                            else
                                goto default;
                            break;
                        default:
                            monoBehaviour.AddStatusEffect(condition.data);
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
                monoBehaviour.RemoveStatusEffect(data);

            foreach (var name in removeNameEffects)
                monoBehaviour.RemoveStatusEffect(name);

            foreach (var group in removeGroupEffects)
                monoBehaviour.RemoveAllStatusEffects(group);

            if (preventStatusEffect)
                return null;
            // Declare here to use later.
            StatusEffect statusEffect;
            // If stacking is allowed then check for the max stack and possible stack merging.
            if (statusEffectData.allowEffectStacking)
            {
                List<StatusEffect> statusEffects = monoBehaviour.GetStatusEffects(data: statusEffectData);

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

                StatusEffect oldStatusEffect = monoBehaviour.GetStatusEffects(name: statusEffectData.comparableName)?.FirstOrDefault();

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
                                RemoveStatusEffect(monoBehaviour, statusEffectData.comparableName);
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
                        RemoveStatusEffect(monoBehaviour, statusEffectData.comparableName);
                        break;
                    case NonStackingBehaviour.TakeHighestDuration:
                        if (!(durationValue < 0) && (durationValue < oldStatusEffect.duration || oldStatusEffect.duration < 0))
                            return null;
                        else
                            RemoveStatusEffect(monoBehaviour, statusEffectData.comparableName);
                        break;
                    case NonStackingBehaviour.TakeHighestValue:
                        float oldValue = Mathf.Abs(oldStatusEffect.data.baseValue);
                        float newValue = Mathf.Abs(statusEffectData.baseValue);
                        if (newValue == oldValue)
                            goto case NonStackingBehaviour.TakeHighestDuration;
                        if (newValue < oldValue)
                            return null;
                        else
                            RemoveStatusEffect(monoBehaviour, statusEffectData.comparableName);
                        break;
                    case NonStackingBehaviour.TakeNewest:
                        RemoveStatusEffect(monoBehaviour, statusEffectData.comparableName);
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
            if (monoBehaviour.effects == null)
                monoBehaviour.effects = new List<StatusEffect> { statusEffect };
            else
                monoBehaviour.effects.Add(statusEffect);

            AddedToStack:
#if UNITY_EDITOR
            UpdateReferences(monoBehaviour);
#endif
            // If a custom effect exists it will be started.
            statusEffect.EnableModules(monoBehaviour, stack);
            // Call the method on the inherited interface.
            monoBehaviour.OnStatusEffect(statusEffect, true, stack);
            // Return the effect in case it is wanted for other reference.
            return statusEffect;
        }

#if UNITY_EDITOR
        private static void UpdateReferences<T>(T monoBehaviour) where T : MonoBehaviour, IStatus
        {
            // Use reflection to get all the status variables on the monobehaviour
            // And remove the effect reference from each of the fields. Note this
            // is just to update the inspector value.
            foreach (var field in monoBehaviour.GetType().GetFields(_bindingFlags).Where(f => f.FieldType.IsSubclassOf(typeof(StatusVariable))))
            {
                ((StatusVariable)field.GetValue(monoBehaviour)).OnStatusEffect(monoBehaviour);
            }
        }
#endif
        #endregion
    }
}
