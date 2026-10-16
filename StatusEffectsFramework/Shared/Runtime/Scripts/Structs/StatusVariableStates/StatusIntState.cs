namespace StatusEffectsFramework
{
    /// <summary>
    /// A blittable struct to store values of a <see cref="StatusInt"/>.
    /// </summary>
    public struct StatusIntState : IStatusState<StatusIntState>
    {
        /// <summary>
        /// See <see cref="StatusInt.BaseValue"/>.
        /// </summary>
        public int BaseValue;
        /// <summary>
        /// See <see cref="StatusInt.SignProtected"/>.
        /// </summary>
        public bool SignProtected;

        public StatusIntState(int baseValue, bool signProtected)
        {
            BaseValue = baseValue;
            SignProtected = signProtected;
        }

        public void Serialize<TSerializer>(ref TSerializer serializer) where TSerializer : struct, IStatusSerializer
        {
            serializer.SerializeValue(ref BaseValue);
            serializer.SerializeValue(ref SignProtected);
        }

        public bool Equals(StatusIntState other) => BaseValue == other.BaseValue && SignProtected == other.SignProtected;
    }
}
