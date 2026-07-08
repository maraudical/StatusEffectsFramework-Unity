#if ENTITIES
using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;

namespace StatusEffectsFramework.Entities
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
    }
}
#endif