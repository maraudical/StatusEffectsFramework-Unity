namespace StatusEffectsFramework
{
    /// <summary>
    /// A blittable struct to store values of a <see cref="StatusFloat"/>.
    /// </summary>
    public struct StatusFloatState : IStatusState<StatusFloatState>
    {
        /// <summary>
        /// See <see cref="StatusFloat.BaseValue"/>.
        /// </summary>
        public float BaseValue;
        /// <summary>
        /// See <see cref="StatusFloat.SignProtected"/>.
        /// </summary>
        public bool SignProtected;

        public StatusFloatState(float baseValue, bool signProtected)
        {
            BaseValue = baseValue;
            SignProtected = signProtected;
        }

        public void Serialize<TSerializer>(ref TSerializer serializer) where TSerializer : struct, IStatusSerializer
        {
            serializer.SerializeValue(ref BaseValue);
            serializer.SerializeValue(ref SignProtected);
        }

        public bool Equals(StatusFloatState other) => BaseValue == other.BaseValue && SignProtected == other.SignProtected;
    }
}
