using UnityEngine;

namespace StatusEffectsFramework.Samples
{
    [CreateAssetMenu(fileName = "Damage Over Time Module", menuName = "Status Effects Framework/Modules/Damage Over Time", order = 1)]
    [AttachModuleInstance(typeof(DamageOverTimeInstance))]
    public partial class DamageOverTimeModule : Module { }
}