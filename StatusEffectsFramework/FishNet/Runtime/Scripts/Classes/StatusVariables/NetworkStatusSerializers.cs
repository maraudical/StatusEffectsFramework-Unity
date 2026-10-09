using FishNet.Serializing;

namespace StatusEffectsFramework.FishNet
{
    /// <summary>
    /// Writes a <see cref="IStatusState{T}"/> to a FishNet <see cref="Writer"/>.
    /// </summary>
    public struct NetworkStatusWriter : IStatusSerializer
    {
        private readonly Writer m_Writer;

        public NetworkStatusWriter(Writer writer) { m_Writer = writer; }

        public void SerializeValue(ref float value) => m_Writer.WriteSingle(value);
        public void SerializeValue(ref int value) => m_Writer.WriteInt32(value);
        public void SerializeValue(ref bool value) => m_Writer.WriteBoolean(value);
    }

    /// <summary>
    /// Reads a <see cref="IStatusState{T}"/> from a FishNet <see cref="Reader"/>.
    /// </summary>
    public struct NetworkStatusReader : IStatusSerializer
    {
        private readonly Reader m_Reader;

        public NetworkStatusReader(Reader reader) { m_Reader = reader; }

        public void SerializeValue(ref float value) => value = m_Reader.ReadSingle();
        public void SerializeValue(ref int value) => value = m_Reader.ReadInt32();
        public void SerializeValue(ref bool value) => value = m_Reader.ReadBoolean();
    }
}
