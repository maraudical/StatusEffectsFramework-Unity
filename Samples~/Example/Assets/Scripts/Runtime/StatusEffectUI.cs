using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffects.UI
{
    public class StatusEffectUI : MonoBehaviour
    {
        [SerializeField] private Text _text;
        [SerializeField] private Image _image;

        public int stack;

        public void Initialize(Sprite sprite)
        {
            _image.sprite = sprite;
            stack = 0;
            UpdateStack(1);
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