using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusFloat : StatusVariable
    {
        public StatusNameFloat statusName;
        [SerializeField] private float _baseValue;
        [SerializeField] private bool _signProtected;
        public float value;
        
        public StatusFloat(float baseValue, bool signProtected = true)
        {
            _baseValue = baseValue;
            _signProtected = signProtected;
        }

        public StatusFloat(float baseValue, StatusNameFloat statusName, bool signProtected = true)
        {
            this.statusName = statusName;
            _baseValue = baseValue;
            _signProtected = signProtected;
            value = GetValue();
        }
        
        public void ChangeBaseValue(float value)
        {
            _baseValue = value;
            this.value = GetValue();
        }

        protected override void InstanceUpdate(StatusEffect statusEffect)
        {
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.data.effects.Select(e => e.statusName).Contains(statusName))
                value = GetValue();
        }

        protected float GetValue()
        {
            if (instance == null)
                return _baseValue;

            bool positive = Mathf.Sign(_baseValue) >= 0;
            float additiveValue = 0;
            float multiplicativeValue = 1;
            float postAdditiveValue = 0;

            float effectValue;

            foreach (StatusEffect statusEffect in instance.effects)
            {
                foreach (Effect effect in statusEffect.data.effects)
                {
                    if (effect.statusName != statusName)
                        continue;

                    effectValue = statusEffect.stack * (effect.useBaseValue ? statusEffect.data.baseValue : effect.floatValue);

                    switch (effect.valueModifier)
                    {
                        case ValueModifier.Additive:
                            additiveValue += effectValue;
                            break;
                        case ValueModifier.Multiplicative:
                            multiplicativeValue += effectValue;
                            break;
                        case ValueModifier.PostAdditive:
                            postAdditiveValue += effectValue;
                            break;

                    }
                }
            }
            
            if (_signProtected)
                return Mathf.Clamp((_baseValue + additiveValue) * multiplicativeValue + postAdditiveValue, positive ? 0 : float.NegativeInfinity, positive ? float.PositiveInfinity : 0);
            else
                return (_baseValue + additiveValue) * multiplicativeValue + postAdditiveValue;
        }

        public static implicit operator float(StatusFloat statusFloat) => statusFloat.value;

        public override void SetManager(StatusManager instance)
        {
            base.SetManager(instance);

            value = GetValue();
        }
#if UNITY_EDITOR

        private async void BaseValueUpdate()
        {
            await Task.Yield();
            value = GetValue();
        }
        private async void SignProtectedUpdate()
        {
            await Task.Yield();
            value = GetValue();
        }
#endif
    }
}
