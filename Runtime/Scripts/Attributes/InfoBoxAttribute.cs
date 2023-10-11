using System;
using UnityEngine;

namespace StatusEffects.Inspector
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class InfoBoxAttribute : PropertyAttribute { public string hexCode = "#FFFFFF"; public FontStyle style = FontStyle.Normal; }
}
