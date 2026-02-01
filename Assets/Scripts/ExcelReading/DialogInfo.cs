using UnityEngine;
using XlsWork;
using XlsWork.Dialogs;
using System.Collections;
using System.Collections.Generic;
using Core.EventSystem;
using ChatSystem;

[System.Serializable]
public class DialogItem
{
    public string flag; // 标志位：#对话 / &选项 / END结束
    public int id;      // 行ID（对应表格B列）
    public string character; // 人物（C列）
    public string position;  // 位置（左/右，D列）
    public string content;   // 内容（E列）
    public int jumpId;       // 跳转ID（F列）
    public string effect;    // 效果（G列，如"好感度加@1"）
    public string target;    // 目标（H列）
    public float delay;      // 延迟时间（I列，秒）
    public string task;      // 任务（J列）
    public string optionDesc; // 选项描述（K列，显示在按钮上但不上屏）
    public int costTime;     // 消耗时间（M列，秒）
    public float totalTime;  // 总时间（N列，秒，通常只在第一行配置）
    public string consequence; // 后果弹窗内容（O列）
}

public class DialogInfo : MonoBehaviour
{
    [SerializeField] private ChatController chatController;
    private Dictionary<int, List<DialogItem>> dialogDict;
    private int currentDialogId;
    
    // 自动播放间隔（秒）
    [SerializeField] private float autoPlayDelay = 1.0f;

    void Awake()
    {
        // 加载对话表格
        dialogDict = DialogXls.LoadDialogAsDictionary();
        
        // 尝试从表格中获取总时间配置 (遍历寻找第一个非0的 totalTime)
        if (dialogDict != null)
        {
            foreach (var kvp in dialogDict)
            {
                foreach (var item in kvp.Value)
                {
                    if (item.totalTime > 0)
                    {
                        Debug.Log($"[DialogInfo] Found TotalTime config in Excel: {item.totalTime}");
                        if (Core.TimeSystem.GameCountdownTimer.Instance != null)
                        {
                            Core.TimeSystem.GameCountdownTimer.Instance.SetTotalTime(item.totalTime);
                        }
                        goto FoundTime; // 找到后跳出所有循环
                    }
                }
            }
        }
        FoundTime:;
        
        if (chatController == null)
            chatController = FindObjectOfType<ChatController>();
    }

    private void OnEnable()
    {
        EventManager.Instance.Subscribe(GameEvents.DIALOG_START, OnDialogStartEvent);
    }

    private void OnDisable()
    {
        EventManager.Instance.Unsubscribe(GameEvents.DIALOG_START, OnDialogStartEvent);
    }

    private void OnDialogStartEvent(object eventData)
    {
        int startId = 0;
        if (eventData is int id) startId = id;
        else if (eventData is string str && int.TryParse(str, out int parsed)) startId = parsed;

        Debug.Log($"[DialogInfo] Received DIALOG_START event with ID: {startId}");

        // 检查是否需要排队（如果是发给非当前联系人的消息）
        if (startId > 0 && dialogDict.ContainsKey(startId) && chatController != null)
        {
            var firstItems = dialogDict[startId];
            if (firstItems != null && firstItems.Count > 0)
            {
                string charName = firstItems[0].character;
                if (!string.IsNullOrEmpty(charName))
                {
                    var contact = chatController.GetContactByName(charName);
                    // 如果是发给某个联系人的，且当前没有在看这个联系人
                    if (contact != null && chatController.CurrentContact != contact)
                    {
                        Debug.Log($"[DialogInfo] Target contact {charName} is not active. Queuing dialog {startId}.");
                        // 调用 ChatController 的 TriggerNewDialog 进行排队处理（标记未读、设置ID）
                        chatController.TriggerNewDialog(charName, startId);
                        return; // 阻止直接播放
                    }
                }
            }
        }

        // 开始新对话前，清理所有旧的 pending 选项，防止状态冲突
        if (chatController != null)
        {
            chatController.ClearAllPendingOptions();
        }

        if (startId > 0 && dialogDict.ContainsKey(startId))
        {
            Debug.Log($"[DialogInfo] Found ID {startId} in dictionary. Starting sequence.");
            currentDialogId = startId;
            StopAllCoroutines();
            StartCoroutine(RunDialogSequence());
        }
        else
        {
            Debug.LogError($"[DialogInfo] Failed to start dialog. ID: {startId}, Dict Count: {dialogDict?.Count}, ContainsKey: {dialogDict?.ContainsKey(startId)}");
        }
    }

