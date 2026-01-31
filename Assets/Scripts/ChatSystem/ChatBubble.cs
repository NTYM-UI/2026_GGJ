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
        public void Setup(ChatMessage message)
        {
            // 1. 先设置对齐方式和 Padding（这会影响宽度）
            if (layoutGroup != null)
            {
                layoutGroup.childAlignment = message.isSelf ? TextAnchor.UpperRight : TextAnchor.UpperLeft;
                
                int avatarSpacing = 150; // 头像预留空间（加大）
                int wideSpacing = 400;   // 另一侧留白（加大）

                if (message.isSelf)
                {
                    // 自己发的：靠右
                    layoutGroup.padding = new RectOffset(wideSpacing, avatarSpacing, 0, 0);
                }
                else
                {
                    // 别人发的：靠左
                    layoutGroup.padding = new RectOffset(avatarSpacing, wideSpacing, 0, 0);
                }
                
                // 强制刷新一下父布局，确保宽度生效
                LayoutRebuilder.ForceRebuildLayoutImmediate(layoutGroup.GetComponent<RectTransform>());
            }

            // 2. 根据消息来源（isSelf）控制头像的显示与隐藏
            // 如果是自己，显示右头像；如果是对方，显示左头像
            if (leftAvatarImage != null) leftAvatarImage.gameObject.SetActive(!message.isSelf);
            if (rightAvatarImage != null) rightAvatarImage.gameObject.SetActive(message.isSelf);

            // 3. 设置气泡背景颜色
            if (bubbleBackground != null)
            {
                bubbleBackground.color = message.isSelf ? selfBubbleColor : otherBubbleColor;
            }

            // 4. 最后设置文本内容并刷新（确保高度计算基于最新的宽度）
            if (messageText != null)
            {
                messageText.text = message.content;
                
                // 启动协程确保下一帧刷新，解决复杂的布局嵌套问题
                StartCoroutine(RefreshLayout());
            }
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
