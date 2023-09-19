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
        public static List<StatusEffect> GetStatusEffects<T>(this T monoBehaviour) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour)
                return null;
            // Return the effects for a given monobehaviour
            return monoBehaviour.effects;
        }
        /// <summary>
        /// Adds a <see cref="StatusEffect"/> to a <see cref="MonoBehaviour"/>. 
        /// Defining a <see cref="float"/> time will limit the duration of the effect.
        /// </summary>
        public static StatusEffect AddStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, float time = 0) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || !statusEffectData)
                return null;
            // Create a new status effect instance
            StatusEffect statusEffect = new StatusEffect(statusEffectData, time);
            // Check to delete the effect if it already exists to prevent duplicates
            if (!settings.allowEffectStacking)
                RemoveStatusEffect(monoBehaviour, statusEffect.data, true);
            // Add the effect for a given monobehaviour
            if (monoBehaviour.effects == null)
                monoBehaviour.effects = new List<StatusEffect> { statusEffect };
            else
                monoBehaviour.effects.Add(statusEffect);
            // If the effect is timed, begin a coroutine on the monobehaviour
            if (time > 0)
                monoBehaviour.StartCoroutine(TimedEffect(monoBehaviour, statusEffect));
            // Use reflection to get all the status variables on the monobehaviour
            // And add the effect as a reference to each of the fields
            foreach (var field in monoBehaviour.GetType().GetFields(bindingFlags).Where(f => f.FieldType.IsSubclassOf(typeof(StatusVariable))))
                ((StatusVariable)field.GetValue(monoBehaviour)).UpdateReferences(monoBehaviour);
            // If a custom effect exists it will be started
            statusEffect.StartCustomEffect(monoBehaviour);
            // Call the method on the inherited interface
            monoBehaviour.OnStatusEffect(statusEffect, true);
            // Return the effect in case it is wanted for other reference
            return statusEffect;
        }
        /// <summary>
        /// Removes a <see cref="StatusEffect"/> from a <see cref="MonoBehaviour"/>.
        /// </summary>
        public static void RemoveStatusEffect<T>(this T monoBehaviour, StatusEffect statusEffect) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || statusEffect == null || monoBehaviour.effects == null)
                return;
            // Remove the effects for a given monobehaviour
            monoBehaviour.effects.Remove(statusEffect);
            // Use reflection to get all the status variables on the monobehaviour
            // And remove the effect reference from each of the fields
            foreach (var field in monoBehaviour.GetType().GetFields(bindingFlags).Where(f => f.FieldType.IsSubclassOf(typeof(StatusVariable))))
                ((StatusVariable)field.GetValue(monoBehaviour)).UpdateReferences(monoBehaviour);
            // If a custom effect exists it will be stopped
            statusEffect.StopCustomEffect(monoBehaviour);
            // Call the method on the inherited interface
            monoBehaviour.OnStatusEffect(statusEffect, false);
        }
        /// <summary>
        /// Removes a <see cref="StatusEffect"/> from a <see cref="MonoBehaviour"/>.
        /// </summary>
        public static void RemoveStatusEffect<T>(this T monoBehaviour, StatusEffectData statusEffectData, bool removeStack = true) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || statusEffectData == null || monoBehaviour.effects == null)
                return;
            // Attempt to find the effects for a given monobehaviour
            try
            {
                if (removeStack)
                    foreach (StatusEffect effect in monoBehaviour.effects.Where(e => e.data == statusEffectData))
                        RemoveStatusEffect(monoBehaviour, effect);
                else
                    RemoveStatusEffect(monoBehaviour, monoBehaviour.effects.First(e => e.data == statusEffectData));
            }
            catch { }
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/> that 
        /// are part of the given <see cref="string"/> group. See <see cref="GroupStringAttribute"/>.
        /// </summary>
        public static void RemoveStatusEffects<T>(this T monoBehaviour, string group) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || monoBehaviour.effects == null)
                return;
            // From the end of the list iterate through and if the given group is tagged remove the effect
            for (int i = monoBehaviour.effects.Count - 1; i >= 0; i--)
                if (monoBehaviour.effects.ElementAt(i).data.group == group)
                    RemoveStatusEffect(monoBehaviour, monoBehaviour.effects.ElementAt(i));
        }
        /// <summary>
        /// Removes all <see cref="StatusEffect"/>s from a <see cref="MonoBehaviour"/>.
        /// </summary>
        public static void RemoveAllStatusEffects<T>(this T monoBehaviour) where T : MonoBehaviour, IStatus
        {
            if (!monoBehaviour || monoBehaviour.effects == null)
                return;
            // From the end of the list iterate through and if the given group is tagged remove the effect
            for (int i = monoBehaviour.effects.Count - 1; i >= 0; i--)
                RemoveStatusEffect(monoBehaviour, monoBehaviour.effects.ElementAt(i));
        }

        private static IEnumerator TimedEffect<T>(T monoBehaviour, StatusEffect statusEffect) where T : MonoBehaviour, IStatus
        {
            // Basic decreasing timer
            while (statusEffect.time > 0)
            {
                yield return null;
                statusEffect.time -= Time.deltaTime;
            }
            // Once it has ended remove the given effect
            RemoveStatusEffect(monoBehaviour, statusEffect);
        }

        #region Extra Methods
        /// <summary>
        /// Returns a <see cref="MemberInfo"/> from a <see cref="MonoBehaviour"/> given the <see cref="string"/> member name.
        /// </summary>
        public static MemberInfo GetMemberInfo<T>(this T monoBehaviour, string memberName) where T : MonoBehaviour, IStatus
        {
            // Use reflection to get all the status floats on the monobehaviour
            // And return the one that matches the given status name
            return monoBehaviour.GetType().GetField(memberName, bindingFlags) as MemberInfo ??
                   monoBehaviour.GetType().GetProperty(memberName, bindingFlags);
        }
        /// <summary>
        /// Returns a <see cref="MethodInfo"/> from a <see cref="MonoBehaviour"/> given the <see cref="string"/> method name.
        /// </summary>
        public static MethodInfo GetMethodInfo<T>(this T monoBehaviour, string methodName) where T : MonoBehaviour, IStatus
        {
            // Use reflection to get all the status floats on the monobehaviour
            // And return the one that matches the given status name
            return monoBehaviour.GetType().GetMethod(methodName, bindingFlags);
        }
        #endregion
    }
}
