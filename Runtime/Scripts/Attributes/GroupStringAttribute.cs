using System;
using UnityEngine;

namespace StatusEffects
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
    public class GroupStringAttribute : PropertyAttribute { }
}
