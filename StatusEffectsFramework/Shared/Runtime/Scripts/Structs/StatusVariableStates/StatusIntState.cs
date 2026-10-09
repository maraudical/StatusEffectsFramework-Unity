namespace StatusEffectsFramework
{
    public struct StatusIntState : IStatusState<StatusIntState>
    {
        public int BaseValue;
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
