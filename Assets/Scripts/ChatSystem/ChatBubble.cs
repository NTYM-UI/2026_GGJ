using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChatSystem
{
    /// <summary>
    /// 挂载在单个聊天气泡 Prefab 上的脚本。
    /// 负责显示消息内容，并根据消息来源（自己/对方）调整气泡的样式和位置。
    /// </summary>
    public class ChatBubble : MonoBehaviour
    {
        [Header("UI Components (请拖入对应的 UI 元素)")]
        
        [SerializeField] 
        [Tooltip("用于显示消息内容的 TextMeshProUGUI 组件")]
        private TMP_Text messageText; // 改为 TMP_Text
        
        [SerializeField] 
        [Tooltip("气泡的背景图，用于改变颜色")]
        private Image bubbleBackground;
        
        [SerializeField] 
        [Tooltip("左侧头像（对方），通常默认隐藏")]
        private Image leftAvatarImage;
        
        [SerializeField] 
        [Tooltip("右侧头像（自己），通常默认隐藏")]
        private Image rightAvatarImage;
        
        [SerializeField] 
        [Tooltip("用于控制左右对齐的 Layout Group")]
        private HorizontalLayoutGroup layoutGroup;

        [Header("Settings (样式设置)")]
        
        [SerializeField] 
        [Tooltip("自己发送的消息气泡颜色")]
        private Color selfBubbleColor = new Color(0.6f, 1f, 0.6f); // 浅绿色
        
        [SerializeField] 
        [Tooltip("对方发送的消息气泡颜色")]
        private Color otherBubbleColor = Color.white;

        /// <summary>
        /// 初始化气泡数据。由 ChatController 在生成气泡时调用。
        /// </summary>
        /// <param name="message">消息数据对象</param>
        /// <param name="avatarSprite">头像图片（可选）</param>
        /// <param name="onAvatarClick">头像点击回调（可选，带 RectTransform 参数）</param>
        public void Setup(ChatMessage message, Sprite avatarSprite = null, System.Action<RectTransform> onAvatarClick = null)
        {
            // 配置参数
            float avatarSize = 100f;   // 头像大小
            float spacing = 20f;       // 气泡与头像/边缘的间距
            float wideSidePadding = 300f;  // 另一侧留白

            // 1. 处理头像显示 (使用标准布局模式)
            // 确保头像组件上有 LayoutElement 且不忽略布局
            if (leftAvatarImage != null)
            {
                bool showLeft = !message.isSelf;
                leftAvatarImage.gameObject.SetActive(showLeft);
                if (showLeft)
                {
                    if (avatarSprite != null) leftAvatarImage.sprite = avatarSprite;
                    SetAvatarLayoutElement(leftAvatarImage, avatarSize);
                    SetupAvatarButton(leftAvatarImage, onAvatarClick);
                }
            }
            
            if (rightAvatarImage != null)
            {
                bool showRight = message.isSelf;
                rightAvatarImage.gameObject.SetActive(showRight);
                if (showRight)
                {
                    if (avatarSprite != null) rightAvatarImage.sprite = avatarSprite;
                    SetAvatarLayoutElement(rightAvatarImage, avatarSize);
                    // 自己的头像通常不需要点击看简介，但如果需要也可以加上
                    // SetupAvatarButton(rightAvatarImage, onAvatarClick); 
                }
            }

            // 2. 设置 LayoutGroup Padding
            // 此时头像已经占据了空间，Padding 只需要处理"额外的"留白
            if (layoutGroup != null)
            {
                layoutGroup.childAlignment = message.isSelf ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
                
                // 气泡和头像之间的间距由 LayoutGroup 的 Spacing 控制（如果在 Inspector 里设置了）
                // 这里我们主要控制左右两侧的额外留白
                
                // 注意：如果 HorizontalLayoutGroup 开启了 Child Force Expand Width，需要关掉它，否则气泡会被拉伸
                layoutGroup.childForceExpandWidth = false; 
                layoutGroup.childControlWidth = true;
                layoutGroup.childControlHeight = true;
                layoutGroup.spacing = spacing; // 设置头像和气泡之间的间距

                if (message.isSelf)
                {
                    // 自己发的：靠右
                    // 左边留大白，右边留一点点边距(或者0，因为有头像了)
                    layoutGroup.padding = new RectOffset((int)wideSidePadding, (int)spacing, 0, 0);
                }
                else
                {
                    // 别人发的：靠左
                    // 左边留一点点边距，右边留大白
                    layoutGroup.padding = new RectOffset((int)spacing, (int)wideSidePadding, 0, 0);
                }
                
                // 强制刷新
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
            }

            // 3. 设置气泡背景颜色
            if (bubbleBackground != null)
            {
                bubbleBackground.color = message.isSelf ? selfBubbleColor : otherBubbleColor;
            }

            // 4. 最后设置文本内容并刷新
            if (messageText != null)
            {
                messageText.text = message.content;
                StartCoroutine(RefreshLayout());
            }
        }

        /// <summary>
        /// 为头像添加点击功能
        /// </summary>
        private void SetupAvatarButton(Image avatar, System.Action<RectTransform> onClick)
        {
            if (onClick == null) return;

            Button btn = avatar.GetComponent<Button>();
            if (btn == null) btn = avatar.gameObject.AddComponent<Button>();
            
            btn.onClick.RemoveAllListeners();
            btn.onClick.AddListener(() => onClick.Invoke(avatar.rectTransform));
        }

        /// <summary>
        /// 设置头像的布局属性 (标准布局)
        /// </summary>
        private void SetAvatarLayoutElement(Image avatar, float size)
        {
            // 确保有 LayoutElement 并参与布局
            LayoutElement le = avatar.GetComponent<LayoutElement>();
            if (le == null) le = avatar.gameObject.AddComponent<LayoutElement>();
            
            le.ignoreLayout = false; // 参与布局！
            le.minWidth = size;
            le.preferredWidth = size;
            le.flexibleWidth = 0; // 不伸缩
            le.minHeight = size;
            le.preferredHeight = size;
            le.flexibleHeight = 0;
            
            // 重置 RectTransform，防止之前的绝对定位残留影响
            RectTransform rt = avatar.rectTransform;
            rt.anchoredPosition = Vector2.zero;
        }

        private System.Collections.IEnumerator RefreshLayout()
        {
            // 必须等待足够的时间让 Unity UI 系统完成布局计算
            // 第一帧：设置文字
            // 第二帧：计算文字大小
            // 第三帧：计算背景大小
            yield return null;
            yield return null;

            if (messageText != null)
            {
                messageText.ForceMeshUpdate();
                LayoutRebuilder.ForceRebuildLayoutImmediate(messageText.rectTransform);
            }

            if (bubbleBackground != null)
            {
                // 如果背景上有 ContentSizeFitter，强制它更新
                ContentSizeFitter fitter = bubbleBackground.GetComponent<ContentSizeFitter>();
                if (fitter != null)
                {
                    fitter.SetLayoutVertical();
                    fitter.SetLayoutHorizontal();
                }
                LayoutRebuilder.ForceRebuildLayoutImmediate(bubbleBackground.rectTransform);
            }
            
            if (layoutGroup != null)
            {
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
            }
        }
    }
}
