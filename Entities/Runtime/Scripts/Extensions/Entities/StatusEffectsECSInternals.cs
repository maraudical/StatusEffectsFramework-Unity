using Unity.Burst;

namespace Unity.Entities
{
    [BurstCompile]
    public static unsafe class StatusEffectsECSInternals
    {
        public static int GetIndexInTypeArray(in ArchetypeChunk chunk, TypeIndex typeIndex) => ChunkDataUtility.GetIndexInTypeArray(chunk.Archetype.Archetype, typeIndex);

        public static bool TryGetBufferWithTypeRW(in ArchetypeChunk chunk, int baseEntityIndex, int indexInTypeArray, uint globalSystemVersion, out byte* header, out byte* buffer, out int length)
        {
            if (indexInTypeArray < 0)
            {
                header = null;
                buffer = null;
                length = 0;
                return false;
            }

            header = ChunkDataUtility.GetComponentDataWithTypeRW(chunk.m_Chunk, chunk.Archetype.Archetype, baseEntityIndex, indexInTypeArray, globalSystemVersion);
            var bufferHeader = (BufferHeader*)header;

            if (bufferHeader == null)
            {
                buffer = null;
                length = 0;
                return false;
            }

            buffer = BufferHeader.GetElementPointer(bufferHeader);
            length = bufferHeader->Length;
            return true;
        }
    }
}