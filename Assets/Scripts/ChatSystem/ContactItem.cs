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
        
        // 用于显示选中状态的 Image 组件（如果是直接替换按钮背景图，可以指向 Button 的 TargetGraphic）
        [SerializeField] private Image backgroundOrSelectionImage; 

        private ContactData data;
        private Action<ContactData> onClickCallback;
        private Sprite defaultSprite; // 默认图片（未选中）

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

            // 保存默认图片（假设当前显示的图片就是默认图）
            if (backgroundOrSelectionImage != null)
            {
                if (defaultSprite == null) 
                    defaultSprite = backgroundOrSelectionImage.sprite;
            }
            else if (button != null && button.targetGraphic is Image)
            {
                // 如果没手动指定，尝试获取按钮的目标图片
                backgroundOrSelectionImage = button.targetGraphic as Image;
                if (defaultSprite == null) 
                    defaultSprite = backgroundOrSelectionImage.sprite;
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
            // 如果配置了选中状态的图片，则切换图片
            if (backgroundOrSelectionImage != null && data.selectedSprite != null)
            {
                backgroundOrSelectionImage.sprite = isSelected ? data.selectedSprite : defaultSprite;
            }
            else if (button != null)
            {
                // 降级方案：改变颜色
                ColorBlock colors = button.colors;
                colors.normalColor = isSelected ? new Color(0.8f, 0.8f, 0.8f) : Color.white;
                button.colors = colors;
            }
        }
    }
}
