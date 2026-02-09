#if ENTITIES
using StatusEffects.Modules;
using System;
using System.Runtime.InteropServices;
using Unity.Collections.LowLevel.Unsafe;

namespace StatusEffects.Entities
{
    public interface IEntityModule
    {
        public IntPtr ModuleStructPtr(ModuleInstance moduleInstance)
        {
            
        }
        public Type ModuleSystemType();
    }
}
#endif