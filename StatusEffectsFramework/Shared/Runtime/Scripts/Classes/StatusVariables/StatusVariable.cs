using System;

namespace StatusEffectsFramework
{
    [Serializable]
    public abstract class StatusVariable
    {
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

        public abstract TState GetState();

        /// <summary>
        /// Applies the given <typeparamref name="TState"/>. This still invokes <see cref="StateChanged"/>.
        /// </summary>
        public abstract void ApplyState(in TState state);

        protected void RaiseStateChanged() => StateChanged?.Invoke();
    }
}
