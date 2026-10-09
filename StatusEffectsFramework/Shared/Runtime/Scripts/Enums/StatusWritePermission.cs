namespace StatusEffectsFramework
{
    /// <summary>
    /// Who is allowed to change the base values of a networked status variable.
    /// </summary>
    public enum StatusWritePermission
    {
        /// <summary>
        /// Only the server or host.
        /// </summary>
        Server,
        /// <summary>
        /// The server or host, and the owner of the status manager.
        /// </summary>
        ServerAndOwner
    }
}
