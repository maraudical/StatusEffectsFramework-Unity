#if ENTITIES
using System;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    public struct DynamicEffectInfo
    {
        public TypeIndex TypeIndex { get; internal set; }
        internal BlobArray<byte> Bytes;
        internal int Size;
        internal int InstanceIdOffset;
        internal int IdOffset;
        internal int ValueModifierOffset;
        internal int PostEvaluateOffset;
        internal int PriorityOffset;
        internal int ValueOffset;
        internal int StructOffset;

        /// <summary>
        /// Creates a new <see cref="DynamicEffectInfo"/> for the specified dynamic effect struct, allocating a copy of it
        /// to unmanaged memory.
        /// </summary>
        public static void AllocateDynamicEffect<T>(T dynamicEffectStruct, ValueType valueType, ref DynamicEffectInfo info, ref BlobBuilder builder) where T : unmanaged
        {
            Type type;
            switch (valueType)
            {
                case ValueType.Int:
                    type = typeof(DynamicInts<T>);
                    info.TypeIndex = TypeManager.GetTypeIndex<DynamicInts<T>>();
                    info.InstanceIdOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicInts<T>.InstanceId)));
                    info.IdOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicInts<T>.Id)));
                    info.ValueModifierOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicInts<T>.ValueModifier)));
                    info.PostEvaluateOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicInts<T>.PostEvaluate)));
                    info.PriorityOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicInts<T>.Priority)));
                    info.ValueOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicInts<T>.Value)));
                    info.StructOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicInts<T>.Struct)));
                    break;
                case ValueType.Bool:
                    type = typeof(DynamicBools<T>);
                    info.TypeIndex = TypeManager.GetTypeIndex<DynamicBools<T>>();
                    info.InstanceIdOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicBools<T>.InstanceId)));
                    info.IdOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicBools<T>.Id)));
                    info.PostEvaluateOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicBools<T>.PostEvaluate)));
                    info.PriorityOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicBools<T>.Priority)));
                    info.ValueOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicBools<T>.Value)));
                    info.StructOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicBools<T>.Struct)));
                    break;
                default:
                    type = typeof(DynamicFloats<T>);
                    info.TypeIndex = TypeManager.GetTypeIndex<DynamicFloats<T>>();
                    info.InstanceIdOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicFloats<T>.InstanceId)));
                    info.IdOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicFloats<T>.Id)));
                    info.ValueModifierOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicFloats<T>.ValueModifier)));
                    info.PostEvaluateOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicFloats<T>.PostEvaluate)));
                    info.PriorityOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicFloats<T>.Priority)));
                    info.ValueOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicFloats<T>.Value)));
                    info.StructOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(DynamicFloats<T>.Struct)));
                    break;
            }
            info.Size = UnsafeUtility.SizeOf<T>();

            BlobBuilderArray<byte> bytes = builder.Allocate(ref info.Bytes, info.Size);

            if (info.Size <= 0)
                return;

            ref byte firstByte = ref bytes[0];
            UnsafeUtility.As<byte, T>(ref firstByte) = dynamicEffectStruct;
        }
    }
}
#endif
