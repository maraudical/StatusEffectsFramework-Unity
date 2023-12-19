namespace StatusEffects
{
    public abstract class StatusVariable
    {
        protected IStatus monoBehaviour;
#if UNITY_EDITOR
        public void UpdateReferences(IStatus monoBehaviour)
        {
            this.monoBehaviour = monoBehaviour;
            
            OnReferencesChanged();
        }

        protected abstract void OnReferencesChanged();
#endif
    }
}
