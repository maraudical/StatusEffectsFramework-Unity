#if UNITASK
using Cysharp.Threading.Tasks;
#else
using System.Collections;
#endif
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
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
#if UNITY_EDITOR
        private const BindingFlags _bindingFlags = BindingFlags.Public |
                                                  BindingFlags.NonPublic |
                                                  BindingFlags.Instance |
                                                  BindingFlags.Static;
#endif
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
        /// Returns the listed <see cref="StatusEffect"/>s in a <see cref="HashSet{}"/> for a given <see cref="MonoBehaviour"/>.
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
        public static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData) where T : MonoBehaviour, IStatus
        {
            return AddStatusEffect(monoBehaviour, statusEffectData, duration: null);
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to a <see cref="MonoBehaviour"/>. 
        /// The given <see cref="float"/> time will limit the duration of the 
        /// effect in seconds. Returns null if no <see cref="StatusEffect"/> was added.
        /// </summary>
        public static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, float duration) where T : MonoBehaviour, IStatus
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");

            StatusEffect statusEffect = AddStatusEffect(monoBehaviour, statusEffectData, (float?)duration);

            if (statusEffect == null)
                return null;
            // Begin a timer on the monobehaviour.
#if UNITASK
            statusEffect.timedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(monoBehaviour.GetCancellationTokenOnDestroy());
            statusEffect.timedTask = TimedEffect(statusEffect.timedTokenSource.Token);
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
        public static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, float duration, UnityEvent unityEvent, int interval = 1) where T : MonoBehaviour, IStatus
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");
            if (unityEvent == null)
                Debug.LogError("The given Unity Event is null!");
            // Create the effect
            StatusEffect statusEffect = AddStatusEffect(monoBehaviour, statusEffectData, (float?)duration);
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
        public static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, System.Func<bool> predicate) where T : MonoBehaviour, IStatus
        {
            // Check for null values
            if (!statusEffectData)
                Debug.LogError("The given Status Effect Data is null!");
            if (predicate == null)
                Debug.LogError("The given predicate is null!");

            StatusEffect statusEffect = AddStatusEffect(monoBehaviour, statusEffectData, duration: null);

            if (statusEffect == null)
                return null;
            // Begin a timer on the monobehaviour.
#if UNITASK
            statusEffect.timedTokenSource = CancellationTokenSource.CreateLinkedTokenSource(monoBehaviour.GetCancellationTokenOnDestroy());
            statusEffect.timedTask = TimedEffect(statusEffect.timedTokenSource.Token);
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
            statusEffect.StopCustomEffect(monoBehaviour);
            // Call the method on the inherited interface.
            monoBehaviour.OnStatusEffect(statusEffect, false);
#if UNITY_EDITOR
            // Use reflection to get all the status variables on the monobehaviour
            // And remove the effect reference from each of the fields. Note this
            // is just to update the inspector value.
            foreach (var field in monoBehaviour.GetType().GetFields(_bindingFlags).Where(f => f.FieldType.IsSubclassOf(typeof(StatusVariable))))
                ((StatusVariable)field.GetValue(monoBehaviour)).UpdateReferences(monoBehaviour);
#endif
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/> from a <see cref="MonoBehaviour"/>. If a stack count is given it will remove only the specified amount.
        /// </summary>
#nullable enable
        public static void RemoveStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, int? stack = null) where T : MonoBehaviour, IStatus
#nullable disable
        {
            int removedCount = 0;

            if (!monoBehaviour || statusEffectData == null || monoBehaviour.effects == null)
                return;
            // From the end of the list iterate through and if the given data is tagged remove the effect.
            for (int i = monoBehaviour.effects.Count - 1; i >= 0; i--)
                if (monoBehaviour.effects.ElementAt(i).data == statusEffectData)
                {
                    if (stack != null && removedCount >= stack)
                        break;

                    RemoveStatusEffect(monoBehaviour, monoBehaviour.effects.ElementAt(i));
                    removedCount++;
                }
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/> that 
        /// have the same <see cref="string"/> name.
        /// </summary>
        public static void RemoveStatusEffect<T>(this T monoBehaviour, ComparableName name) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || monoBehaviour.effects == null)
                return;
            // From the end of the list iterate through and if the given name is tagged remove the effect.
            for (int i = monoBehaviour.effects.Count - 1; i >= 0; i--)
                if (monoBehaviour.effects.ElementAt(i).data.comparableName == name)
                    RemoveStatusEffect(monoBehaviour, monoBehaviour.effects.ElementAt(i));
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
        private static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, float? duration) where T : MonoBehaviour, IStatus
#nullable disable
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                Debug.LogWarning("Please only add Status Effects in play mode!");
                return null;
            }
