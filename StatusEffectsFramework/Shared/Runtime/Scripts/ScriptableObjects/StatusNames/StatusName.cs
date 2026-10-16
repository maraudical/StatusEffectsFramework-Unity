namespace StatusEffectsFramework
{
    /// <summary>
    /// Identifies a status that <see cref="Effect"/>s change. A status variable is changed by the effects whose
    /// <see cref="StatusName"/> has the same <see cref="Registrant.Key"/>. The subclasses decide the type of the value.
    /// </summary>
    public abstract class StatusName : Registrant { }
}
