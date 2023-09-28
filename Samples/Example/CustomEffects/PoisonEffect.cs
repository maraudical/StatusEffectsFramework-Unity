using System.Collections;
using UnityEngine;

namespace StatusEffects.Custom
{
    [CreateAssetMenu(fileName = "Poison Effect", menuName = "Custom Effects/Poison", order = 1)]
    public class PoisonEffect : CustomEffect
    {
        [SerializeField] private float intervalSeconds = 1f;

        public override IEnumerator Effect<T>(T monoBehaviour, StatusEffect statusEffect)
        {
            Entity entity = monoBehaviour.GetComponent<Entity>();

            for (; ; )
            {
                entity.health -= statusEffect.data.baseValue;
                yield return new WaitForSeconds(intervalSeconds);
            }
        }

        public override void EffectEnd<T>(T monoBehaviour, StatusEffect statusEffect) { }
    }
}
