using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.EventSystem
{
    /// <summary>
    /// 核心事件管理器 (单例模式)
    /// 用于管理全局的事件订阅与分发
    /// </summary>
    public class EventManager : MonoBehaviour
    {
        private static EventManager instance;
        private static bool applicationIsQuitting = false;

        public static bool HasInstance => !applicationIsQuitting && instance != null;

        public static EventManager Instance
        {
            get
            {
                if (applicationIsQuitting) return null;

                if (instance == null)
                {
                    instance = FindFirstObjectByType<EventManager>();
                    if (instance == null)
                    {
                        GameObject obj = new GameObject("EventManager");
                        instance = obj.AddComponent<EventManager>();
                    }
                }
                return instance;
            }
        }

        // 存储事件名和对应的委托列表
        private Dictionary<string, Action<object>> eventDictionary;

        private void OnApplicationQuit()
        {
            applicationIsQuitting = true;
        }

        void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(this.gameObject);
                return;
            }

            instance = this;
            DontDestroyOnLoad(this.gameObject);
            eventDictionary = new Dictionary<string, Action<object>>();
        }

        /// <summary>
        /// 订阅事件 (Subscribe)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="listener">回调函数</param>
        public void Subscribe(string eventName, Action<object> listener)
        {
            if (eventDictionary.TryGetValue(eventName, out Action<object> thisEvent))
            {
                thisEvent += listener;
                eventDictionary[eventName] = thisEvent;
            }
            else
            {
                thisEvent += listener;
                eventDictionary.Add(eventName, thisEvent);
            }
        }

        /// <summary>
        /// 取消订阅事件 (Unsubscribe)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="listener">回调函数</param>
        public void Unsubscribe(string eventName, Action<object> listener)
        {
            if (instance == null) return;
            
            if (eventDictionary.TryGetValue(eventName, out Action<object> thisEvent))
            {
                thisEvent -= listener;
                eventDictionary[eventName] = thisEvent;
            }
        }

        /// <summary>
        /// 触发事件 (TriggerEvent)
        /// </summary>
        /// <param name="eventName">事件名称</param>
        /// <param name="payload">传递的数据 (可选)</param>
        public void TriggerEvent(string eventName, object payload = null)
        {
            if (eventDictionary.TryGetValue(eventName, out Action<object> thisEvent))
            {
                thisEvent?.Invoke(payload);
            }
        }
    }
}
