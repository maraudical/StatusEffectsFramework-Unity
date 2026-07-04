using System;
using Unity.Burst;
using Unity.Burst.CompilerServices;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    [BurstCompile]
    public static unsafe class StatusEffectsECSInternals
    {
        public static int GetIndexInTypeArray(in ArchetypeChunk chunk, TypeIndex typeIndex) => ChunkDataUtility.GetIndexInTypeArray(chunk.Archetype.Archetype, typeIndex);

        public static void SetChangeVersion(in ArchetypeChunk chunk, int indexInTypeArray, uint globalSystemVersion) => chunk.Archetype.Archetype->Chunks.SetChangeVersion(indexInTypeArray, chunk.m_Chunk.ListIndex, globalSystemVersion);

        public static byte* GetComponentDataWithTypeRO(in ArchetypeChunk chunk, int baseEntityIndex, int indexInTypeArray) => ChunkDataUtility.GetComponentDataRO(chunk.m_Chunk, chunk.Archetype.Archetype, baseEntityIndex, indexInTypeArray);

        public static byte* GetComponentDataWithTypeRW(in ArchetypeChunk chunk, int baseEntityIndex, int indexInTypeArray, uint globalSystemVersion) => ChunkDataUtility.GetComponentDataRW(chunk.m_Chunk, chunk.Archetype.Archetype, baseEntityIndex, indexInTypeArray, globalSystemVersion);

        public static bool TryGetElementPointerAndLength(byte* header, out byte* buffer, out int length)
        {
            var bufferHeader = (BufferHeader*)header;

            if (Hint.Unlikely(bufferHeader == null))
            {
                buffer = null;
                length = 0;
                return false;
            }
            
            buffer = BufferHeader.GetElementPointer(bufferHeader);
            length = bufferHeader->Length;
            return true;
        }

        public static bool IsEmpty(in ArchetypeChunk chunk, int baseEntityIndex, int indexInTypeArray)
        {
            var bufferHeader = (BufferHeader*)ChunkDataUtility.GetComponentDataRO(chunk.m_Chunk, chunk.Archetype.Archetype, baseEntityIndex, indexInTypeArray);

            if (Hint.Unlikely(bufferHeader == null))
                throw new InvalidOperationException("Invalid pointer to buffer header.");

            return bufferHeader->Length > 0;
        }

        public static ref int LengthAsRef(byte* header)
        {
            var bufferHeader = (BufferHeader*)header;

            if (Hint.Unlikely(bufferHeader == null))
                throw new InvalidOperationException("Invalid pointer to buffer header.");

            return ref bufferHeader->Length;
        }

        public static void EnsureCapacity(byte* header, int count, int typeSize, int alignment)
        {
            var bufferHeader = (BufferHeader*)header;

            if (Hint.Unlikely(bufferHeader == null))
                throw new InvalidOperationException("Invalid pointer to buffer header.");

            BufferHeader.EnsureCapacity(bufferHeader, count, typeSize, alignment, BufferHeader.TrashMode.RetainOldData, false, 0);
        }

        public static unsafe void AppendToBuffer(ref EntityCommandBuffer.ParallelWriter ecb, int sortKey, Entity e, ComponentType componentType, int typeSize, void* value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS || UNITY_DOTS_DEBUG
            if (Hint.Unlikely(e == Entity.Null))
                throw new InvalidOperationException("Invalid Entity.Null passed. ECBCommand.AppendToBufferCommand");
#endif
            var data = ecb.m_Data;
            var chain = (ecb.m_ThreadIndex >= 0) ? &data->m_ThreadedChains[ecb.m_ThreadIndex] : &data->m_MainThreadChain;
            // NOTE: This has to be sizeof not TypeManager.SizeInChunk since we use UnsafeUtility.CopyStructureToPtr
            //       even on zero size components.
            var sizeNeeded = EntityCommandBufferData.Align(sizeof(EntityComponentCommand) + typeSize, EntityCommandBufferData.ALIGN_64_BIT);

            data->ResetCommandBatching(chain);
            var cmd = (EntityComponentCommand*)data->Reserve(chain, sortKey, sizeNeeded);

            cmd->Header.Header.CommandType = ECBCommand.AppendToBuffer;
            cmd->Header.Header.TotalSize = sizeNeeded;
            cmd->Header.Header.SortKey = chain->m_LastSortKey;
            cmd->Header.Entity = e;
            cmd->Header.IdentityIndex = 0;
            cmd->Header.BatchCount = 1;
            cmd->ComponentTypeIndex = componentType.TypeIndex;
            cmd->ComponentSize = (short)typeSize;
            byte* componentValue = (byte*)(cmd + 1);
            UnsafeUtility.MemCpy(componentValue, value, typeSize);
            cmd->ValueRequiresEntityFixup = componentType.HasEntityReferences ? (byte)1 : (byte)0;
        }

        public static bool RemoveAtSwapBack(byte* header, int typeSize, int index)
        {
            var bufferHeader = (BufferHeader*)header;

            if (Hint.Unlikely(bufferHeader == null))
                return false;

            var buffer = BufferHeader.GetElementPointer(bufferHeader);
            ref var length = ref bufferHeader->Length;
            
            if (Hint.Unlikely(length <= index))
                throw new IndexOutOfRangeException($"Value for index {index} is out of bounds.");

            length--;

            var copyTo = buffer + index * typeSize;
            var copyFrom = buffer + length * typeSize;

            UnsafeUtility.MemCpy(copyTo, copyFrom, typeSize);

            return true;
        }
    }
}