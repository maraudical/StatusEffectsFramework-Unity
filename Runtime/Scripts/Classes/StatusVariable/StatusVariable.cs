using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace StatusEffects
{
    public abstract class StatusVariable
    {
        [StatusString] public string statusName;
        protected IStatus monoBehaviour;

        public void UpdateReferences(IStatus monoBehaviour)
        {
            this.monoBehaviour = monoBehaviour;
            
            OnReferencesChanged();
        }

        protected abstract void OnReferencesChanged();
    }
}
