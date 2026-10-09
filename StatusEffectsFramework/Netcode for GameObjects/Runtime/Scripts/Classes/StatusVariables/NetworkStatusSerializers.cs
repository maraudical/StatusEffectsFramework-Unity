using Unity.Netcode;

namespace StatusEffectsFramework.Netcode
{
    /// <summary>
    /// Writes a <see cref="IStatusState{T}"/> to a <see cref="FastBufferWriter"/>.
    /// </summary>
    public struct NetworkStatusWriter : IStatusSerializer
    {
        private FastBufferWriter m_Writer;

        public NetworkStatusWriter(FastBufferWriter writer) { m_Writer = writer; }

        public void SerializeValue(ref float value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref int value) => m_Writer.WriteValueSafe(value);
        public void SerializeValue(ref bool value) => m_Writer.WriteValueSafe(value);
    }

    /// <summary>
    /// Reads a <see cref="IStatusState{T}"/> from a <see cref="FastBufferReader"/>.
    /// </summary>
    public struct NetworkStatusReader : IStatusSerializer
    {
        private FastBufferReader m_Reader;

        public NetworkStatusReader(FastBufferReader reader) { m_Reader = reader; }

        public void SerializeValue(ref float value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref int value) => m_Reader.ReadValueSafe(out value);
        public void SerializeValue(ref bool value) => m_Reader.ReadValueSafe(out value);
    }
}
