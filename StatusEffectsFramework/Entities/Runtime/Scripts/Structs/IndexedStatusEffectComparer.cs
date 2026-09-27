#if ENTITIES
using System.Collections.Generic;

namespace StatusEffectsFramework.Entities
{
    internal struct IndexedStatusEffectComparer : IComparer<IndexedStatusEffects>
    {
        private bool m_UseIndex;

        public int Compare(IndexedStatusEffects x, IndexedStatusEffects y)
        {
            if (m_UseIndex)
                return x.Index.CompareTo(y.Index);

            // Compare timing rank.
            int comparison = TimingRank(x.Timing).CompareTo(TimingRank(y.Timing));
            if (comparison != 0)
                return comparison;
            // Then compare base value.
            comparison = x.BaseValue.CompareTo(y.BaseValue);
            if (comparison != 0)
                return comparison;
            // Then compare remaining time. Events counting down on different ids can't be
            // compared and infinite and predicate effects have no meaningful time left.
            if (x.Timing is StatusEffectTiming.Duration || (x.Timing is StatusEffectTiming.Event && x.EventId == y.EventId))
            {
                comparison = x.TimeRemaining.CompareTo(y.TimeRemaining);
                if (comparison != 0)
                    return comparison;
            }
            // Order oldest first so the newest is kept.
            return x.Index.CompareTo(y.Index);
        }

        /// <summary>
        /// Ranks how long an effect lasts regardless of its duration:
        /// Infinite > Predicate > Event > Duration.
        /// </summary>
        internal static int TimingRank(StatusEffectTiming timing) => timing switch
        {
            StatusEffectTiming.Infinite => 3,
            StatusEffectTiming.Predicate => 2,
            StatusEffectTiming.Event => 1,
            _ => 0,
        };

        public IndexedStatusEffectComparer(bool useIndex)
        {
            m_UseIndex = useIndex;
        }
    }
}
#endif
