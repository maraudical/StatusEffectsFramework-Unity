#if ENTITIES
using System.Collections.Generic;

namespace StatusEffectFramework.Entities
{
    internal struct IndexedStatusEffectComparer : IComparer<IndexedStatusEffects>
    {
        private StatusReferences m_References;
        private bool m_UseIndex;

        public int Compare(IndexedStatusEffects x, IndexedStatusEffects y)
        {
            if (m_UseIndex)
                return x.Index.CompareTo(y.Index);

            ref UnmanagedStatusEffectData dataX = ref m_References.IdToStatusEffectDataMap.Value[x.StatusEffectDataId].Value;
            ref UnmanagedStatusEffectData dataY = ref m_References.IdToStatusEffectDataMap.Value[y.StatusEffectDataId].Value;
            // Compare base value.
            int comparison = dataX.BaseValue.CompareTo(dataY.BaseValue);
            if (comparison != 0)
                return comparison;
            // Then compare duration.
            return (x.Timing is StatusEffectTiming.Infinite or StatusEffectTiming.Predicate ? float.PositiveInfinity : x.Duration).CompareTo(y.Timing is StatusEffectTiming.Infinite or StatusEffectTiming.Predicate ? float.PositiveInfinity : y.Duration);
        }

        public IndexedStatusEffectComparer(StatusReferences references, bool useIndex)
        {
            m_References = references;
            m_UseIndex = useIndex;
        }
    }
}
#endif