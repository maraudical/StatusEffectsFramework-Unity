namespace StatusEffects
{
    public abstract class StatusVariable
    {
        protected IStatusManager Instance;
        /// <summary>
        /// Sets up the <see cref="StatusVariable"/>. This must be set before trying to get any value from it.
        /// </summary>
        public virtual void SetManager(IStatusManager instance)
        {
            if (Instance != null)
                Instance.ValueUpdate -= InstanceUpdate;

            Instance = instance;

            Instance.ValueUpdate += InstanceUpdate;
        }

        protected abstract void InstanceUpdate(StatusEffect statusEffect);
    }
}
