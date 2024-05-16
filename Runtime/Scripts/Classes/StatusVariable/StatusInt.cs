using System;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusInt : StatusVariable
    {
        [SerializeField] public event Action onValueChanged;

        public StatusNameInt statusName;
        [SerializeField] private int _baseValue;
        [SerializeField] private bool _signProtected;
        public int value;
        
        public StatusInt(int baseValue, bool signProtected = true)
        {
            _baseValue = baseValue;
            _signProtected = signProtected;
        }

        public StatusInt(int baseValue, StatusNameInt statusName, bool signProtected = true)
        {
            this.statusName = statusName;
            _baseValue = baseValue;
            _signProtected = signProtected;
            value = GetValue();
        }

        public void ChangeBaseValue(int value)
        {
            _baseValue = value;
            this.value = GetValue();
            onValueChanged?.Invoke();
        }

        protected override void InstanceUpdate(StatusEffect statusEffect)
        {
            // Only update if the status effect actually has any effects that have the same StatusName
            if (statusEffect.data.effects.Select(e => e.statusName).Contains(statusName))
            {
                value = GetValue();
                onValueChanged?.Invoke();
            }
        }

        protected virtual int GetValue()
        {
            if (instance == null)
                return _baseValue;

            bool positive = Mathf.Sign(_baseValue) > 0;
            int additiveValue = 0;
            int multiplicativeValue = 1;
            int postAdditiveValue = 0;

            int effectValue;

            foreach (StatusEffect statusEffect in instance.effects)
            {
                foreach (Effect effect in statusEffect.data.effects)
                {
                    if (effect.statusName != statusName)
                        continue;

                    effectValue = statusEffect.stack * (effect.useBaseValue ? (int)statusEffect.data.baseValue : effect.intValue);

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
                return Mathf.Clamp((_baseValue + additiveValue) * multiplicativeValue + postAdditiveValue, positive ? 0 : int.MinValue, positive ? int.MaxValue : 0);
            else
                return (_baseValue + additiveValue) * multiplicativeValue + postAdditiveValue;
        }
        public static implicit operator int(StatusInt statusInt) => statusInt.value;

        public override void SetManager(StatusManager instance)
        {
            base.SetManager(instance);

            value = GetValue();
            onValueChanged?.Invoke();
        }
#if UNITY_EDITOR

        private async void BaseValueUpdate()
        {
            await Task.Yield();
            value = GetValue();
            onValueChanged?.Invoke();
        }
        private async void SignProtectedUpdate()
        {
            await Task.Yield();
            value = GetValue();
            onValueChanged?.Invoke();
        }
#endif
    }
}
