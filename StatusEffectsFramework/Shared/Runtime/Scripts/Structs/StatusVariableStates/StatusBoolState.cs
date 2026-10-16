namespace StatusEffectsFramework
{
    /// <summary>
    /// A blittable struct to store values of a <see cref="StatusBool"/>.
    /// </summary>
    public struct StatusBoolState : IStatusState<StatusBoolState>
    {
        /// <summary>
        /// See <see cref="StatusBool.BaseValue"/>.
        /// </summary>
        public bool BaseValue;

        public StatusBoolState(bool baseValue)
        {
            BaseValue = baseValue;
        }

        public void Serialize<TSerializer>(ref TSerializer serializer) where TSerializer : struct, IStatusSerializer
        {
            serializer.SerializeValue(ref BaseValue);
        }

        public bool Equals(StatusBoolState other) => BaseValue == other.BaseValue;
    }
}
