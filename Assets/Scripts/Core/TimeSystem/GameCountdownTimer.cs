using UnityEngine;
using UnityEngine.UI;

namespace Core.TimeSystem
{
    /// <summary>
    /// 游戏倒计时系统 (600秒)
    /// </summary>
    public class GameCountdownTimer : MonoBehaviour
    {
        public static GameCountdownTimer Instance { get; private set; }

        [Header("Settings")]
        public float totalTime = 600f; // 总时间 600秒
        
        [Header("UI References")]
        public Slider timeSlider; // 进度条引用

        private float currentTime;
        private bool isRunning = false;
        
        // 是否进入安全阶段（时间耗尽也不会立即失败）
        public bool isSafePhase = false;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            // 初始化时间 (如果已经被外部设置过，就不重置为默认值)
            if (currentTime <= 0) 
            {
                currentTime = totalTime;
            }
            isRunning = true;
            UpdateUI();
        }

        public void SetTotalTime(float time)
        {
            totalTime = time;
            currentTime = time; // 立即更新当前时间
            UpdateUI();
        }

        private void Update()
        {
            if (!isRunning) return;

            // 倒计时
            if (currentTime > 0)
            {
                currentTime -= Time.deltaTime;
                if (currentTime <= 0)
                {
                    currentTime = 0;
                    isRunning = false;
                    OnTimerEnd();
                }
                UpdateUI();
            }
        }

        /// <summary>
        /// 扣除时间
        /// </summary>
        /// <param name="amount">秒数</param>
        public void ReduceTime(float amount)
        {
            if (!isRunning) return;
            
            currentTime -= amount;
            if (currentTime < 0) currentTime = 0;
            
            Debug.Log($"[GameCountdownTimer] Reduced {amount}s, Remaining: {currentTime}s");
            UpdateUI();
        }

        private void UpdateUI()
        {
            if (timeSlider != null)
            {
                timeSlider.value = currentTime / totalTime;
            }
        }

        private void OnTimerEnd()
        {
            Debug.Log("[GameCountdownTimer] Time's up!");
            
            // 如果处于安全阶段（已触发关键剧情），则不立即判负，等待剧情播放结束
            if (isSafePhase)
            {
                Debug.Log("[GameCountdownTimer] In Safe Phase. Waiting for dialog to end.");
            }
            else
            {
                // 否则立即失败
                Debug.Log("[GameCountdownTimer] Game Fail triggered!");
                Core.EventSystem.EventManager.Instance.TriggerEvent(Core.EventSystem.GameEvents.GAME_FAIL, null);
            }
        }
    }
}
