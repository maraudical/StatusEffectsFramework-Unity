using System;
using UnityEngine;

namespace StatusEffects
{
    [Serializable]
    public class StatusInt : StatusVariable
    {
        public StatusNameInt statusName;
        public int baseValue;
        public int value => GetValue();

#if UNITY_EDITOR
#pragma warning disable CS0414 // Remove unread private members
        [SerializeField] private int _value;
#pragma warning restore CS0414 // Remove unread private members

#endif
        public StatusInt(int baseValue)
        {
            this.baseValue = baseValue;
        }

        public StatusInt(int baseValue, StatusNameInt statusName, MonoBehaviour monoBehaviour)
        {
            this.statusName = statusName;
            this.baseValue = baseValue;
            this.monoBehaviour = monoBehaviour;
        }

        protected virtual int GetValue()
        {
#if UNITY_EDITOR
            if (monoBehaviour == null)
                return baseValue;
#endif
            if (iStatus == null)
                iStatus = monoBehaviour as IStatus;

            if (iStatus.effects == null)
                return baseValue;

            int additiveValue = 0;
            int multiplicativeValue = 1;
            int postAdditiveValue = 0;

            int effectValue;

            foreach (StatusEffect statusEffect in iStatus.effects)
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
            
            return (baseValue + additiveValue) * multiplicativeValue + postAdditiveValue;
        }

        public static implicit operator int(StatusInt statusInt) => statusInt.value;
#if UNITY_EDITOR
        public override void OnStatusEffect(MonoBehaviour monoBehaviour)
        {
            _value = GetValue();
            this.monoBehaviour = monoBehaviour;
        }
#endif
    }
}
