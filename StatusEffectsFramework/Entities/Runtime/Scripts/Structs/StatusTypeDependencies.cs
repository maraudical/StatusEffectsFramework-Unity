#if ENTITIES
using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Registers dynamic effect or module buffer types with a system so the job system tracks
    /// dependencies for buffers that are accessed through raw pointers. Re-registers when the
    /// <see cref="UnmanagedStatusRegistry"/> version changes.
    /// </summary>
    internal struct StatusTypeDependencies
    {
        private ushort m_Version;

        public void Register(ref SystemState state, ushort version, ref BlobArray<TypeIndex> types, bool isReadOnly)
        {
            if (m_Version == version)
                return;

            m_Version = version;

            StatusEffectsECSInternals.AddDependencies(ref state, ref types, isReadOnly);
        }
    }
}
#endif