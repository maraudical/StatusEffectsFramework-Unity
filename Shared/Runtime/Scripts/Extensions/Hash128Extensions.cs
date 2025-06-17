using System;
using UnityEngine;

namespace StatusEffects
{
    public static class Hash128Extensions
    {
#if ENTITIES
        public static Unity.Entities.Hash128 BurstId() => new(Guid.NewGuid().ToString("N"));

#endif
        public static Hash128 Id() => Hash128.Compute(Guid.NewGuid().ToString("N"));
    }
}