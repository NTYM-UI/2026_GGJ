using System.Collections.Generic;
using UnityEngine;

namespace ChatSystem
{
    [System.Serializable]
    public class ContactData
    {
        // public string contactId; // 不需要手动设置 ID，直接用名字区分
        public string contactName;
        public Sprite avatar; // 头像（可选）
        public Sprite profileImage; // 个人信息图片
        public Sprite selectedSprite; // 选中状态的图片（可选）
    
        // Excel 对话 ID (如果设置了此ID，优先使用 Excel 对话)
    public int excelDialogId = 0;

    // 每个联系人拥有自己独立的聊天记录
    public List<ChatMessage> messageHistory = new List<ChatMessage>();

    // 是否有未读消息
    public bool isUnread = false;

    public ContactData(string name)
    {
        this.contactName = name;
        // this.contactId = System.Guid.NewGuid().ToString();
        this.messageHistory = new List<ChatMessage>();
    }
}
}
