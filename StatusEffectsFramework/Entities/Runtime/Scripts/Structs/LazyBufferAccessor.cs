using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Reads a chunk's buffers read-only and only takes them read-write the first time a write is needed.
    /// Taking a buffer read-write bumps the chunk's change version for that type, so chunks that are only
    /// read don't show up as changed to change filters.
    /// </summary>
    /// <remarks>
    /// Create one per chunk at the top of <c>IJobChunk.Execute</c> and keep it in a local. A copy doesn't
    /// see write access taken through the original and takes it again, which is safe but wasted work.
    /// </remarks>
    public struct LazyBufferAccessor<T> where T : unmanaged, IBufferElementData
    {
        private ArchetypeChunk m_Chunk;
        private BufferTypeHandle<T> m_Handle;
        private BufferAccessor<T> m_ReadOnly;
        private BufferAccessor<T> m_ReadWrite;

        /// <summary>
        /// True once the buffers have been taken read-write and the chunk's change version bumped.
        /// </summary>
        public bool HasWriteAccess { get; private set; }

        public LazyBufferAccessor(in ArchetypeChunk chunk, ref BufferTypeHandle<T> handle)
        {
            m_Chunk = chunk;
            m_Handle = handle;
            m_ReadOnly = chunk.GetBufferAccessorRO(ref handle);
            m_ReadWrite = default;
            HasWriteAccess = false;
        }

        /// <summary>
        /// Gets the buffer of the entity at <paramref name="index"/> without bumping the change version.
        /// </summary>
        public DynamicBuffer<T> GetReadOnly(int index) => HasWriteAccess ? m_ReadWrite[index] : m_ReadOnly[index];

        /// <summary>
        /// Gets the buffer of the entity at <paramref name="index"/> for writing. The first call per chunk
        /// takes the buffers read-write, which bumps the chunk's change version.
        /// </summary>
        public DynamicBuffer<T> GetReadWrite(int index)
        {
            if (!HasWriteAccess)
            {
                m_ReadWrite = m_Chunk.GetBufferAccessorRW(ref m_Handle);
                HasWriteAccess = true;
            }

            return m_ReadWrite[index];
        }
    }
}
