using System;
namespace StatusEffectsFramework
{
    /// <summary>
    /// A ScriptableObject that represents a status event. It serves as a base class for creating specific events that can be triggered and handled within the framework.
    /// </summary>
    public class StatusEvent : Registrant 
    {
        public event Action<float> Invoked;

        /// <summary>
        /// Invokes the event, triggering any subscribed listeners. The optional parameter <paramref name="value"/> is used to subtract from each associated <see cref="StatusEffect.Duration"/>.
        /// </summary>
        /// <param name="value">The float value to pass through the event. This will be subtracted from each associated <see cref="StatusEffect.Duration"/>.</param>
        public virtual void Invoke(float value = 1f)
        {
            Invoked?.Invoke(value);
        }
    }
}
