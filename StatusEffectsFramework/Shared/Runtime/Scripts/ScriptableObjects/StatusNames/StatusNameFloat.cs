using UnityEngine;

namespace StatusEffectsFramework
{
    /// <summary>
    /// A <see cref="StatusName"/> for a <see cref="StatusFloat"/>.
    /// </summary>
    [CreateAssetMenu(fileName = "New Status Name Float", menuName = "Status Effects Framework/Status Name/Float", order = -3)]
    public class StatusNameFloat : StatusName
    {
        public override ValueType ValueType => ValueType.Float;
    }
}
