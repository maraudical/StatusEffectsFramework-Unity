using System;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffects.Example.UI
{
    public class StatusEffectUI : MonoBehaviour
    {
        [SerializeField] private Text m_Text;        
        [SerializeField] private Image m_Image;

        [NonSerialized] public int Stack;

        public void Initialize(Sprite sprite, int stacks)
        {
            m_Image.sprite = sprite;
            Stack = stacks;
            UpdateStack(0);
        }

        public void UpdateStack(int value)
        {
            Stack += value;

            m_Text.text = Stack.ToString();

            if (Stack > 1)
                m_Text.gameObject.SetActive(true);
            else
                m_Text.gameObject.SetActive(false);
        }
    }
}