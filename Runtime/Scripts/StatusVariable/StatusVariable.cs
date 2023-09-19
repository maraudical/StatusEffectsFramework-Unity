using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StatusEffects
{
    public abstract class StatusVariable
    {
        [StatusString] public string statusName;
        public HashSet<StatusEffect> statusEffectReferences { get; protected set; }

        public void AddEffectReference(StatusEffect statusEffect)
        {
            if (statusEffectReferences == null)
                statusEffectReferences = new HashSet<StatusEffect>();
            statusEffectReferences.Add(statusEffect);
            
            OnReferencesChanged();
        }

        public void RemoveEffectReference(StatusEffect statusEffect)
        {
            if (statusEffectReferences == null)
                statusEffectReferences = new HashSet<StatusEffect>();
            statusEffectReferences.Remove(statusEffect);
            
            OnReferencesChanged();
        }

        protected abstract void OnReferencesChanged();
    }
}
