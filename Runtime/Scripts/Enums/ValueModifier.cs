namespace StatusEffects
{
    public enum ValueModifier
    {
        Additive = 0,
        Multiplicative = 1 << 1,
        PostAdditive = 1 << 2,
        Maximum = 1 << 3,
        Minimum = 1 << 4,
        Overwrite = 1 << 5
    }
}
