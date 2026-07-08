using UnityEngine;

namespace StatusEffectsFramework
{
    public abstract class DynamicEffect : ScriptableObject 
    {
        public abstract bool PostEvaluate { get; }
    }
}
