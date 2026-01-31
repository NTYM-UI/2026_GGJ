using System;
using UnityEngine;

namespace ChatSystem
{
    public enum MessageType
    {
        Normal,
        Separator
    }

    /// <summary>
    /// 聊天消息的数据模型类。
    /// 标记为 [Serializable] 以便可以在 Unity Inspector 面板中查看（如果是列表成员）。
    /// </summary>
    [Serializable]
    public class ChatMessage
    {
        public MessageType type = MessageType.Normal;

        /// <summary>
        /// 发送者名称
        /// </summary>
        public string senderName;

        /// <summary>
        /// 消息的具体内容文本
        /// </summary>
        [TextArea] // 在 Inspector 中显示为多行文本框
        public string content;

        /// <summary>
        /// 标识消息来源。
        /// true: 表示玩家自己发送的消息（通常显示在右侧）。
        /// false: 表示对方/NPC/系统发送的消息（通常显示在左侧）。
        /// </summary>
        public bool isSelf;

        /// <summary>
        /// 消息发送的时间戳 (ticks)
        /// </summary>
        public long timestamp;

        /// <summary>
        /// 构造函数，用于快速创建消息对象
        /// </summary>
        /// <param name="content">消息内容</param>
        /// <param name="isSelf">是否是自己发送的</param>
        /// <param name="senderName">发送者名字（可选）</param>
        public ChatMessage(string content, bool isSelf, string senderName = "")
        {
            this.content = content;
            this.isSelf = isSelf;
            this.senderName = senderName;
            this.timestamp = DateTime.Now.Ticks; // 记录当前时间
        }
    }
}
