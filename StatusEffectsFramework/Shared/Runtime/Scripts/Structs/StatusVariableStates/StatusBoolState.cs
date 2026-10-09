namespace StatusEffectsFramework
{
    public struct StatusBoolState : IStatusState<StatusBoolState>
    {
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
