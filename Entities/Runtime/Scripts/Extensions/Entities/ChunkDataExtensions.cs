using Unity.Burst;
using Unity.Burst.CompilerServices;

namespace Unity.Entities
{
    [BurstCompile]
    public static unsafe class ChunkDataExtensions
    {
        public static int GetIndexInTypeArray(in ArchetypeChunk chunk, TypeIndex typeIndex) => ChunkDataUtility.GetIndexInTypeArray(chunk.Archetype.Archetype, typeIndex);

        public static void GetBufferWithTypeRW(in ArchetypeChunk chunk, int baseEntityIndex, int indexInTypeArray, uint globalSystemVersion, out byte* header, out byte* buffer, out int length)
        {
            if (Hint.Unlikely(indexInTypeArray < 0))
            {
                header = null;
                buffer = null;
                length = 0;
                return;
            }

            header = ChunkDataUtility.GetComponentDataWithTypeRW(chunk.m_Chunk, chunk.Archetype.Archetype, baseEntityIndex, indexInTypeArray, globalSystemVersion);
            var bufferHeader = (BufferHeader*)header;
            buffer = BufferHeader.GetElementPointer(bufferHeader);
            length = bufferHeader->Length;
        }
    }
}