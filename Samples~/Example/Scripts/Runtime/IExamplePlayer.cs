namespace StatusEffects.Example
{
    public interface IExamplePlayer
    {
        public float Health { get; set; }
        public float MaxHealth { get; }
        public float Speed { get; }
        public int CoinMultiplier { get; }
        public bool Stunned { get; }
        public StatusEffectData StatusEffectData { get; set; }

        public void DebugAddStatusEffect();
        public void DebugAddStatusEffectTimed();
        public void DebugAddStatusEffectTimedEvent();
        public void InvokeEvent();
        public void DebugAddStatusEffectPredicate();
        public void DebugRemoveStatusEffect();
        public void DebugRemoveStatusEffectGroup();
    }
}