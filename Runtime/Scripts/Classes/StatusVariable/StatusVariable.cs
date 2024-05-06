using UnityEngine;

namespace StatusEffects
{
    public abstract class StatusVariable
    {
        [SerializeField] protected StatusManager instance;

        public virtual void SetInstance(StatusManager instance)
        {
            if (this.instance)
                this.instance.valueUpdate -= InstanceUpdate;

            this.instance = instance;

            this.instance.valueUpdate += InstanceUpdate;
        }

        protected abstract void InstanceUpdate(StatusEffect statusEffect);
    }
}
