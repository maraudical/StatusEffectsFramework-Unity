using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Hash128 = UnityEngine.Hash128;

namespace StatusEffectsFramework
{
    /// <summary>
    /// What a server tells each client about its <see cref="StatusRegistry"/> before gameplay, so both assign the same ids.
    /// It holds no strings or references to any networking library: send <see cref="ToBytes"/> with whatever the game
    /// uses, read it back with <see cref="TryFromBytes"/>, then call <see cref="StatusRegistry.TryApplyPayload"/>.
    /// </summary>
    [Serializable]
    public struct StatusRegistryPayload
    {
        private const int k_HashSize = 16;

        /// <summary>
        /// One per set of registrants the server has registered: the main registry first, then each
        /// <see cref="StatusRegistryManifest"/>. Each is the hash of that set's keys.
        /// </summary>
        public Hash128[] ManifestHashes;

        public StatusRegistryPayload(Hash128[] manifestHashes)
        {
            ManifestHashes = manifestHashes;
        }

        /// <summary>
        /// Writes the count, then 16 bytes per hash.
        /// </summary>
        public byte[] ToBytes()
        {
            var hashes = ManifestHashes ?? Array.Empty<Hash128>();
            var bytes = new byte[sizeof(int) + hashes.Length * k_HashSize];
            BinaryPrimitives.WriteInt32LittleEndian(bytes, hashes.Length);
            MemoryMarshal.AsBytes(hashes.AsSpan()).CopyTo(bytes.AsSpan(sizeof(int)));
            return bytes;
        }

        /// <summary>
        /// Reads what <see cref="ToBytes"/> wrote. Returns false when the bytes aren't a whole payload.
        /// </summary>
        public static bool TryFromBytes(ReadOnlySpan<byte> bytes, out StatusRegistryPayload payload)
        {
            payload = default;
            if (bytes.Length < sizeof(int))
                return false;

            int count = BinaryPrimitives.ReadInt32LittleEndian(bytes);
            if (count < 0 || bytes.Length != sizeof(int) + (long)count * k_HashSize)
                return false;

            payload.ManifestHashes = MemoryMarshal.Cast<byte, Hash128>(bytes.Slice(sizeof(int))).ToArray();
            return true;
        }
    }
}
