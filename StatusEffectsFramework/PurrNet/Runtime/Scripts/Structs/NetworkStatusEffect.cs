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
        /// total duration, measured from when the status effect was added.
        /// </summary>
        public float Duration;
        public int Stacks;
        public uint InstanceId;
        /// <summary>
        /// Seconds that had passed on the server since the status effect was added, when this was sent.
        /// </summary>
        public float Elapsed;

        public NetworkStatusEffect(ushort id, StatusEffectTiming timing, float duration, int stacks, uint instanceId, float elapsed)
        {
            Id = id;
            Timing = timing;
            Duration = duration;
            Stacks = stacks;
            InstanceId = instanceId;
            Elapsed = elapsed;
        }
    }
}
