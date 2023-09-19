using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace StatusEffects
{
    public interface IStatus
    {
        /// <summary>
        /// Do not directly edit this <see cref="HashSet{T}"/>! Please call 
        /// <see cref="StatusManager.AddStatusEffect{T}(T, StatusEffectData, float)"/> or  <see cref="StatusManager.RemoveAllStatusEffects{T}(T)"/>.
        /// </summary>
        public List<StatusEffect> effects { get; set; }
        /// <summary>
        /// Use this callback to monitor changes on a specific <see cref="StatusEffect"/> when they become active or removed.
        /// </summary>
        void OnStatusEffect(StatusEffect statusEffect, bool active);
    }
}
