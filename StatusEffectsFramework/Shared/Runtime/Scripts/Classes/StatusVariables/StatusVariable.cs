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
                Manager.OnStatusEffect -= OnStatusEffect;

            Manager = instance;

            Manager.OnStatusEffect += OnStatusEffect;
        }

        protected abstract void OnStatusEffect(StatusEffect statusEffect, StatusEffectAction action, int previousStacks, int currentStacks);
    }
}
