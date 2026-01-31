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
        private GameObject leftAvatar;
        
        [SerializeField] 
        [Tooltip("右侧头像（自己），通常默认隐藏")]
        private GameObject rightAvatar;
        
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
            // 1. 设置文本内容
            if (messageText != null)
            {
                messageText.text = message.content;
            }

            // 2. 根据消息来源（isSelf）控制头像的显示与隐藏
            // 如果是自己，显示右头像；如果是对方，显示左头像
            if (leftAvatar != null) leftAvatar.SetActive(!message.isSelf);
            if (rightAvatar != null) rightAvatar.SetActive(message.isSelf);

            // 3. 设置气泡背景颜色
            if (bubbleBackground != null)
            {
                bubbleBackground.color = message.isSelf ? selfBubbleColor : otherBubbleColor;
            }

            // 4. 控制对齐方式
            // 如果是自己，气泡靠右对齐；如果是对方，气泡靠左对齐
            if (layoutGroup != null)
            {
                layoutGroup.childAlignment = message.isSelf ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
                
                // 注意：如果你的 Prefab 结构是 [LeftAvatar] [Bubble] [RightAvatar] 
                // 并且使用了 Horizontal Layout Group，那么显隐头像会自动调整布局，
                // 但 TextAnchor 的调整可以确保整体靠左或靠右。
            }
        }
    }
}
