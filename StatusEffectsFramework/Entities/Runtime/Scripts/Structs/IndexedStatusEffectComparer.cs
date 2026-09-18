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
            if (m_UseIndex 
                || !m_Registry.TryGetStatusEffectData(x.Id, out var xData) 
                || !m_Registry.TryGetStatusEffectData(y.Id, out var yData))
                return x.Index.CompareTo(y.Index);
            
            // Compare base value.
            int comparison = xData.Value.BaseValue.CompareTo(yData.Value.BaseValue);
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