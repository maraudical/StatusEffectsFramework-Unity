using System;
using UnityEngine;
using UnityEngine.UI;

namespace StatusEffects.Example.UI
{
    public class StatusEffectUI : MonoBehaviour
    {
        [SerializeField] private Text m_Text;        
        [SerializeField] private Image m_Image;

        [NonSerialized] public int Stacks;

        public void Initialize(Sprite sprite, int stacks)
        {
            m_Image.sprite = sprite;
            Stacks = stacks;
            UpdateStack(0);
        }

        public void UpdateStack(int value)
        {
            Stacks = value;

            m_Text.text = Stacks.ToString();

            if (Stacks > 1)
                m_Text.gameObject.SetActive(true);
            else
                m_Text.gameObject.SetActive(false);
        }
    }
}