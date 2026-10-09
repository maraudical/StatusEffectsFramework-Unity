namespace StatusEffectsFramework
{
    /// <summary>
    /// A networking library agnostic reader and writer. A <see cref="IStatusState{T}"/> describes its layout once
    /// through this, and each networking library supplies a struct that reads or writes it. When writing, the
    /// value is sent. When reading, the value is filled in.
    /// </summary>
    public interface IStatusSerializer
    {
        void SerializeValue(ref float value);
        void SerializeValue(ref int value);
        void SerializeValue(ref bool value);
    }
}