#endif
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
            List<StatusEffectData> removeEffects = new List<StatusEffectData>();
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
                        case Timing.Duration:
                            monoBehaviour.AddStatusEffect(condition.configurable, condition.duration);
                            break;
                        case Timing.Inherited:
                            if (duration.HasValue)
                                monoBehaviour.AddStatusEffect(condition.configurable, durationValue);
                            else
                                goto default;
                            break;
                        default:
                            monoBehaviour.AddStatusEffect(condition.configurable);
                            break;
                    }
                // Special case where the configurable which is the
                // current data to be added is tagged for removal.
                else if (condition.configurable == statusEffectData)
                    preventStatusEffect = true;
                else
                    removeEffects.Add(condition.configurable);
            }

            foreach (var data in removeEffects)
                monoBehaviour.RemoveStatusEffect(data.comparableName);

            if (preventStatusEffect)
                return null;
            // Create a new status effect instance.
            StatusEffect statusEffect = new StatusEffect(statusEffectData, durationValue);
            // Check to delete the effect if it already exists to prevent duplicates.
            if (!statusEffectData.allowEffectStacking)
            {
                StatusEffect oldStatusEffect = monoBehaviour.GetStatusEffects(name: statusEffectData.comparableName)?.FirstOrDefault();

                if (oldStatusEffect == null)
                    goto NothingToStack;

                switch (statusEffect.data.nonStackingBehaviour)
                {
                    case NonStackingBehaviour.MatchHighestValue:
                        // WARNING: There is an extremely special case here where
                        // a player may either have or try to apply an effect which
                        // has an infinite duration (-1). In this situation, attempt
                        // to take the higest value, and if they are the same take
                        // the infinite duration effect.
                        if (statusEffect.duration < 0 || oldStatusEffect.duration < 0)
                        {
                            if (statusEffect.data.baseValue < oldStatusEffect.data.baseValue) { return null; }
                            else if (statusEffect.data.baseValue > oldStatusEffect.data.baseValue || statusEffect.duration < 0)
                            {
                                RemoveStatusEffect(monoBehaviour, statusEffectData.comparableName);
                                break;
                            }
                            else { return null; }
                        }
                        // Find which effect is highest value.
                        StatusEffect higestValue = statusEffect.data.baseValue < oldStatusEffect.data.baseValue ? oldStatusEffect : statusEffect;
                        StatusEffect lowestValue = statusEffect.data.baseValue < oldStatusEffect.data.baseValue ? statusEffect : oldStatusEffect;
                        // Calculate the new duration = d1 + d2 / (v1 / v2). Note this assumes neither base value will ever be 0.
                        if (higestValue.data.baseValue == 0 || lowestValue.data.baseValue == 0)
                            Debug.LogError($"{(higestValue.data.baseValue == 0 ? higestValue.data : lowestValue.data)} has a base value of 0! This will cause an error!");

                        higestValue.duration = higestValue.duration + lowestValue.duration / (Mathf.Abs(higestValue.data.baseValue) / Mathf.Abs(lowestValue.data.baseValue));
                        statusEffect = higestValue;
                        RemoveStatusEffect(monoBehaviour, statusEffectData.comparableName);
                        break;
                    case NonStackingBehaviour.TakeHighestDuration:
                        if (statusEffect.duration < oldStatusEffect.duration)
                            return null;
                        RemoveStatusEffect(monoBehaviour, statusEffectData.comparableName);
                        break;
                    case NonStackingBehaviour.TakeHighestValue:
                        if (Mathf.Abs(statusEffect.data.baseValue) < Mathf.Abs(oldStatusEffect.data.baseValue))
                            return null;
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
            // Add the effect for a given monobehaviour.
            if (monoBehaviour.effects == null)
                monoBehaviour.effects = new List<StatusEffect> { statusEffect };
            else
                monoBehaviour.effects.Add(statusEffect);
#if UNITY_EDITOR
            // Use reflection to get all the status variables on the monobehaviour
            // And add the effect as a reference to each of the fields. Note this
            // is just to update the inspector value.
            foreach (var field in monoBehaviour.GetType().GetFields(_bindingFlags).Where(f => f.FieldType.IsSubclassOf(typeof(StatusVariable))))
                ((StatusVariable)field.GetValue(monoBehaviour)).UpdateReferences(monoBehaviour);
#endif
            // If a custom effect exists it will be started.
            statusEffect.StartCustomEffect(monoBehaviour);
            // Call the method on the inherited interface.
            monoBehaviour.OnStatusEffect(statusEffect, true);
            // Return the effect in case it is wanted for other reference.
            return statusEffect;
        }
#endregion
    }
}
