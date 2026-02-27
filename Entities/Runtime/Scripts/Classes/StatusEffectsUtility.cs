using Unity.Burst;
using Unity.Collections;
using Unity.Entities;

namespace StatusEffects.Entities
{
    [BurstCompile]
    public static class StatusEffectsUtility
    {
        /// <summary>
        /// Finds the index in the <see cref="DynamicBuffer{T}"/> where the <see cref="ActiveStatusEffects.Id"/> equals a given <paramref name="id"/>.
        /// </summary>
        /// <returns>The index of the first occurrence of the value in the buffer. Returns -1 if no occurrence is found.</returns>
        [BurstCompile]
        public static int IndexOfStatusEffect(in DynamicBuffer<ActiveStatusEffects> buffer, uint id)
        {
            unsafe { return NativeArrayExtensions.IndexOf<ActiveStatusEffects, uint>(buffer.GetUnsafePtr(), buffer.Length, id); }
        }

        /// <summary>
        /// Attempts to find the <see cref="ActiveStatusEffects"/> in a <see cref="DynamicBuffer{T}"/> where the <see cref="ActiveStatusEffects.Id"/> equals a given <paramref name="id"/>.
        /// </summary>
        [BurstCompile]
        public static bool TryGetStatusEffect(in DynamicBuffer<ActiveStatusEffects> buffer, uint id, out ActiveStatusEffects statusEffect)
        {
            var index = IndexOfStatusEffect(buffer, id);
            bool foundIndex = index >= 0;
            statusEffect = foundIndex ? buffer[index] : default;
            return foundIndex;
        }
        
        [BurstCompile]
        public static bool ModuleInfosContainType(ref BlobArray<ModuleInfo> array, TypeIndex type)
        {
            for (var i = 0; i < array.Length; i++)
                if (array[i].TypeIndex == type)
                    return true;

            return false;
        }

        /// <summary>
        /// Adds a new <see cref="Modules{T}"/> and retrieves values from the <see cref="ModuleInfo"/>.
        /// </summary>
        [BurstCompile]
        public static Modules<T> AddModuleToBuffer<T>(ref DynamicBuffer<Modules<T>> buffer, ModuleInfo moduleInfo, uint id) where T : unmanaged
        {
            var module = new Modules<T>
            {
                Id = id,
                Value = moduleInfo.GetValue<T>()
            };
            buffer.Add(module);
            return module;
        }

        /// <summary>
        /// Removes all <see cref="Modules{T}"/> with the same <paramref name="id"/> value.
        /// </summary>
        [BurstCompile]
        public static void RemoveModulesFromBuffer<T>(ref DynamicBuffer<Modules<T>> buffer, uint id) where T : unmanaged
        {
            for (int i = buffer.Length - 1; i >= 0; i--)
                if (buffer[i].Id == id)
                    buffer.RemoveAtSwapBack(i);
        }
    }
}
