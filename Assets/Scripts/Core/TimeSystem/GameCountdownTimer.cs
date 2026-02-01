using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
        [Tooltip("用于显示剩余时间百分比的文本组件")]
        public TextMeshProUGUI percentageText;
        
        [Header("Appearance Settings")]
        [Tooltip("背景图片组件（代表已逝去的时间/走过的部分），通常是 Slider 的 Background")]
        public Image backgroundImage;
        [Tooltip("已逝去部分的颜色")]
        public Color elapsedColor = new Color(0.3f, 0.3f, 0.3f, 1f); // 默认深灰色

        [Tooltip("填充图片组件（代表剩余时间），通常是 Slider 的 Fill")]
        public Image fillImage;
        [Tooltip("是否让剩余时间根据进度变色（例如：绿->红）")]
        public bool useColorGradient = false;
        [Tooltip("剩余时间的颜色渐变（左边是0/结束，右边是1/开始）")]
        public Gradient remainingTimeGradient;

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
            
            // 刷新引用和颜色
            RefreshReferencesAndColor();

            isRunning = true;
            UpdateUI();
        }

        private void OnValidate()
        {
            // 在编辑器中修改值时实时预览
            RefreshReferencesAndColor();
            // 仅仅为了预览颜色，不真正修改 value
            if (Application.isPlaying) UpdateUI();
        }

        private void RefreshReferencesAndColor()
        {
            // 自动查找 UI 组件引用（如果未手动赋值）
            if (timeSlider != null)
            {
                // 优先找到 Fill Image (因为它是排除项)
                if (fillImage == null && timeSlider.fillRect != null)
                {
                    // fillRect 可能是 Image 本身，也可能是父容器
                    fillImage = timeSlider.fillRect.GetComponent<Image>();
                    if (fillImage == null)
                    {
                        fillImage = timeSlider.fillRect.GetComponentInChildren<Image>();
                    }
                }

                // [Fix] 纠错：如果 backgroundImage 被错误地赋值为了 fillImage，强制重置
                if (backgroundImage != null && fillImage != null && backgroundImage == fillImage)
                {
                    backgroundImage = null;
                }
                
                // [Fix] 纠错：如果 backgroundImage 是 fillRect 的子物体，强制重置
                if (backgroundImage != null && timeSlider.fillRect != null && backgroundImage.transform.IsChildOf(timeSlider.fillRect))
                {
                     backgroundImage = null;
                }

                if (backgroundImage == null)
                {
                    // 1. 尝试直接查找名为 "Background" 的子物体
                    Transform bgTrans = timeSlider.transform.Find("Background");
                    
                    // 2. 如果没找到，尝试在所有子物体中递归查找名为 "Background" 的物体
                    if (bgTrans == null)
                    {
                        foreach (Transform t in timeSlider.GetComponentsInChildren<Transform>(true))
                        {
                            if (t.name == "Background")
                            {
                                bgTrans = t;
                                break;
                            }
                        }
                    }

                    // 3. 如果还是没找到，尝试获取 Slider 上除了 Fill 之外的第一个 Image
                    if (bgTrans == null)
                    {
                         // 这是一个比较冒险的猜测，但对于标准 Slider 结构通常有效
                         // Slider -> Background (Image)
                         //        -> Fill Area -> Fill (Image)
                         // 我们排除 Fill 对应的 Image 及其子物体
                         var images = timeSlider.GetComponentsInChildren<Image>(true);
                         foreach (var img in images)
                         {
                             // 排除 Fill Rect 及其子物体
                             if (timeSlider.fillRect != null && img.transform.IsChildOf(timeSlider.fillRect)) continue;
                             // 排除 Handle Rect (如果有)
                             if (timeSlider.handleRect != null && img.transform.IsChildOf(timeSlider.handleRect)) continue;
                             
                             // 找到第一个既不是 Fill 也不是 Handle 的 Image，假定它是 Background
                             backgroundImage = img;
                             break;
                         }
                    }
                    else
                    {
                        backgroundImage = bgTrans.GetComponent<Image>();
                    }
                }

                // 尝试自动查找百分比文本
                if (percentageText == null)
                {
                    percentageText = timeSlider.GetComponentInChildren<TextMeshProUGUI>();
                    if (percentageText == null)
                    {
                        percentageText = GetComponentInChildren<TextMeshProUGUI>();
                    }
                }
            }

            // 设置背景颜色（走过的部分）
            if (backgroundImage != null)
            {
                backgroundImage.color = elapsedColor;
            }
            else
            {
                // 仅在运行时警告，避免编辑器里太吵
                if (Application.isPlaying)
                    Debug.LogWarning("[GameCountdownTimer] Background Image not found! 'Elapsed Color' will not work. Please assign it manually.");
            }
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

            // 如果教程正在进行，暂停倒计时
            // 通过 ChatController 检查教程状态
            if (ChatSystem.ChatController.Instance != null && ChatSystem.ChatController.Instance.IsTutorialActive)
            {
                return;
            }

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
            
            if (currentTime <= 0)
            {
                currentTime = 0;
                isRunning = false;
                UpdateUI();
                OnTimerEnd();
            }
            else
            {
                Debug.Log($"[GameCountdownTimer] Reduced {amount}s, Remaining: {currentTime}s");
                UpdateUI();
            }
        }

        private void UpdateUI()
        {
            if (timeSlider != null)
            {
                float ratio = currentTime / totalTime;
                timeSlider.value = ratio;

                // 更新剩余时间颜色渐变
                if (useColorGradient && fillImage != null && remainingTimeGradient != null)
                {
                    fillImage.color = remainingTimeGradient.Evaluate(ratio);
                }

                // 更新百分比文本
                if (percentageText != null)
                {
                    int percentage = Mathf.RoundToInt(ratio * 100f);
                    percentageText.text = $"{percentage}%";
                }
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
