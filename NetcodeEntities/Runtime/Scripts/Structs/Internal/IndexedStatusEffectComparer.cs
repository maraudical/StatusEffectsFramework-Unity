#if ENTITIES && ADDRESSABLES
using System;
using System.Collections.Generic;

namespace StatusEffects.NetCode.Entities
{
    internal struct IndexedStatusEffectComparer : IComparer<IndexedStatusEffect>
    {
        private bool m_UseIndex;

        public int Compare(IndexedStatusEffect x, IndexedStatusEffect y)
        {
            if (m_UseIndex)
                return x.Index.CompareTo(y.Index);
            // Compare base value.
            int comparison = x.Data.Value.BaseValue.CompareTo(y.Data.Value.BaseValue);
            if (comparison != 0)
                return comparison;
            // Then compare duration.
            return (x.Timing is StatusEffectTiming.Infinite or StatusEffectTiming.Predicate ? float.PositiveInfinity : x.Duration).CompareTo(y.Timing is StatusEffectTiming.Infinite or StatusEffectTiming.Predicate ? float.PositiveInfinity : y.Duration);
        }

        public IndexedStatusEffectComparer(bool useIndex)
        {
            m_UseIndex = useIndex;
        }
    }
}
#endif