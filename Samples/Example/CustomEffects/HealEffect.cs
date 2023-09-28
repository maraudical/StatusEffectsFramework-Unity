using System.Collections;
using UnityEngine;

namespace StatusEffects.Custom
{
    [CreateAssetMenu(fileName = "Heal Effect", menuName = "Custom Effects/Heal", order = 1)]
    public class HealEffect : CustomEffect
    {
        public override IEnumerator Effect<T>(T monoBehaviour, StatusEffect statusEffect)
        {
            Entity entity = monoBehaviour.GetComponent<Entity>();

            entity.health += statusEffect.data.baseValue;

            yield return null;
        }

        public override void EffectEnd<T>(T monoBehaviour, StatusEffect statusEffect)
        {
            Entity entity = monoBehaviour.GetComponent<Entity>();

            entity.health = Mathf.Min(entity.health, entity.maxHealth);                         
        }
    }
}
