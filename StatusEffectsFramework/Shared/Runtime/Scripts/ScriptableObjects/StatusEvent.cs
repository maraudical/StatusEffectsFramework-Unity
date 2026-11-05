using System;
using UnityEngine;
namespace StatusEffectsFramework
{
    /// <summary>
    /// A ScriptableObject that represents a status event. It serves as a base class for creating specific events that can be triggered and handled within the framework.
    /// </summary>
    [CreateAssetMenu(fileName = "New Status Event", menuName = "Status Effects Framework/Status Event", order = 0)]
    public class StatusEvent : Registrant 
    {
        /// <summary>
        /// Raised by <see cref="Invoke"/> with the value that was passed.
        /// </summary>
        public event Action<float> Invoked;

        /// <summary>
        /// Invokes the event, triggering any subscribed listeners. The optional parameter <paramref name="value"/> is used to subtract from each associated <see cref="StatusEffect.Duration"/>.
        /// </summary>
        /// <param name="value">The float value to pass through the event. This will be subtracted from each associated <see cref="StatusEffect.Duration"/>.</param>
        public virtual void Invoke(float value = 1f)
        {
            // Timers listen to the registered event, so a copy of it, such as one inside a mod's bundle, raises that one.
            var registry = StatusRegistry.Instance;
            var registered = registry ? registry.GetRegistered(this) : this;
            if (registered != this)
            {
                registered.Invoke(value);
                return;
            }

            Invoked?.Invoke(value);
        }
    }
}
