namespace StatusEffects.Example
{
    public interface IExampleEntity
    {
        public float Health { get; set; }
        public float MaxHealth { get; }
        public float Speed { get; }
        public int CoinMultiplier { get; }
        public bool Stunned { get; }
    }
}