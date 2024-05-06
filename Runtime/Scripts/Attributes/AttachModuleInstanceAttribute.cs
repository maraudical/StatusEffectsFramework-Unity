using System;

namespace StatusEffects.Modules
{
    /// <summary>
    /// Define the <see cref="ModuleInstance"/> to be used with this <see cref="Module"/>.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
    public class AttachModuleInstanceAttribute : Attribute
    {
        public Type type;
        
        public AttachModuleInstanceAttribute(Type requiredComponent)
        {
            type = requiredComponent;
        }
    }
}
