using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

namespace ChatSystem
{
    /// <summary>
    /// 挂载在左侧联系人列表的单个按钮上
    /// </summary>
    public class ContactItem : MonoBehaviour
    {
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private Button button;
        [SerializeField] private Image avatarImage; // 可选：头像显示
        [SerializeField] private GameObject redDotObj; // 红点提示

        private ContactData data;
        private Action<ContactData> onClickCallback;

        public void Setup(ContactData data, Action<ContactData> onClick)
        {
            this.data = data;
            this.onClickCallback = onClick;

            if (nameText != null) nameText.text = data.contactName;
            
            // 如果有头像且设置了 Image 组件
            if (avatarImage != null && data.avatar != null)
            {
                avatarImage.sprite = data.avatar;
                avatarImage.gameObject.SetActive(true);
            }

            // 设置红点状态
            SetUnread(data.isUnread);

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnItemClicked);
            }
        }

        public void SetUnread(bool isUnread)
        {
            if (redDotObj == null)
            {
                // 尝试查找
                Transform t = transform.Find("RedDot");
                if (t != null) 
                {
                    redDotObj = t.gameObject;
                }
                else
                {
                    // 动态创建红点
                    GameObject obj = new GameObject("RedDot");
                    obj.transform.SetParent(transform, false);
                    Image img = obj.AddComponent<Image>();
                    img.color = Color.red;
                    
                    // 设置为圆形（如果有圆形Sprite最好，没有就是方块）
                    // img.sprite = ... 

                    RectTransform rt = obj.GetComponent<RectTransform>();
                    // 右上角
                    rt.anchorMin = new Vector2(1, 1);
                    rt.anchorMax = new Vector2(1, 1);
                    rt.pivot = new Vector2(1, 1);
                    rt.anchoredPosition = new Vector2(-10, -10);
                    rt.sizeDelta = new Vector2(20, 20);
                    
                    redDotObj = obj;
                }
            }
            
            if (redDotObj != null)
            {
                redDotObj.SetActive(isUnread);
            }
        }

        private void OnItemClicked()
        {
            onClickCallback?.Invoke(data);
        }

        /// <summary>
        /// 设置选中状态的高亮（可选）
        /// </summary>
        public void SetSelected(bool isSelected)
        {
            if (button != null)
            {
                // 简单示例：选中时变暗，未选中恢复
                // 实际项目中建议替换为切换 Sprite 或改变文字颜色
                ColorBlock colors = button.colors;
                colors.normalColor = isSelected ? new Color(0.8f, 0.8f, 0.8f) : Color.white;
                button.colors = colors;
            }
        }
    }
}
