namespace StatusEffectsFramework
{
    public struct StatusFloatState : IStatusState<StatusFloatState>
    {
        public float BaseValue;
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
