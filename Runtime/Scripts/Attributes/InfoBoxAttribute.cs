using System;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class InfoBoxAttribute : PropertyAttribute { public int messageType = 0; }
}