    private ChatSystem.ContactData FindContactInDeepSearch(int startId)
    {
        // 深度搜索：最多往下找 50 层，防止死循环
        int currentId = startId;
        int depth = 0;
        while (currentId > 0 && dialogDict.ContainsKey(currentId) && depth < 50)
        {
            var items = dialogDict[currentId];
            if (items == null || items.Count == 0) break;

            foreach (var item in items)
            {
                // 1. 检查 Character 字段
                if (!string.IsNullOrEmpty(item.character))
                {
                    var contact = chatController.GetContactByName(item.character);
                    if (contact != null) return contact;
                }
            }

            // 没找到，继续找下一句 (JumpId)
            // 如果是选项，通常会有多个 item，我们取第一个的 JumpId 继续找
            // (通常同一段对话里，选项最终都会回到同一个人的对话流，或者这一段就是这个人的)
            currentId = items[0].jumpId;
            depth++;
        }
        return null;
    }

    private IEnumerator RunDialogSequence()
    {
        bool isFirstMessage = true; // 标记是否为第一条消息
        ChatSystem.ContactData activeContact = null; // 记录当前对话的活跃联系人
        HashSet<ChatSystem.ContactData> processedContacts = new HashSet<ChatSystem.ContactData>(); // 记录本次对话中已处理过分隔符的联系人

        // 在开始对话前，尝试检测是否需要插入分割线（如果该联系人已有历史消息）
        if (currentDialogId > 0 && dialogDict.ContainsKey(currentDialogId) && chatController != null)
        {
            var firstItems = dialogDict[currentDialogId];
            if (firstItems != null && firstItems.Count > 0)
            {
                // 预判联系人
                if (activeContact == null)
                {
                    activeContact = FindContactInDeepSearch(currentDialogId);
                }

                // 如果找到了联系人，处理分割线逻辑
                if (activeContact != null)
                {
                    // 标记为已处理
                    processedContacts.Add(activeContact);

                    // 如果联系人存在且已经有聊天记录
                    if (activeContact.messageHistory.Count > 0)
                    {
                        // 检查最后一条是否已经是分割线（避免重复）
                        var lastMsg = activeContact.messageHistory[activeContact.messageHistory.Count - 1];
                        if (lastMsg.type != ChatSystem.MessageType.Separator)
                        {
                            chatController.AddSeparator(activeContact);
                        }
                    }
                }
            }
        }

        while (currentDialogId > 0 && dialogDict.ContainsKey(currentDialogId))
        {
            // 检查是否达到安全节点 (ID >= 12001)
            // 只要触发过一次 >= 12001，就认为进入安全阶段
            // 移到循环开头，确保如果是 12001 且是 END 也能正确触发胜利
            if (currentDialogId >= 12001)
            {
                if (Core.TimeSystem.GameCountdownTimer.Instance != null && !Core.TimeSystem.GameCountdownTimer.Instance.isSafePhase)
                {
                    Core.TimeSystem.GameCountdownTimer.Instance.isSafePhase = true;
                    Debug.Log($"[DialogInfo] Reached checkpoint {currentDialogId}, Safe Phase enabled.");
                    
                    // 只要读到12001且此时没失败，就直接判定胜利触发
                    Debug.Log("[DialogInfo] Reached 12001 (Safe Phase). Triggering GAME_WIN immediately.");
                    EventManager.Instance.TriggerEvent(GameEvents.GAME_WIN, null);
                }
            }

            List<DialogItem> items = dialogDict[currentDialogId];
            if (items == null || items.Count == 0) break;

            // [新增] 每一句都检查当前说话人是否需要分割线（处理中途换人的情况）
            string charName = items[0].character;
            if (!string.IsNullOrEmpty(charName))
            {
                var itemContact = chatController.GetContactByName(charName);
                if (itemContact != null)
                {
                    // 如果是本次对话还没处理过的联系人
                    if (!processedContacts.Contains(itemContact))
                    {
                        processedContacts.Add(itemContact);
                        // 检查是否需要加分割线
                        if (itemContact.messageHistory.Count > 0)
                        {
                            var lastMsg = itemContact.messageHistory[itemContact.messageHistory.Count - 1];
                            if (lastMsg.type != ChatSystem.MessageType.Separator)
                            {
                                chatController.AddSeparator(itemContact);
                            }
                        }
                    }
                    // 更新活跃联系人，确保选项发给对的人
                    activeContact = itemContact;
                }
            }

            // 检查后果弹窗 (O列)
            string consequenceMsg = null;
            foreach (var it in items)
            {
                if (!string.IsNullOrEmpty(it.consequence))
                {
                    consequenceMsg = it.consequence;
                    break;
                }
            }
            if (!string.IsNullOrEmpty(consequenceMsg))
            {
                Debug.Log($"[DialogInfo] Triggering Consequence: {consequenceMsg}");
                EventManager.Instance.TriggerEvent(GameEvents.SHOW_CONSEQUENCE, consequenceMsg);
            }

            // 检查是否是选项（多个条目或者 Flag 为 &）
            bool isOptions = items.Count > 1 || items[0].flag == "&";

            if (isOptions)
            {
                // 如果不是第一条消息（即之前已经有过对话），在显示选项前额外等待1秒
                // 避免选项直接“蹦”出来，给玩家一点反应时间
                if (!isFirstMessage)
                {
                    yield return new WaitForSeconds(1.0f);
                }

                // 选项出现意味着第一条消息阶段已过（或者是选项开局）
                isFirstMessage = false;

                // 处理选项逻辑
                Debug.Log($"[DialogInfo] Displaying {items.Count} options.");
                
                // 收集选项文本
                string[] optionTexts = new string[items.Count];
                for (int i = 0; i < items.Count; i++)
                {
                    // 优先使用 K 列 (OptionDesc) 作为按钮显示文本
                    // 如果 K 列为空，则回退使用 E 列 (Content)
                    if (!string.IsNullOrEmpty(items[i].optionDesc))
                    {
                        optionTexts[i] = items[i].optionDesc;
                    }
                    else
                    {
                        optionTexts[i] = items[i].content;
                    }
                }

                bool optionSelected = false;

                // 显示选项并等待回调
                if (chatController != null)
                {
                    // 传递 activeContact，确保选项只在该联系人界面显示
                    chatController.ShowOptions(optionTexts, (selectedIndex) => {
                        if (selectedIndex >= 0 && selectedIndex < items.Count)
                        {
                            // 选中选项，跳转到对应的 jumpId
                            DialogItem selectedItem = items[selectedIndex];
                            Debug.Log($"[DialogInfo] Option selected: {selectedItem.content}, Jumping to: {selectedItem.jumpId}");

                            // 扣除时间 (M列)
                            if (selectedItem.costTime > 0)
                            {
                                if (Core.TimeSystem.GameCountdownTimer.Instance != null)
                                {
                                    Core.TimeSystem.GameCountdownTimer.Instance.ReduceTime(selectedItem.costTime);
                                }
                            }

                            currentDialogId = selectedItem.jumpId;
                            
                            // 这里可以处理 effect，比如增加好感度等
                            // ProcessEffect(selectedItem.effect);
                            
                            // 将选中的内容作为玩家气泡发送出去
                            // 注意：即使按钮显示的是 K 列，发出去的依然是 E 列 (Content)
                            if (chatController != null)
                            {
                                // 确保发给正确的联系人
                                if (activeContact != null)
                                    chatController.AddMessageToContact(selectedItem.content, true, activeContact);
                                else
                                    chatController.AddMessage(selectedItem.content, true);
                            }

                            optionSelected = true;
                        }
                    }, activeContact); // 传入 activeContact
                }
                else
                {
                    Debug.LogError("[DialogInfo] ChatController is missing!");
                    break;
                }

                // 等待用户选择
                while (!optionSelected)
                {
                    yield return null;
                }
            }
            else
            {
                // 普通单条对话
                DialogItem current = items[0];

                if (current.flag == "END")
                {
                    Debug.Log("[DialogInfo] Reached END flag. Ending dialog.");
                    
                    // 检查是否有后续对话 (JumpID > 0)
                    if (current.jumpId > 0 && chatController != null)
                    {
                        // 使用 I 列 (Delay) 作为等待时间
                        // 使用 C 列 (Character) 作为目标联系人名字
                        float nextDelay = current.delay;
                        string targetChar = current.character;

                        if (!string.IsNullOrEmpty(targetChar))
                        {
                            Debug.Log($"[DialogInfo] Scheduling next dialog for {targetChar}: ID {current.jumpId} after {nextDelay}s");
                            chatController.ScheduleNextDialog(targetChar, current.jumpId, nextDelay);
                        }
                        else
                        {
                             Debug.LogWarning($"[DialogInfo] END row has JumpID {current.jumpId} but missing Character name!");
                        }
                    }

                    EventManager.Instance.TriggerEvent(GameEvents.DIALOG_END, currentDialogId);
                    
                    break;
                }

                // 处理普通消息 (# 或其他)
                bool isSelf = (current.position == "Right" || current.position == "右");
                
                // 扣除时间 (M列) - 普通对话也生效
                if (current.costTime > 0)
                {
                    Debug.Log($"[DialogInfo] Try reducing time: {current.costTime}. Timer Instance: {Core.TimeSystem.GameCountdownTimer.Instance}");
                    if (Core.TimeSystem.GameCountdownTimer.Instance != null)
                    {
                        Core.TimeSystem.GameCountdownTimer.Instance.ReduceTime(current.costTime);
                    }
                    else
                    {
                        Debug.LogError("[DialogInfo] GameCountdownTimer Instance is NULL! Time will not be reduced.");
                    }
                }
                
                // 延迟逻辑：如果是第一条消息，且是对方发的，直接显示不等待
                if (isFirstMessage && !isSelf)
                {
                    // 第一条对方消息，立即显示
                    isFirstMessage = false;
                }
                else
                {
                    // 后续消息或自己发的消息
                    if (!isSelf) 
                    {
                        // 对方消息：优先使用表格配置的 delay，如果没有配置(0)则使用默认 autoPlayDelay
                        float waitTime = current.delay > 0 ? current.delay : autoPlayDelay;
                        yield return new WaitForSeconds(waitTime);
                    }
                    else 
                    {
                        // 自己消息：固定 0.5s 或者也可以配置
                        yield return new WaitForSeconds(0.5f);
                    }
                }

                if (chatController != null)
                {
                    // 查找目标联系人（如果表格里指定了 Character）
                    // 如果 Character 为空，则默认发给当前正在聊天的对象
                    ChatSystem.ContactData targetContact = null;
                    if (!string.IsNullOrEmpty(current.character))
                    {
                        targetContact = chatController.GetContactByName(current.character);
                        if (targetContact != null) activeContact = targetContact; // 更新活跃联系人
                    }
                    else if (activeContact != null)
                    {
                        // 如果当前行没填名字，但之前已经确定了活跃联系人，就沿用
                        targetContact = activeContact;
                    }
                    
                    if (targetContact != null)
                    {
                        chatController.AddMessageToContact(current.content, isSelf, targetContact);
                    }
                    else
                    {
                        // 没找到或者没填，就发给当前界面（回退逻辑）
                        chatController.AddMessage(current.content, isSelf);
                    }
                }

                // 跳转到下一句
                currentDialogId = current.jumpId;
            }
        }
        
        Debug.Log("[DialogInfo] Dialog sequence ended.");
    }
}
