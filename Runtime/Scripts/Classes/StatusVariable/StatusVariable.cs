using UnityEngine;

namespace StatusEffects
{
    public abstract class StatusVariable
    {
        [SerializeField] protected MonoBehaviour monoBehaviour;
        protected IStatus iStatus;
#if UNITY_EDITOR

        public abstract void OnStatusEffect(MonoBehaviour monoBehaviour);
#endif
    }
}
