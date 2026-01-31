using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ChatSystem
{
    /// <summary>
    /// 聊天系统的主控制器。
    /// 负责管理消息列表、处理选项交互、实例化消息气泡以及自动滚动。
    /// </summary>
    public class ChatController : MonoBehaviour
    {
        [Header("References (请拖入场景中的 UI 对象)")]
        
        [SerializeField] 
        [Tooltip("聊天气泡的预制体 (Prefab)")]
        private GameObject chatBubblePrefab;
        
        [SerializeField] 
        [Tooltip("消息内容的父容器 (Scroll View -> Viewport -> Content)")]
        private Transform contentContainer;
        
        [SerializeField] 
        [Tooltip("用于控制滚动的 ScrollRect 组件")]
        private ScrollRect scrollRect;

        [Header("Option System (选项系统)")]
        
        [SerializeField]
        [Tooltip("选项按钮的预制体")]
        private GameObject optionButtonPrefab;

        [SerializeField]
        [Tooltip("选项按钮的父容器 (Panel)")]
        private Transform optionsContainer;

        [Header("Contact List System (联系人列表)")]
        
        [SerializeField]
        [Tooltip("将场景中手动放置的联系人按钮拖到这里")]
        private List<ContactItem> manualContactButtons = new List<ContactItem>();
        
        [SerializeField] private List<ContactData> contacts = new List<ContactData>();

        [Header("Audio (音效)")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private AudioClip notificationSound;

        private ContactData currentContact;
        private Dictionary<ContactData, ContactItem> contactItemMap = new Dictionary<ContactData, ContactItem>();
        
        // 用于控制对话流程的变量
        private System.Action<int> onOptionSelectedCallback;
        private bool isWaitingForOption = false; // 是否正在等待玩家选择选项

        // 存储所有聊天消息的列表 (不再使用全局列表，改为使用 currentContact.messageHistory)
        // private List<ChatMessage> messages = new List<ChatMessage>();

        private void Awake()
        {
            if (contentContainer == null)
            {
                Transform viewport = transform.Find("Scroll View/Viewport/Content");
                if (viewport != null) contentContainer = viewport;
                else
                {
                    var found = transform.Find("Content");
                    if (found == null) found = GetComponentInChildren<VerticalLayoutGroup>()?.transform;
                    if (found != null) contentContainer = found;
                }
            }
            if (scrollRect == null) scrollRect = GetComponentInChildren<ScrollRect>();
            if (optionsContainer == null) optionsContainer = transform.Find("OptionsContainer");
            
            if (audioSource == null) audioSource = GetComponent<AudioSource>();
            if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        }

        private void Start()
        {
            // 确保选项容器一开始是空的
            ClearOptions();

            // 检查 DialogInfo 是否存在
            if (FindObjectOfType<DialogInfo>() == null)
            {
                Debug.LogError("【严重错误】场景中找不到 DialogInfo 组件！\n请创建一个空物体（例如叫 DialogManager），并将 Assets/Scripts/ExcelReading/DialogInfo.cs 脚本挂载上去。\n否则无法读取 Excel 对话！");
            }

            // 初始化联系人列表
            InitializeContacts();

            // 默认选中第一个联系人
            if (contacts.Count > 0)
            {
                SwitchToContact(contacts[0]);
            }
        }

        private void InitializeContacts()
        {
            // 确保有手动配置的按钮
            if (manualContactButtons == null || manualContactButtons.Count == 0) return;

            // 清理映射
            contactItemMap.Clear();

            for (int i = 0; i < manualContactButtons.Count; i++)
            {
                if (manualContactButtons[i] == null) continue;

                if (i < contacts.Count)
                {
                    // 检查是否应该标记为未读
                    // 如果有待触发的对话，强制设为未读
                    if (contacts[i].excelDialogId > 0)
                    {
                        contacts[i].isUnread = true;
                    }

                    // 激活并设置按钮
                    manualContactButtons[i].gameObject.SetActive(true);
                    manualContactButtons[i].Setup(contacts[i], OnContactSelected);
                    contactItemMap[contacts[i]] = manualContactButtons[i];
                }
                else
                {
                    // 如果按钮多于数据，隐藏多余按钮
                    manualContactButtons[i].gameObject.SetActive(false);
                }
            }
        }

        private void OnContactSelected(ContactData contact)
        {
            // 如果正在等待选项，禁止切换联系人
            if (isWaitingForOption)
            {
                Debug.Log("[ChatController] Cannot switch contact while waiting for option selection.");
                return;
            }

            if (currentContact == contact) return;
            SwitchToContact(contact);
        }

        public void SwitchToContact(ContactData contact)
        {
            currentContact = contact;

            // 清除未读状态
            if (contact.isUnread)
            {
                contact.isUnread = false;
                if (contactItemMap.ContainsKey(contact))
                {
                    contactItemMap[contact].SetUnread(false);
                }
            }

            // 1. 清空当前聊天界面
            foreach (Transform child in contentContainer)
            {
                Destroy(child.gameObject);
            }

            // 2. 加载该联系人的历史消息
            foreach (var msg in contact.messageHistory)
            {
                CreateBubble(msg);
            }

            // 3. 检查是否有待触发的新对话 (无论是第一次还是后续追加)
            if (contact.excelDialogId > 0)
            {
                // 暂时保存 ID
                int startId = contact.excelDialogId;
                Debug.Log($"[ChatController] Triggering dialog for contact {contact.contactName}. ID: {startId}");

                // 触发对话
                Core.EventSystem.EventManager.Instance.TriggerEvent(Core.EventSystem.GameEvents.DIALOG_START, startId);
                
                // 触发后重置 ID，防止下次切换回来时重复触发
                // 注意：如果对话系统需要保留这个ID来判断进度，则不应重置。
                // 但根据目前逻辑，DialogInfo 会自己接管流程。
                // 如果 DialogInfo 只是播放一段，那么播放完就结束了。
                // 如果我们需要"保存进度"，应该由外部系统或任务系统更新 contact.excelDialogId。
                // 这里为了防止重复播放，我们将其重置。
                contact.excelDialogId = 0;
            }
        }
        
        /// <summary>
        /// 提供给外部系统调用，用于强制开始一段新的对话（追加在现有记录后面）
        /// </summary>
        /// <param name="contactName">联系人名字</param>
        /// <param name="dialogId">新的对话ID</param>
        public void TriggerNewDialog(string contactName, int dialogId)
        {
            // 找到对应的联系人
            ContactData targetContact = contacts.Find(c => c.contactName == contactName);
            if (targetContact != null)
            {
                // 如果当前正好在这个联系人的界面，直接触发对话
                if (currentContact == targetContact)
                {
                    Core.EventSystem.EventManager.Instance.TriggerEvent(Core.EventSystem.GameEvents.DIALOG_START, dialogId);
                }
                else
                {
                    // 如果不在当前界面，标记为未读并设置新的 Excel ID
                    targetContact.excelDialogId = dialogId;
                    targetContact.isUnread = true;
                    
                    if (contactItemMap.ContainsKey(targetContact))
                    {
                        contactItemMap[targetContact].SetUnread(true);
                    }
                    
                    // 播放提示音
                    PlayNotificationSound();
                }
            }
        }

        public ContactData GetContactByName(string name)
        {
            return contacts.Find(c => c.contactName == name);
        }

        /// <summary>
        /// 延迟触发下一段对话
        /// </summary>
        public void ScheduleNextDialog(string contactName, int nextId, float delay)
        {
            StartCoroutine(DelayedTrigger(contactName, nextId, delay));
        }

        private IEnumerator DelayedTrigger(string contactName, int nextId, float delay)
        {
            if (delay > 0) yield return new WaitForSeconds(delay);
            TriggerNewDialog(contactName, nextId);
        }

        private void PlayNotificationSound()
        {
            if (audioSource != null && notificationSound != null)
            {
                audioSource.PlayOneShot(notificationSound);
            }
        }

        // 旧的演示流程，保留备用或删除
        // private IEnumerator DemoConversationFlow() ...

        /// <summary>
        /// 显示一组选项供玩家选择
        /// </summary>
        /// <param name="options">选项文本数组</param>
        public void ShowOptions(string[] options)
        {
            ShowOptions(options, null);
        }

        /// <summary>
        /// 显示一组选项供玩家选择，并指定回调
        /// </summary>
        /// <param name="options">选项文本数组</param>
        /// <param name="onSelected">选择后的回调，参数为选项索引</param>
        public void ShowOptions(string[] options, System.Action<int> onSelected)
        {
            if (optionsContainer == null || optionButtonPrefab == null)
            {
                Debug.LogError("ChatController: 缺少选项相关的引用！");
                return;
            }

            // 设置回调
            onOptionSelectedCallback = onSelected;
            isWaitingForOption = true; // 锁定联系人切换

            // 清理旧选项
            ClearOptions();

            // 显示容器
            optionsContainer.gameObject.SetActive(true);

            // 生成新选项
            for (int i = 0; i < options.Length; i++)
            {
                string optionText = options[i];
                int index = i; // 捕获索引
                GameObject btnObj = Instantiate(optionButtonPrefab, optionsContainer);
                ChatOptionButton btnScript = btnObj.GetComponent<ChatOptionButton>();
                
                if (btnScript != null)
                {
                    // 绑定点击事件
                    btnScript.Setup(optionText, () => OnOptionSelected(optionText, index));
                }
            }
            
            // 强制滚动到底部，确保选项可见
            StartCoroutine(ScrollToBottom());
        }

        /// <summary>
        /// 当玩家点击某个选项时触发
        /// </summary>
        private void OnOptionSelected(string selectedOption, int index)
        {
            isWaitingForOption = false; // 解锁联系人切换

            // 1. 清空选项按钮，但保持容器可见
            ClearOptions();
            // optionsContainer.gameObject.SetActive(false); // 保持常显
            
            if (optionsContainer != null)
            {
                foreach (Transform child in optionsContainer)
                {
                    var btn = child.GetComponent<ChatOptionButton>();
                    if (btn != null) btn.SetInteractable(false);
                }
            }

            // 2. 将选中的文本作为玩家消息发送
            // AddMessage(selectedOption, true); // 修改：不再自动发送选项文本，交由外部控制（DialogInfo）

            // 3. 标记选择完成
            // 4. 执行外部回调
            onOptionSelectedCallback?.Invoke(index);
            onOptionSelectedCallback = null;
        }

        // private IEnumerator SimulateNPCResponse() ... 旧逻辑删除

        private void ClearOptions()
        {
            if (optionsContainer == null) return;

            foreach (Transform child in optionsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// 添加一条新消息到界面上
        /// </summary>
        public void AddMessage(string text, bool isSelf)
        {
            AddMessageToContact(text, isSelf, currentContact);
        }

        /// <summary>
        /// 指定联系人添加消息
        /// </summary>
        public void AddMessageToContact(string text, bool isSelf, ContactData targetContact)
        {
            if (string.IsNullOrEmpty(text)) return;
            if (targetContact == null) return;

            // 1. 添加到该联系人的历史记录
            ChatMessage msg = new ChatMessage(text, isSelf);
            targetContact.messageHistory.Add(msg);

            // 2. 如果当前正好显示的是该联系人，则立即生成气泡
            if (currentContact == targetContact)
            {
                CreateBubble(msg);
            }
            // 3. 否则，标记为未读（仅限对方发的消息）
            else if (!isSelf)
            {
                targetContact.isUnread = true;
                if (contactItemMap.ContainsKey(targetContact))
                {
                    contactItemMap[targetContact].SetUnread(true);
                }
                PlayNotificationSound();
            }
        }

        private void CreateBubble(ChatMessage msg)
        {
            if (chatBubblePrefab == null || contentContainer == null) return;

            GameObject bubbleObj = Instantiate(chatBubblePrefab, contentContainer);
            ChatBubble bubble = bubbleObj.GetComponent<ChatBubble>();
            if (bubble != null)
            {
                bubble.Setup(msg);
            }

            StartCoroutine(ScrollToBottom());
        }

        private IEnumerator ScrollToBottom()
        {
            yield return new WaitForEndOfFrame();
            if (scrollRect != null)
            {
                scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
}
