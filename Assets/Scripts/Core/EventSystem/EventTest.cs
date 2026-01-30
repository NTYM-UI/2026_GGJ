using UnityEngine;

namespace Core.EventSystem
{
    /// <summary>
    /// 事件系统测试脚本
    /// 挂载到场景中的任意 GameObject 上进行测试
    /// </summary>
    public class EventTest : MonoBehaviour
    {
        void OnEnable()
        {
            // 订阅事件 (Subscribe)
            EventManager.Instance.Subscribe(GameEvents.ON_GAME_START, HandleGameStart);
            EventManager.Instance.Subscribe(GameEvents.ON_OBJECT_GRABBED, HandleObjectGrabbed);
        }

        void OnDisable()
        {
            // 取消订阅 (Unsubscribe) - 防止内存泄漏
            if (EventManager.HasInstance)
            {
                EventManager.Instance.Unsubscribe(GameEvents.ON_GAME_START, HandleGameStart);
                EventManager.Instance.Unsubscribe(GameEvents.ON_OBJECT_GRABBED, HandleObjectGrabbed);
            }
        }

        void Start()
        {
            // 模拟触发事件
            Debug.Log("测试：3秒后触发游戏开始事件...");
            Invoke(nameof(TestTriggerGameStart), 3f);
            
            Debug.Log("测试：5秒后触发物体抓取事件...");
            Invoke(nameof(TestTriggerGrab), 5f);
        }

        void TestTriggerGameStart()
        {
            // 触发事件 (TriggerEvent)
            EventManager.Instance.TriggerEvent(GameEvents.ON_GAME_START);
        }

        void TestTriggerGrab()
        {
            // 触发事件 (TriggerEvent) - 带参数
            string grabbedObjectName = "Ancient Sword";
            EventManager.Instance.TriggerEvent(GameEvents.ON_OBJECT_GRABBED, grabbedObjectName);
        }

        // 事件处理函数
        void HandleGameStart(object payload)
        {
            Debug.Log("【事件响应】游戏开始了！");
        }

        void HandleObjectGrabbed(object payload)
        {
            string objName = (string)payload;
            Debug.Log($"【事件响应】玩家抓取了物体: {objName}");
        }
    }
}
