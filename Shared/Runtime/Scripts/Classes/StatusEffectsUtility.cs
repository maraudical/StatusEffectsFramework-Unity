using System;
using UnityEngine;

namespace StatusEffectFramework
{
    public static class StatusEffectsUtility
    {
        public static Hash128 GenerateId() => Hash128.Compute(Guid.NewGuid().ToString("N"));
    }
}