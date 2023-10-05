using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace StatusEffects
{
    public static class StatusManager
    {
        public static StatusEffectSettings settings => StatusEffectSettings.GetOrCreateSettings();

        private const BindingFlags bindingFlags = BindingFlags.Public |
                                                  BindingFlags.NonPublic |
                                                  BindingFlags.Instance |
                                                  BindingFlags.Static;
        /// <summary>
        /// Returns the listed <see cref="StatusEffect"/>s in a <see cref="HashSet{}"/> for a given <see cref="MonoBehaviour"/>.
        /// </summary>
#nullable enable
        public static List<StatusEffect> GetStatusEffects<T>(this T monoBehaviour, string? group = null, string? name = null) where T : MonoBehaviour, IStatus
        {
#nullable disable
            if (!monoBehaviour)
                return null;
            // Return the effects for a given monobehaviour, if given a group
            // or name to match only return effects within those categories.
            return monoBehaviour.effects.Where(e => (name  == null || e.data.name  == name) 
                                                 && (group == null || e.data.group == group))
                                        .ToList();
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to a <see cref="MonoBehaviour"/>.
        /// </summary>
        public static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData) where T : MonoBehaviour, IStatus
        {
            return AddStatusEffect(monoBehaviour, statusEffectData, duration: null);
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to a <see cref="MonoBehaviour"/>. 
        /// The given <see cref="float"/> time will limit the duration of the 
        /// effect in seconds.
        /// </summary>
        public static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, float duration) where T : MonoBehaviour, IStatus
        {
            StatusEffect statusEffect = AddStatusEffect(monoBehaviour, statusEffectData, (float?)duration);
            // Begin a coroutine on the monobehaviour.
            monoBehaviour.StartCoroutine(TimedEffect(monoBehaviour, statusEffect));

            return statusEffect;

            static IEnumerator TimedEffect(T monoBehaviour, StatusEffect statusEffect)
            {
                // Basic decreasing timer.
                while (statusEffect.duration > 0)
                {
                    yield return null;
                    statusEffect.duration -= Time.deltaTime;
                }
                // Once it has ended remove the given effect.
                RemoveStatusEffect(monoBehaviour, statusEffect);
            }
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to a <see cref="MonoBehaviour"/>. 
        /// The given <see cref="float"/> time will limit the duration of the 
        /// effect where each invocation of the <see cref="System.Action"/> 
        /// will reduce the duration by 1.
        /// </summary>
        public static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, float duration, ref System.Action action) where T : MonoBehaviour, IStatus
        {
            StatusEffect statusEffect = AddStatusEffect(monoBehaviour, statusEffectData, (float?)duration);
            // Begin a coroutine on the monobehaviour.
            action += () => { statusEffect.duration -= 1; };
            monoBehaviour.StartCoroutine(TimedEffect());
            
            return statusEffect;

            IEnumerator TimedEffect()
            {
                // Wait until duration has reached 0 due to action event calls.
                yield return new WaitUntil(() => statusEffect.duration <= 0);
                // Once it has ended remove the given effect.
                RemoveStatusEffect(monoBehaviour, statusEffect);
            }
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to a <see cref="MonoBehaviour"/>. 
        /// The StatusEffect will be removed when the given 
        /// <see cref="System.Func{bool}"/> is true.
        /// </summary>
        public static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, System.Func<bool> predicate) where T : MonoBehaviour, IStatus
        {
            StatusEffect statusEffect = AddStatusEffect(monoBehaviour, statusEffectData, duration: null);
            // Begin a coroutine on the monobehaviour.
            monoBehaviour.StartCoroutine(TimedEffect());

            return statusEffect;

            IEnumerator TimedEffect()
            {
                // Wait until the predicate is true.
                yield return new WaitUntil(predicate);
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
            // Remove the effects for a given monobehaviour.
            monoBehaviour.effects.Remove(statusEffect);
            // Use reflection to get all the status variables on the monobehaviour
            // And remove the effect reference from each of the fields.
            foreach (var field in monoBehaviour.GetType().GetFields(bindingFlags).Where(f => f.FieldType.IsSubclassOf(typeof(StatusVariable))))
                ((StatusVariable)field.GetValue(monoBehaviour)).UpdateReferences(monoBehaviour);
            // If a custom effect exists it will be stopped.
            statusEffect.StopCustomEffect(monoBehaviour);
            // Call the method on the inherited interface.
            monoBehaviour.OnStatusEffect(statusEffect, false);
        }
        /// <summary>
        /// Removes a <see cref="StatusEffect"/> from a <see cref="MonoBehaviour"/>.
        /// </summary>
        public static void RemoveStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || statusEffectData == null || monoBehaviour.effects == null)
                return;
            // From the end of the list iterate through and if the given group is tagged remove the effect.
            for (int i = monoBehaviour.effects.Count - 1; i >= 0; i--)
                if (monoBehaviour.effects.ElementAt(i).data == statusEffectData)
                    RemoveStatusEffect(monoBehaviour, monoBehaviour.effects.ElementAt(i));
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/> that 
        /// have the same <see cref="string"/> name.
        /// </summary>
        public static void RemoveStatusEffect<T>(this T monoBehaviour, string name) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || monoBehaviour.effects == null)
                return;
            // From the end of the list iterate through and if the given group is tagged remove the effect.
            for (int i = monoBehaviour.effects.Count - 1; i >= 0; i--)
                if (monoBehaviour.effects.ElementAt(i).data.name == name)
                    RemoveStatusEffect(monoBehaviour, monoBehaviour.effects.ElementAt(i));
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/>.
        /// </summary>
        public static void RemoveAllStatusEffects<T>(this T monoBehaviour) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || monoBehaviour.effects == null)
                return;
            // From the end of the list iterate through and if the given group is tagged remove the effect.
            for (int i = monoBehaviour.effects.Count - 1; i >= 0; i--)
                RemoveStatusEffect(monoBehaviour, monoBehaviour.effects.ElementAt(i));
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/> that 
        /// are part of the given <see cref="string"/> group. See <see cref="GroupStringAttribute"/>.
        /// </summary>
        public static void RemoveAllStatusEffects<T>(this T monoBehaviour, string group) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || monoBehaviour.effects == null)
                return;
            // From the end of the list iterate through and if the given group is tagged remove the effect.
            for (int i = monoBehaviour.effects.Count - 1; i >= 0; i--)
                if (monoBehaviour.effects.ElementAt(i).data.group == group)
                    RemoveStatusEffect(monoBehaviour, monoBehaviour.effects.ElementAt(i));
        }

        #region Private Methods
        #nullable enable
        private static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, float? duration) where T : MonoBehaviour, IStatus
        {
#nullable disable
            if (!monoBehaviour)
                throw new System.Exception($"Attempted to add a {typeof(StatusEffect).Name} to a null {typeof(MonoBehaviour).Name}. This is not allowed.");
            if (!statusEffectData)
                throw new System.Exception($"Attempted to add a null {typeof(StatusEffect).Name} to a {typeof(MonoBehaviour).Name}. This is not allowed.");

            float durationValue = duration.HasValue ? duration.Value : 0;
            // Create a new status effect instance.
            StatusEffect statusEffect = new StatusEffect(statusEffectData, durationValue);
            // Check to delete the effect if it already exists to prevent duplicates.
            if (!statusEffectData.allowEffectStacking)
            {
                StatusEffect oldStatusEffect = monoBehaviour.GetStatusEffects(name: statusEffectData.name).FirstOrDefault();

                if (oldStatusEffect != null)
                    switch (statusEffect.data.nonStackingBehaviour)
                    {
                        case NonStackingBehaviour.MatchHighestValue:
                            // WARNING: There is an extremely special case here where
                            // a player may either have or try to apply an effect which
                            // has an infinite duration (0). In this situation, attempt
                            // to take the higest value, and if they are the same take
                            // the infinite duration effect.
                            if (statusEffect.duration <= 0 || oldStatusEffect.duration <= 0)
                            {
                                if (statusEffect.data.baseValue < oldStatusEffect.data.baseValue) { return oldStatusEffect; }
                                else if (statusEffect.data.baseValue > oldStatusEffect.data.baseValue || statusEffect.duration <= 0)
                                {
                                    RemoveStatusEffect(monoBehaviour, statusEffectData.name);
                                    break;
                                }
                                else { return oldStatusEffect; }
                            }
                            // Find which effect is highest value.
                            StatusEffect higestValue = statusEffect.data.baseValue < oldStatusEffect.data.baseValue ? oldStatusEffect : statusEffect;
                            StatusEffect lowestValue = statusEffect.data.baseValue < oldStatusEffect.data.baseValue ? statusEffect : oldStatusEffect;
                            // Calculate the new duration = d1 + d2 / (v1 / v2).
                            higestValue.duration = higestValue.duration + lowestValue.duration / (higestValue.data.baseValue / lowestValue.data.baseValue);
                            statusEffect = higestValue;
                            RemoveStatusEffect(monoBehaviour, statusEffectData.name);
                            break;
                        case NonStackingBehaviour.TakeHighestDuration:
                            // If the old status effect duration is 0 it means that it is an infinite duration so keep that.
                            if (statusEffect.duration < oldStatusEffect.duration || oldStatusEffect.duration <= 0)
                                return oldStatusEffect;
                            RemoveStatusEffect(monoBehaviour, statusEffectData.name);
                            break;
                        case NonStackingBehaviour.TakeHighestValue:
                            if (statusEffect.data.baseValue < oldStatusEffect.data.baseValue)
                                return oldStatusEffect;
                            RemoveStatusEffect(monoBehaviour, statusEffectData.name);
                            break;
                        case NonStackingBehaviour.TakeNewest:
                            RemoveStatusEffect(monoBehaviour, statusEffectData.name);
                            break;
                        case NonStackingBehaviour.TakeOldest:
                            return oldStatusEffect;
                    }
            }
            // Add the effect for a given monobehaviour.
            if (monoBehaviour.effects == null)
                monoBehaviour.effects = new List<StatusEffect> { statusEffect };
            else
                monoBehaviour.effects.Add(statusEffect);
            // Use reflection to get all the status variables on the monobehaviour
            // And add the effect as a reference to each of the fields.
            foreach (var field in monoBehaviour.GetType().GetFields(bindingFlags).Where(f => f.FieldType.IsSubclassOf(typeof(StatusVariable))))
                ((StatusVariable)field.GetValue(monoBehaviour)).UpdateReferences(monoBehaviour);
            // If a custom effect exists it will be started.
            statusEffect.StartCustomEffect(monoBehaviour);
            // Call the method on the inherited interface.
            monoBehaviour.OnStatusEffect(statusEffect, true);
            // Return the effect in case it is wanted for other reference.
            return statusEffect;
        }
        #endregion

        #region Extra Methods
        /// <summary>
        /// Returns a <see cref="MemberInfo"/> from a <see cref="MonoBehaviour"/> given the <see cref="string"/> member name.
        /// </summary>
        public static MemberInfo GetMemberInfo<T>(this T monoBehaviour, string memberName) where T : MonoBehaviour, IStatus
        {
            // Use reflection to get all the status floats on the monobehaviour
            // And return the one that matches the given status name.
            return monoBehaviour.GetType().GetField(memberName, bindingFlags) as MemberInfo ??
                   monoBehaviour.GetType().GetProperty(memberName, bindingFlags);
        }
        /// <summary>
        /// Returns a <see cref="MethodInfo"/> from a <see cref="MonoBehaviour"/> given the <see cref="string"/> method name.
        /// </summary>
        public static MethodInfo GetMethodInfo<T>(this T monoBehaviour, string methodName) where T : MonoBehaviour, IStatus
        {
            // Use reflection to get all the status floats on the monobehaviour
            // And return the one that matches the given status name.
            return monoBehaviour.GetType().GetMethod(methodName, bindingFlags);
        }
        #endregion
    }
}
