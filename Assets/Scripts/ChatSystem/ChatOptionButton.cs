using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ChatSystem
{
    /// <summary>
    /// 挂载在单个选项按钮 Prefab 上的脚本。
    /// </summary>
    public class ChatOptionButton : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text optionText;
        [SerializeField] private Image iconImage; // 用于显示面具/图标

        /// <summary>
        /// 初始化选项按钮
        /// </summary>
        /// <param name="text">选项文本</param>
        /// <param name="sprite">选项图片（可选）</param>
        /// <param name="onClick">点击回调</param>
        public void Setup(string text, Sprite sprite, Action onClick)
        {
            if (optionText != null)
            {
                optionText.text = text;
                // 如果有图片，可以考虑隐藏文字，或者同时显示。
                // 这里假设如果有图片，且文字就是面具名字，则隐藏文字只显示图片；
                // 或者根据设计需求调整。暂时保留文字显示。
                // 如果用户希望完全替换，可以将 optionText.gameObject.SetActive(sprite == null);
            }

            if (iconImage != null)
            {
                if (sprite != null)
                {
                    iconImage.sprite = sprite;
                    iconImage.gameObject.SetActive(true);
                    // 即使有图片，也保持文字显示
                    if (optionText != null) optionText.gameObject.SetActive(true);
                }
                else
                {
                    iconImage.gameObject.SetActive(false);
                    if (optionText != null) optionText.gameObject.SetActive(true);
                }
            }

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(() => onClick?.Invoke());
                button.interactable = true; // Reset state
            }
        }

        public void SetInteractable(bool interactable)
        {
            if (button != null)
            {
                button.interactable = interactable;
            }
        }
    }
}
