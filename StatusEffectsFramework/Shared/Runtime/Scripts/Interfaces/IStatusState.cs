using System;

namespace StatusEffectsFramework
{
    /// <summary>
    /// The blittable part of a <see cref="StatusVariable{TState}"/> that needs to be replicated over the network.
    /// </summary>
    public interface IStatusState<T> : IEquatable<T> where T : struct
    {
        void Serialize<TSerializer>(ref TSerializer serializer) where TSerializer : struct, IStatusSerializer;
    }
}
