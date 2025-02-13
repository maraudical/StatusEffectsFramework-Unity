#if ENTITIES
using System.Collections.Generic;

namespace StatusEffects.Entities
{
    internal struct IndexedStatusEffectComparer : IComparer<IndexedStatusEffect>
    {
        private StatusReferences m_References;
        private bool m_UseIndex;

        public int Compare(IndexedStatusEffect x, IndexedStatusEffect y)
        {
            if (m_UseIndex)
                return x.Index.CompareTo(y.Index);

            ref StatusEffectData dataX = ref m_References.BlobAsset.Value[x.Id].Value;
            ref StatusEffectData dataY = ref m_References.BlobAsset.Value[y.Id].Value;
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