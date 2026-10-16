using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// Creates the value of an <see cref="Effect"/> whose <see cref="ValueSource"/> is
    /// <see cref="ValueSource.DynamicValue"/>. The value can change while the <see cref="StatusEffect"/> is active.
    /// </summary>
    public abstract class DynamicEffect : ScriptableObject 
    {
        /// <summary>
        /// Whether the value is applied after all other effects, instead of together with them. It is then applied on top of
        /// <see cref="StatusFloat.PreEvaluationValue"/>.
        /// </summary>
        public abstract bool PostEvaluate { get; }
    }
}
