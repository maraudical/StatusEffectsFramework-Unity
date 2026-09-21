#if ENTITIES
using System.Collections.Generic;

namespace StatusEffectsFramework.Entities
{
    internal struct IndexedStatusEffectComparer : IComparer<IndexedStatusEffects>
    {
        private UnmanagedStatusRegistry m_Registry;
        private bool m_UseIndex;

        public int Compare(IndexedStatusEffects x, IndexedStatusEffects y)
        {
            if (m_UseIndex)
                return x.Index.CompareTo(y.Index);
            
            // Compare base value.
            int comparison = m_Registry.GetStatusEffectData(x.Id).BaseValue.CompareTo(m_Registry.GetStatusEffectData(y.Id).BaseValue);
            if (comparison != 0)
                return comparison;
            // Then compare duration.
            return (x.Timing is StatusEffectTiming.Infinite or StatusEffectTiming.Predicate ? float.PositiveInfinity : x.Duration).CompareTo(y.Timing is StatusEffectTiming.Infinite or StatusEffectTiming.Predicate ? float.PositiveInfinity : y.Duration);
        }

        public IndexedStatusEffectComparer(UnmanagedStatusRegistry registry, bool useIndex)
        {
            m_Registry = registry;
            m_UseIndex = useIndex;
        }
    }
}
#endif