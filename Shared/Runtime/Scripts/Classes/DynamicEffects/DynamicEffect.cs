using UnityEngine;

namespace StatusEffectFramework
{
    public abstract class DynamicEffect : ScriptableObject 
    {
        public bool PostEvaluate => m_PostEvaluate;
        [SerializeField]
        private bool m_PostEvaluate;
    }
}
