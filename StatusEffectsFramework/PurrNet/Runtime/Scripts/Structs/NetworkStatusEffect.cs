namespace StatusEffectsFramework.PurrNet
{
    /// <summary>
    /// Captures a <see cref="StatusEffect"/> as values that PurrNet can pack automatically.
    /// </summary>
    public struct NetworkStatusEffect
    {
        /// <summary>
        /// The id of the <see cref="StatusEffectData"/> in the <see cref="StatusRegistry"/>.
        /// </summary>
        public ushort Id;
        public StatusEffectTiming Timing;
        /// <summary>
        /// The <see cref="StatusEffect.Duration"/>. For <see cref="StatusEffectTiming.Duration"/> this is the
        /// total duration, measured from <see cref="ServerTimeAdded"/>.
        /// </summary>
        public float Duration;
        public int Stacks;
        public uint InstanceId;
        /// <summary>
        /// When the status effect started counting down, in seconds of synced ticks.
        /// </summary>
        public double ServerTimeAdded;

        public NetworkStatusEffect(ushort id, StatusEffectTiming timing, float duration, int stacks, uint instanceId, double serverTimeAdded)
        {
            Id = id;
            Timing = timing;
            Duration = duration;
            Stacks = stacks;
            InstanceId = instanceId;
            ServerTimeAdded = serverTimeAdded;
        }
    }
}
