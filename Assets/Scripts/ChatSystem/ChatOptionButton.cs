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

        /// <summary>
        /// 初始化选项按钮
        /// </summary>
        /// <param name="text">选项文本</param>
        /// <param name="onClick">点击回调</param>
        public void Setup(string text, Action onClick)
        {
            if (optionText != null)
            {
                optionText.text = text;
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
