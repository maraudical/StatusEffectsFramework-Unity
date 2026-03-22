using System;

namespace StatusEffectFramework
{
    public class DynamicBool
    {
        public event Action OnValueChanged;
        public bool Value;
    }
}
