using System;
using Unity.Burst.CompilerServices;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Collects the effects on an entity into per-value-type maps keyed by status variable id and evaluates
    /// them for each status variable. Shared by the pre and post evaluate jobs.
    /// </summary>
    /// <remarks>
    /// Create one per chunk since the cached index in the type array depends on the chunk's archetype.
    /// Call <see cref="BeginEntity"/> before adding any effects for each entity.
    /// <para>
    /// Every entry carries the order of the status effect it came from, which is its index in the
    /// <see cref="StatusEffects"/> buffer. The buffer keeps insertion order, so a lower order is an older
    /// status effect, and the oldest wins ties between effects of equal priority.
    /// </para>
    /// </remarks>
    internal unsafe struct EffectCollector : IDisposable
    {
        // LastEntityIndex marks which entity last read the type's buffer so it is only read once per entity.
        private UnsafeHashMap<TypeIndex, (int IndexInTypeArray, TypeManager.TypeInfo TypeInfo, int LastEntityIndex)> m_TypeToIndexAndTypeInfo;
        private UnsafeHashMap<uint, (int Stacks, int Order)> m_InstanceIdToStacksAndOrder;

        private UnsafeParallelMultiHashMap<ushort, (ValueModifier ValueModifier, float Value, int Priority, int Stacks, int Order)> m_Floats;
        private UnsafeParallelMultiHashMap<ushort, (ValueModifier ValueModifier, int Value, int Priority, int Stacks, int Order)> m_Ints;
        private UnsafeParallelMultiHashMap<ushort, (bool Value, int Priority, int Order)> m_Bools;

        public EffectCollector(int capacity, Allocator allocator)
        {
            m_TypeToIndexAndTypeInfo = new UnsafeHashMap<TypeIndex, (int, TypeManager.TypeInfo, int)>(capacity, allocator);
            m_InstanceIdToStacksAndOrder = new UnsafeHashMap<uint, (int, int)>(capacity, allocator);
            m_Floats = new UnsafeParallelMultiHashMap<ushort, (ValueModifier, float, int, int, int)>(capacity, allocator);
            m_Ints = new UnsafeParallelMultiHashMap<ushort, (ValueModifier, int, int, int, int)>(capacity, allocator);
            m_Bools = new UnsafeParallelMultiHashMap<ushort, (bool, int, int)>(capacity, allocator);
        }

        /// <summary>
        /// Clears the effects of the previous entity and maps the instance ids of <paramref name="statusEffects"/>.
        /// </summary>
        public void BeginEntity(in DynamicBuffer<StatusEffects> statusEffects)
        {
            m_InstanceIdToStacksAndOrder.Clear();
            m_Floats.Clear();
            m_Ints.Clear();
            m_Bools.Clear();

            // Must be filled before any dynamic effect buffer is read since a buffer can contain
            // elements belonging to any of the status effects on the entity.
            for (int order = 0; order < statusEffects.Length; order++)
            {
                var statusEffect = statusEffects[order];
                m_InstanceIdToStacksAndOrder.Add(statusEffect.InstanceId, (statusEffect.Stacks, order));
            }
        }

        /// <summary>
        /// Resolves a non-dynamic <paramref name="effect"/> to its value and adds it.
        /// </summary>
        /// <param name="order">Index of the status effect the effect belongs to in the <see cref="StatusEffects"/> buffer.</param>
        public void AddStatic(ref UnmanagedEffect effect, int stacks, int order, float baseValue)
        {
            bool fromBase = effect.ValueSource == ValueSource.BaseValue;

            switch (effect.ValueType)
            {
                case ValueType.Float:
                    m_Floats.Add(effect.Id, (effect.ValueModifier, fromBase ? baseValue : effect.FloatValue, effect.Priority, stacks, order));
                    break;
                case ValueType.Int:
                    m_Ints.Add(effect.Id, (effect.ValueModifier, fromBase ? (int)baseValue : effect.IntValue, effect.Priority, stacks, order));
                    break;
                case ValueType.Bool:
                    m_Bools.Add(effect.Id, (fromBase ? baseValue != 0 : effect.BoolValue, effect.Priority, order));
                    break;
            }
        }

        /// <summary>
        /// Adds the elements of the dynamic effect buffer of <paramref name="effect"/>'s type on the entity at
        /// <paramref name="entityIndex"/>, keeping only those whose post evaluate flag matches <paramref name="postEvaluate"/>.
        /// </summary>
        public void AddDynamic(in ArchetypeChunk chunk, int entityIndex, ref UnmanagedEffect effect, bool postEvaluate)
        {
            ref var dynamicEffectInfo = ref effect.DynamicEffectInfo;
            var typeIndex = dynamicEffectInfo.TypeIndex;

            // A dynamic effect buffer holds the elements for every effect of its type so it must
            // only be read once per entity, even when multiple effects share the same type.
            ref var info = ref m_TypeToIndexAndTypeInfo.GetValueRefOrNullRef(typeIndex, out bool foundInfo);

            if (!foundInfo)
            {
                // The ref from TryGetValueByRef is not valid when the key was not found so rebind it to the new entry.
                info = ref m_TypeToIndexAndTypeInfo.AddByRef(typeIndex);
                info = (StatusEffectsUtility.GetIndexInTypeArray(chunk, typeIndex), TypeManager.GetTypeInfo(typeIndex), entityIndex);
            }
            else if (info.LastEntityIndex == entityIndex)
                return;
            else
                info.LastEntityIndex = entityIndex;

            if (Hint.Unlikely(info.IndexInTypeArray < 0))
                return;

            var header = (BufferHeader*)StatusEffectsUtility.GetComponentDataWithTypeRO(chunk, entityIndex, info.IndexInTypeArray);

            if (Hint.Unlikely(header == null))
                return;

            var buffer = BufferHeader.GetElementPointer(header);
            var length = header->Length;
            int sizeOfDynamicEffect = info.TypeInfo.ElementSize;

            for (int n = 0; n < length; n++)
            {
                var element = buffer + sizeOfDynamicEffect * n;
                if (*(bool*)(element + dynamicEffectInfo.PostEvaluateOffset) != postEvaluate)
                    continue;

                // Skip elements whose status effect is no longer on the entity.
                if (Hint.Unlikely(!m_InstanceIdToStacksAndOrder.TryGetValue(*(uint*)(element + dynamicEffectInfo.InstanceIdOffset), out var owner)))
                    continue;

                var id = *(ushort*)(element + dynamicEffectInfo.IdOffset);
                var priority = *(int*)(element + dynamicEffectInfo.PriorityOffset);

                switch (effect.ValueType)
                {
                    case ValueType.Float:
                        m_Floats.Add(id, (*(ValueModifier*)(element + dynamicEffectInfo.ValueModifierOffset), *(float*)(element + dynamicEffectInfo.ValueOffset), priority, owner.Stacks, owner.Order));
                        break;
                    case ValueType.Int:
                        m_Ints.Add(id, (*(ValueModifier*)(element + dynamicEffectInfo.ValueModifierOffset), *(int*)(element + dynamicEffectInfo.ValueOffset), priority, owner.Stacks, owner.Order));
                        break;
                    case ValueType.Bool:
                        m_Bools.Add(id, (*(bool*)(element + dynamicEffectInfo.ValueOffset), priority, owner.Order));
                        break;
                }
            }
        }

        /// <summary>
        /// Applies the collected effects for the status variable <paramref name="id"/> to <paramref name="startValue"/>.
        /// </summary>
        public float Evaluate(ushort id, float startValue, bool signProtected)
        {
            var statusFloatValue = new StatusFloatValue(startValue, signProtected);

            foreach (var effect in m_Floats.GetValuesForKey(id))
                statusFloatValue.ApplyEffect(effect.ValueModifier, effect.Stacks * effect.Value, effect.Priority, effect.Order);

            return statusFloatValue.GetValue();
        }

        /// <inheritdoc cref="Evaluate(ushort, float, bool)"/>
        public int Evaluate(ushort id, int startValue, bool signProtected)
        {
            var statusIntValue = new StatusIntValue(startValue, signProtected);

            foreach (var effect in m_Ints.GetValuesForKey(id))
                statusIntValue.ApplyEffect(effect.ValueModifier, effect.Stacks * effect.Value, effect.Priority, effect.Order);

            return statusIntValue.GetValue();
        }

        /// <inheritdoc cref="Evaluate(ushort, float, bool)"/>
        public bool Evaluate(ushort id, bool startValue)
        {
            var statusBoolValue = new StatusBoolValue(startValue);

            foreach (var effect in m_Bools.GetValuesForKey(id))
                statusBoolValue.ApplyEffect(effect.Value, effect.Priority, effect.Order);

            return statusBoolValue.GetValue();
        }

        public void Dispose()
        {
            m_TypeToIndexAndTypeInfo.Dispose();
            m_InstanceIdToStacksAndOrder.Dispose();
            m_Floats.Dispose();
            m_Ints.Dispose();
            m_Bools.Dispose();
        }
    }
}
