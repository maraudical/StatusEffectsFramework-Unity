using System.Collections;
using System.Linq;
using System;

namespace StatusEffects
{
    /// <summary>
    /// Specifies groups for status effects.
    /// </summary>
    [Serializable]
    public struct StatusEffectGroup
    {
        /// <summary>
        /// Returns the integer value of the group.
        /// </summary>
        public int Value;
        /// <summary>
        /// Given a set of group names, returns the equivalent 
        /// <see cref="int"/> value for all of them.
        /// </summary>
        public static int GetGroup(params string[] groupNames)
        {
            var groups = StatusEffectSettings.GetOrCreateSettings().Groups;

            BitArray bits = new BitArray(new[] { 0 });

            for (int i = 0; i < bits.Length; i++)
                bits[i] = groupNames.Contains(groups[i]);

            var result = new int[1];
            bits.CopyTo(result, 0);
            return result[0];
        }
        /// <summary>
        /// Given an <see cref="int"/> index, returns the name of the group as defined in the <see cref="StatusEffectSettings"/>.
        /// </summary>
        public static string IndexToName(int group)
        {
            var groups = StatusEffectSettings.GetOrCreateSettings().Groups;

            return groups.ElementAtOrDefault(group);
        }
        /// <summary>
        /// Given a group name, returns the <see cref="int"/> index as defined in the <see cref="StatusEffectSettings"/>.
        /// </summary>
        public static int NameToIndex(string groupName)
        {
            var groups = StatusEffectSettings.GetOrCreateSettings().Groups;

            return Array.IndexOf(groups, groupName);
        }

        public StatusEffectGroup(int value) { this.Value = value; }

        public static implicit operator int(StatusEffectGroup group) => group.Value;
        public static implicit operator StatusEffectGroup(int value) => new StatusEffectGroup(value);
    }
}
