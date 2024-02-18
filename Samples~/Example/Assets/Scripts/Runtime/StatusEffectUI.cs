using UnityEngine;
using UnityEngine.UI;

namespace StatusEffects.UI
{
    public class StatusEffectUI : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField] private Image _image;

        public int stack;

        public void Initialize(Sprite sprite, int stacks)
        {
            _image.sprite = sprite;
            stack = stacks;
            UpdateStack(0);
        }

        public void UpdateStack(int value)
        {
            stack += value;

            _text.text = stack.ToString();

            if (stack > 1)
                _text.gameObject.SetActive(true);
            else
                _text.gameObject.SetActive(false);
        }
    }
}