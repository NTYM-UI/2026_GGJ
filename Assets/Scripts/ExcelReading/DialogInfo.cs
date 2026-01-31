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

    private IEnumerator RunDialogSequence()
    {
        bool isFirstMessage = true; // 标记是否为第一条消息

        while (currentDialogId > 0 && dialogDict.ContainsKey(currentDialogId))
        {
            List<DialogItem> items = dialogDict[currentDialogId];
            if (items == null || items.Count == 0) break;

            // 检查是否是选项（多个条目或者 Flag 为 &）
            bool isOptions = items.Count > 1 || items[0].flag == "&";

            if (isOptions)
            {
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
                    chatController.ShowOptions(optionTexts, (selectedIndex) => {
                        if (selectedIndex >= 0 && selectedIndex < items.Count)
                            {
                                // 选中选项，跳转到对应的 jumpId
                                DialogItem selectedItem = items[selectedIndex];
                                Debug.Log($"[DialogInfo] Option selected: {selectedItem.content}, Jumping to: {selectedItem.jumpId}");
                                currentDialogId = selectedItem.jumpId;
                                
                                // 这里可以处理 effect，比如增加好感度等
                                // ProcessEffect(selectedItem.effect);
                                
                                // 将选中的内容作为玩家气泡发送出去
                                // 注意：即使按钮显示的是 K 列，发出去的依然是 E 列 (Content)
                                if (chatController != null)
                                {
                                    chatController.AddMessage(selectedItem.content, true);
                                }

                                optionSelected = true;
                            }
                    });
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
                    chatController.AddMessage(current.content, isSelf);
                }

                // 跳转到下一句
                currentDialogId = current.jumpId;
            }
        }
        
        Debug.Log("[DialogInfo] Dialog sequence ended.");
    }
}
