#if ENTITIES
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace StatusEffectFramework.Entities
{
    [BurstCompile]
    public static class StatusEffectsECSUtility
    {
        public static Hash128 GenerateBurstId() => new(Guid.NewGuid().ToString("N"));
        /// <summary>
        /// Finds the index in the <see cref="DynamicBuffer{T}"/> where the <see cref="StatusEffects.Id"/> equals a given <paramref name="id"/>.
        /// </summary>
        /// <returns>The index of the first occurrence of the value in the buffer. Returns -1 if no occurrence is found.</returns>
        [BurstCompile]
        public static int IndexOfStatusEffect(in DynamicBuffer<StatusEffects> buffer, uint id)
        {
            return buffer.AsNativeArray().IndexOf(id);
        }

        /// <summary>
        /// Attempts to find the <see cref="StatusEffects"/> in a <see cref="DynamicBuffer{T}"/> where the <see cref="StatusEffects.Id"/> equals a given <paramref name="id"/>.
        /// </summary>
        [BurstCompile]
        public static bool TryGetStatusEffect(in DynamicBuffer<StatusEffects> buffer, uint id, out StatusEffects statusEffect)
        {
            var index = IndexOfStatusEffect(buffer, id);
            bool foundIndex = index >= 0;
            statusEffect = foundIndex ? buffer[index] : default;
            return foundIndex;
        }

        /*/// <summary>
        /// Processes an event <see cref="Modules{T}"/> with the same <paramref name="id"/> value.
        /// </summary>
        [BurstCompile]
        public unsafe static bool ProcessEventForModulesBuffer<T>(ref DynamicBuffer<Modules<T>> buffer, 
            ref DynamicBuffer<StatusEffects> statusEffects,
            ref UnmanagedStatusEffectData data,
            ref EntityCommandBuffer.ParallelWriter commandBuffer,
            ref bool foundBuffer,
            in StatusEffectEvents statusEffectEvent, 
            in TypeIndex typeIndex, 
            in Entity entity,
            in int sortKey,
            out StatusEffects statusEffect, 
            out ReadOnlySpan<ModuleInfo> moduleInfos) where T : unmanaged
        {
            statusEffect = default;
            moduleInfos = ReadOnlySpan<ModuleInfo>.Empty;

            if (!ModuleInfosContainType(ref data.Modules, typeIndex))
                return false;

            switch (statusEffectEvent.Event)
            {
                case StatusEffectEvent.Added:
                    if (!TryGetStatusEffect(statusEffects, statusEffectEvent.Id, out statusEffect))
                        return false;

                    moduleInfos = new ReadOnlySpan<ModuleInfo>(data.Modules.GetUnsafePtr(), data.Modules.Length);
                    int length = 0;
                    int startIndex = moduleInfos.Length;

                    for (int i = 0; i < moduleInfos.Length; i++)
                    {
                        var moduleInfo = moduleInfos[i];

                        if (moduleInfo.TypeIndex != typeIndex)
                            continue;

                        if (i < startIndex)
                            startIndex = i;

                        length++;

                        if (!foundBuffer)
                        {
                            foundBuffer = true;
                            buffer = commandBuffer.AddBuffer<Modules<T>>(sortKey, entity);
                        }
                        AddModuleToBuffer(ref buffer, moduleInfo, statusEffectEvent.Id);
                    }

                    if (length <= 0)
                        moduleInfos
                    moduleInfos.Slice(startIndex, length);

                    break;
                case StatusEffectEvent.Removed:
                    if (foundBuffer)
                        RemoveModulesFromBuffer(ref buffer, statusEffectEvent.Id);
                    break;
                case StatusEffectEvent.Updated:
                    if (!TryGetStatusEffect(statusEffects, statusEffectEvent.Id, out statusEffect))
                        return false;
                    break;
            }

            return true;
        }*/

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
        public static ref Modules<T> AddModuleToBuffer<T>(ref DynamicBuffer<Modules<T>> buffer, ModuleInfo moduleInfo, uint id) where T : unmanaged
        {
            var module = new Modules<T>
            {
                Id = id,
                Value = moduleInfo.GetValue<T>()
            };
            buffer.Add(module);
            return ref buffer.ElementAt(buffer.Length - 1);
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
#endif