using Mirror;

namespace StatusEffectsFramework.Mirror
{
    /// <summary>
    /// Writes a <see cref="IStatusState{T}"/> to a Mirror <see cref="NetworkWriter"/>.
    /// </summary>
    public struct NetworkStatusWriter : IStatusSerializer
    {
        private readonly NetworkWriter m_Writer;

        public NetworkStatusWriter(NetworkWriter writer) { m_Writer = writer; }

        public void SerializeValue(ref float value) => m_Writer.WriteFloat(value);
        public void SerializeValue(ref int value) => m_Writer.WriteInt(value);
        public void SerializeValue(ref bool value) => m_Writer.WriteBool(value);
    }

    /// <summary>
    /// Reads a <see cref="IStatusState{T}"/> from a Mirror <see cref="NetworkReader"/>.
    /// </summary>
    public struct NetworkStatusReader : IStatusSerializer
    {
        private readonly NetworkReader m_Reader;

        public NetworkStatusReader(NetworkReader reader) { m_Reader = reader; }

        public void SerializeValue(ref float value) => value = m_Reader.ReadFloat();
        public void SerializeValue(ref int value) => value = m_Reader.ReadInt();
        public void SerializeValue(ref bool value) => value = m_Reader.ReadBool();
    }
}
