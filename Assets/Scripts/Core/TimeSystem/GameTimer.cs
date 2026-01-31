using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace Core.TimeSystem
{
    /// <summary>
    /// 游戏正计时系统
    /// </summary>
    public class GameTimer : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("是否限制最大时间")]
        public bool hasTimeLimit = true;
        
        [Tooltip("最大时间（秒），例如48小时 = 172800秒")]
        public float maxTimeInSeconds = 172800f; // 默认48小时

        [Tooltip("时间流逝速度倍率，1为真实时间，60为1分钟=1小时")]
        public float timeScale = 1.0f;

        [Header("UI References")]
        [Tooltip("显示时间的文本组件")]
        public TMP_Text timerText;

        private float currentTime;
        private bool isRunning = false;

        private void Start()
        {
            // 初始化时间
            currentTime = 0f; // 从0开始
            isRunning = true;
            UpdateUI();
        }

        private void Update()
        {
            if (!isRunning) return;

            // 增加时间
            currentTime += Time.deltaTime * timeScale;

            // 检查是否达到上限
            if (hasTimeLimit && currentTime >= maxTimeInSeconds)
            {
                currentTime = maxTimeInSeconds;
                isRunning = false;
                OnTimerEnd();
            }

            UpdateUI();
        }

        // --- 按钮控制方法 ---

        /// <summary>
        /// 暂停计时
        /// </summary>
        public void PauseTimer()
        {
            isRunning = false;
        }

        /// <summary>
        /// 继续/开始计时
        /// </summary>
        public void ResumeTimer()
        {
            isRunning = true;
        }

        /// <summary>
        /// 切换暂停/开始状态
        /// </summary>
        public void ToggleTimer()
        {
            isRunning = !isRunning;
        }

        /// <summary>
        /// 重置计时器（归零并暂停）
        /// </summary>
        public void ResetTimer()
        {
            currentTime = 0f;
            isRunning = false;
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (timerText == null) return;

            // 格式化时间显示
            // 假设我们显示为 "48:00:00" 或者 "剩余时间: 48h 00m"
            // 这里使用 HH:MM:SS 格式
            
            System.TimeSpan ts = System.TimeSpan.FromSeconds(currentTime);
            string timeStr = string.Format("{0:D2}:{1:D2}:{2:D2}", 
                (int)ts.TotalHours, 
                ts.Minutes, 
                ts.Seconds);

            timerText.text = timeStr;
        }

        private void OnTimerEnd()
        {
            Debug.Log("GameJam 时间结束！");
            // 这里可以触发游戏结束事件
            // Core.EventSystem.EventManager.Instance.TriggerEvent(Core.EventSystem.GameEvents.GAME_OVER);
        }
        
        // 公共方法：增加时间（比如作为惩罚）
        public void AddTime(float seconds)
        {
            currentTime += seconds;
            if (hasTimeLimit && currentTime > maxTimeInSeconds) currentTime = maxTimeInSeconds;
            UpdateUI();
        }
        
        // 公共方法：减少时间（比如作为奖励）
        public void ReduceTime(float seconds)
        {
            currentTime -= seconds;
            if (currentTime < 0) currentTime = 0;
            UpdateUI();
        }
    }
}
