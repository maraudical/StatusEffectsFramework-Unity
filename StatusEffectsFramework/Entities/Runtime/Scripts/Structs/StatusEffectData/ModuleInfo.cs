#if ENTITIES
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    public struct ModuleInfo
    {
        public TypeIndex TypeIndex { get; internal set; }
        internal BlobArray<byte> Bytes;
        internal int Size;

        // Field offsets within the Modules<T> buffer element. These are stored per module since
        // the offset of Struct depends on the alignment of T.
        internal int IdOffset;
        internal int StructOffset;

        /// <summary>
        /// Creates a new <see cref="ModuleInfo"/> for the specified module struct, allocating a copy of it
        /// to unmanaged memory and associating it with the module's type.
        /// </summary>
        public static void AllocateModule<T>(T moduleStruct, ref ModuleInfo info, ref BlobBuilder builder) where T : unmanaged
        {
            info.TypeIndex = TypeManager.GetTypeIndex<Modules<T>>();
            info.Size = UnsafeUtility.SizeOf<T>();

            var type = typeof(Modules<T>);
            info.IdOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(Modules<T>.Id)));
            info.StructOffset = UnsafeUtility.GetFieldOffset(type.GetField(nameof(Modules<T>.Struct)));

            BlobBuilderArray<byte> bytes = builder.Allocate(ref info.Bytes, info.Size);

            if (info.Size <= 0)
                return;

            ref byte firstByte = ref bytes[0];
            UnsafeUtility.As<byte, T>(ref firstByte) = moduleStruct;
        }
    }
}
#endif
