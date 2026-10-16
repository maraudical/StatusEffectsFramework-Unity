using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// A name that several <see cref="StatusEffectData"/>s can share so they count as the same kind of effect. For example, a
    /// non-stacking "Poison" replaces any other effect named "Poison", and all of them can be found or removed together.
    /// </summary>
    [CreateAssetMenu(fileName = "New Comparable Name", menuName = "Status Effects Framework/Comparable Name", order = -4)]
    public class ComparableName : Registrant { }
}
