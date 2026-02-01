using UnityEngine;
using UnityEngine.SceneManagement;
using Core.EventSystem;

namespace UI
{
    public class GameResultUI : MonoBehaviour
    {
        [Header("UI Panels")]
        public GameObject winPanel;
        public GameObject failPanel;

        [Header("Settings")]
        public string menuSceneName = "MainMenu"; // 主菜单场景名称

        [Header("Animation")]
        [SerializeField] private float popupDuration = 0.3f; // 弹出动画时长
        [Tooltip("弹出动画曲线（建议设置一个稍微超过1的峰值来实现回弹效果）")]
        [SerializeField] private AnimationCurve popupCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.7f, 1.2f), new Keyframe(1, 1)); // 默认回弹曲线

        private System.Collections.Generic.Dictionary<GameObject, Vector3> originalScales = new System.Collections.Generic.Dictionary<GameObject, Vector3>();
        private System.Collections.Generic.Dictionary<GameObject, CanvasGroup> canvasGroups = new System.Collections.Generic.Dictionary<GameObject, CanvasGroup>();
        private System.Collections.Generic.Dictionary<GameObject, Coroutine> activeCoroutines = new System.Collections.Generic.Dictionary<GameObject, Coroutine>();

        private void Awake()
        {
            InitializePanel(winPanel);
            InitializePanel(failPanel);
        }

        private void InitializePanel(GameObject panel)
        {
            if (panel == null) return;

            // 1. 记录原始 Scale
            Vector3 scale = Vector3.one;
            RectTransform rect = panel.GetComponent<RectTransform>();
            if (rect != null)
            {
                if (rect.localScale.x > 0.01f) scale = rect.localScale;
            }
            else
            {
                if (panel.transform.localScale.x > 0.01f) scale = panel.transform.localScale;
            }
            
            if (!originalScales.ContainsKey(panel))
            {
                originalScales.Add(panel, scale);
            }

            // 2. 获取或添加 CanvasGroup
            CanvasGroup cg = panel.GetComponent<CanvasGroup>();
            if (cg == null)
            {
                cg = panel.AddComponent<CanvasGroup>();
            }
            if (!canvasGroups.ContainsKey(panel))
            {
                canvasGroups.Add(panel, cg);
            }

            // 3. 初始隐藏
            cg.alpha = 0f;
            if (rect != null) rect.localScale = Vector3.zero;
            else panel.transform.localScale = Vector3.zero;
            
            // 确保Active为false，等待调用
            panel.SetActive(false);
        }

        private void OnEnable()
        {
            EventManager.Instance.Subscribe(GameEvents.GAME_WIN, OnGameWin);
            EventManager.Instance.Subscribe(GameEvents.GAME_FAIL, OnGameFail);
        }

        private void OnDisable()
        {
            if (EventManager.HasInstance)
            {
                EventManager.Instance.Unsubscribe(GameEvents.GAME_WIN, OnGameWin);
                EventManager.Instance.Unsubscribe(GameEvents.GAME_FAIL, OnGameFail);
            }
        }

        private void OnGameWin(object data)
        {
            Debug.Log("[GameResultUI] Game Win! Showing Win Panel.");
            Core.AudioManager.Instance?.StopMusic(); // 停止背景音乐
            Core.AudioManager.Instance?.PlayVictorySound(); // 播放胜利音效
            ShowPanel(winPanel);
            // 可以在这里添加停止游戏逻辑，例如 Time.timeScale = 0;
        }

        private void OnGameFail(object data)
        {
            Debug.Log("[GameResultUI] Game Fail! Showing Fail Panel.");
            // Core.AudioManager.Instance?.PlayPopupSound(); // 移除通用的弹出音效，改用失败音效
            Core.AudioManager.Instance?.StopMusic(); // 停止背景音乐
            Core.AudioManager.Instance?.PlayFailureSound(); // 播放失败音效
            ShowPanel(failPanel);
            // 可以在这里添加停止游戏逻辑，例如 Time.timeScale = 0;
        }

        private void ShowPanel(GameObject panel)
        {
            if (panel == null) return;
            
            // 确保已被初始化（以防 Awake 没跑或者面板是后来赋值的）
            if (!originalScales.ContainsKey(panel))
            {
                InitializePanel(panel);
            }

            // 停止之前的协程（如果正在运行）
            if (activeCoroutines.ContainsKey(panel))
            {
                if (activeCoroutines[panel] != null)
                {
                    StopCoroutine(activeCoroutines[panel]);
                }
                activeCoroutines.Remove(panel);
            }

            panel.SetActive(true);
            Coroutine co = StartCoroutine(AnimatePopup(panel));
            activeCoroutines.Add(panel, co);
        }

        private System.Collections.IEnumerator AnimatePopup(GameObject panel)
        {
            float timer = 0f;
            Vector3 startScale = Vector3.zero;
            Vector3 targetScale = Vector3.one;
            
            // 从字典获取缓存的数据
            if (originalScales.TryGetValue(panel, out Vector3 scale))
            {
                targetScale = scale;
            }
            
            CanvasGroup cg = null;
            if (canvasGroups.TryGetValue(panel, out CanvasGroup group))
            {
                cg = group;
            }

            RectTransform rect = panel.GetComponent<RectTransform>();
            
            // 初始状态
            if (cg != null) cg.alpha = 0f;
            if (rect != null) rect.localScale = startScale;
            else panel.transform.localScale = startScale;

            while (timer < popupDuration)
            {
                // 使用 unscaledDeltaTime 以防 Time.timeScale 被设为 0
                timer += Time.unscaledDeltaTime;
                float t = timer / popupDuration;
                float curveValue = popupCurve.Evaluate(t);
                
                // 应用缩放
                if (rect != null)
                {
                    rect.localScale = targetScale * curveValue; // 使用乘法以支持回弹（targetScale * >1.0）
                }
                else
                {
                    panel.transform.localScale = targetScale * curveValue;
                }

                // 应用淡入 (Clamp01 确保 alpha 不超过 1)
                if (cg != null)
                {
                    // 让淡入稍微快一点完成，比如在前 80% 时间内完成
                    cg.alpha = Mathf.Clamp01(t / 0.8f);
                }

                yield return null;
            }
            
            // 最终状态确保准确
            if (rect != null) rect.localScale = targetScale;
            else panel.transform.localScale = targetScale;
            
            if (cg != null) cg.alpha = 1f;

            // 动画完成，移除记录
            if (activeCoroutines.ContainsKey(panel))
            {
                activeCoroutines.Remove(panel);
            }
        }

        /// <summary>
        /// 返回主菜单（绑定到按钮点击事件）
        /// </summary>
        public void BackToMenu()
        {
            Debug.Log($"[GameResultUI] Loading Menu Scene: {menuSceneName}");
            // 恢复时间流速（以防之前暂停了）
            Time.timeScale = 1.0f;
            
            // Use SceneTransitionManager if available, otherwise fallback
            if (Core.SceneTransitionManager.Instance != null)
            {
                Core.SceneTransitionManager.Instance.LoadScene(menuSceneName);
            }
            else
            {
                SceneManager.LoadScene(menuSceneName);
            }
        }

        /// <summary>
        /// 重玩当前关卡（可选）
        /// </summary>
        public void RestartLevel()
        {
            Debug.Log("[GameResultUI] Restarting Level");
            Core.AudioManager.Instance?.PlayBackgroundMusic(); // 重新播放背景音乐
            Time.timeScale = 1.0f;
            
            string currentScene = SceneManager.GetActiveScene().name;
            
            // Use SceneTransitionManager if available, otherwise fallback
            if (Core.SceneTransitionManager.Instance != null)
            {
                Core.SceneTransitionManager.Instance.LoadScene(currentScene);
            }
            else
            {
                SceneManager.LoadScene(currentScene);
            }
        }
    }
}
