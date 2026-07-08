using System;
using UnityEngine;

namespace StatusEffectsFramework
{
    public static class StatusEffectsUtility
    {
        public static Hash128 GenerateId() => Hash128.Compute(Guid.NewGuid().ToString("N"));
    }
}