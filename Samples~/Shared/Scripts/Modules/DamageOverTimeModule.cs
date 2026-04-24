using UnityEngine;

namespace StatusEffectFramework.Samples
{
    [CreateAssetMenu(fileName = "Damage Over Time Module", menuName = "Status Effect Framework/Modules/Damage Over Time", order = 1)]
    [AttachModuleInstance(typeof(DamageOverTimeInstance))]
    public partial class DamageOverTimeModule : Module { }
}