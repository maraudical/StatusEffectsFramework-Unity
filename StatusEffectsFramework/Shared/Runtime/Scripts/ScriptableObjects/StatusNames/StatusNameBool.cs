using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// A <see cref="StatusName"/> for a <see cref="StatusBool"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "New Status Name Bool", menuName = "Status Effects Framework/Status Name/Bool", order = -1)]
    public class StatusNameBool : StatusName
    {
        public override ValueType ValueType => ValueType.Bool;
    }
}
