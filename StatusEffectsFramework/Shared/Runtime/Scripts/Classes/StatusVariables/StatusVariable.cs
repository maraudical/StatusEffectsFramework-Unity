using System;

namespace StatusEffectsFramework
{
    /// <summary>
    /// A value, such as a health stat, that the <see cref="StatusEffect"/>s of a <see cref="StatusManager"/> can change.
    /// Call <see cref="SetManager"/> to start applying them.
    /// </summary>
    [Serializable]
    public abstract class StatusVariable
    {
        /// <summary>
        /// The manager whose status effects change this variable.
        /// </summary>
        [NonSerialized]
        protected IStatusManager Manager;
        /// <summary>
        /// Sets up the <see cref="StatusVariable"/>. This must be set before trying to get any value from it.
        /// </summary>
        public virtual void SetManager(IStatusManager instance)
        {
            if (Manager != null)
                Manager.StatusEffectAction -= OnStatusEffect;

            Manager = instance;

            Manager.StatusEffectAction += OnStatusEffect;
        }

        /// <summary>
        /// Called when a status effect is added or removed, or its stack count changes.
        /// </summary>
        protected abstract void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int previousStacks, int currentStacks);
    }

    /// <summary>
    /// A <see cref="StatusVariable"/> whose base values can be replicated over a network through a blittable
    /// <typeparamref name="TState"/>. Only the base values are replicated, since every instance evaluates the
    /// status effects itself.
    /// </summary>
    [Serializable]
    public abstract class StatusVariable<TState> : StatusVariable where TState : struct, IStatusState<TState>
    {
        /// <summary>
        /// Invoked whenever a value that is part of the <typeparamref name="TState"/> changes.
        /// </summary>
        public event Action StateChanged;

        /// <summary>
        /// Gets the base values that are replicated over the network.
        /// </summary>
        public abstract TState GetState();

        /// <summary>
        /// Applies the given <typeparamref name="TState"/>. This still invokes <see cref="StateChanged"/>.
        /// </summary>
        public abstract void ApplyState(in TState state);

        protected void RaiseStateChanged() => StateChanged?.Invoke();
    }
}
