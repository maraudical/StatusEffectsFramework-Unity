using Unity.Entities;

namespace StatusEffectsFramework.Entities
{
    /// <summary>
    /// Registers dynamic effect or module buffer types with a system so the job system tracks
    /// dependencies for buffers that are accessed through raw pointers. Re-registers when the
    /// <see cref="UnmanagedStatusRegistry"/> version changes.
    /// </summary>
    /// <remarks>
    /// Types are only ever added to a system's dependency list, never removed. A type that drops out of the
    /// registry after a rebuild stays registered, which is harmless: it only adds an unnecessary dependency.
    /// </remarks>
    internal struct StatusTypeDependencies
    {
        private ushort m_Version;

        public void Register(ref SystemState state, ushort version, ref BlobArray<TypeIndex> types, bool isReadOnly)
        {
            if (m_Version == version)
                return;

            m_Version = version;

            StatusEffectsUtility.AddDependencies(ref state, ref types, isReadOnly);
        }
    }
}