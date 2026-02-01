using UnityEngine;
using UnityEngine.UI;
using Core.EventSystem;
using TMPro;
using System.Collections;

namespace UI
{
    public class ConsequenceUI : MonoBehaviour
    {
        [Header("UI Components")]
        [SerializeField] private GameObject panel;
        [SerializeField] private TextMeshProUGUI contentText; // Try TMP first
        [SerializeField] private Text legacyText; // Fallback

        [Header("Settings")]
        [SerializeField] private float displayDuration = 1.5f; // 显示多久后开始淡出
        [SerializeField] private float fadeInDuration = 0.3f;  // 弹出动画时间
        [SerializeField] private float fadeDuration = 0.2f;    // 消失动画时间
        
        [Header("Animation")]
        [Tooltip("弹出动画曲线（建议设置一个稍微超过1的峰值来实现回弹效果）")]
        [SerializeField] private AnimationCurve popCurve = new AnimationCurve(new Keyframe(0, 0), new Keyframe(0.8f, 1.1f), new Keyframe(1, 1));
        
        private CanvasGroup canvasGroup;
        private Coroutine currentCoroutine;
        private RectTransform panelRect;
        private Vector3 originalScale = Vector3.one;

        private void Awake()
        {
            if (panel == null) panel = gameObject;
            
            // 获取或添加 CanvasGroup 用于控制透明度
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }
            
            panelRect = panel.GetComponent<RectTransform>();
            if (panelRect != null) originalScale = panelRect.localScale;

            // 默认不阻挡射线（允许穿透点击），因为是自动消失的提示
            canvasGroup.blocksRaycasts = false;
            canvasGroup.alpha = 0f; // 初始设为透明

            // 只有当 panel 不是自身时，才可以在这里安全地 SetActive(false)
            // 如果 panel 是自身，关掉它会导致脚本失效，无法接收事件
            if (panel != gameObject)
            {
                panel.SetActive(false);
            }
        }

        private void OnEnable()
        {
            EventManager.Instance.Subscribe(GameEvents.SHOW_CONSEQUENCE, OnShowConsequence);
        }

        private void OnDisable()
        {
            EventManager.Instance?.Unsubscribe(GameEvents.SHOW_CONSEQUENCE, OnShowConsequence);
        }

        private void OnShowConsequence(object data)
        {
            if (data is string text)
            {
                Show(text);
            }
        }

        public void Show(string text)
        {
            Debug.Log($"[ConsequenceUI] Showing consequence: {text}");
            Core.AudioManager.Instance?.PlayPopupSound();
            
            // 停止之前的流程协程
            if (currentCoroutine != null) StopCoroutine(currentCoroutine);

            // 设置文本
            if (contentText != null)
            {
                contentText.text = text;
            }
            else if (legacyText != null)
            {
                legacyText.text = text;
            }

            // 激活面板并重置状态（准备弹出）
            panel.SetActive(true);
            canvasGroup.alpha = 0f;
            if (panelRect != null) panelRect.localScale = Vector3.zero;

            // 开始完整的显示流程（弹出 -> 等待 -> 消失）
            currentCoroutine = StartCoroutine(ShowSequence());
        }

        private IEnumerator ShowSequence()
        {
            // 1. 弹出动画 (Scale & Fade In)
            float timer = 0f;
            while (timer < fadeInDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / fadeInDuration;
                
                // 应用缩放曲线
                float scale = popCurve.Evaluate(progress);
                if (panelRect != null) panelRect.localScale = originalScale * scale;
                
                // 同时淡入透明度 (线性的)
                canvasGroup.alpha = Mathf.Clamp01(progress / 0.8f); // 稍微快一点显示出来
                
                yield return null;
            }
            
            // 确保最终状态正确
            if (panelRect != null) panelRect.localScale = originalScale;
            canvasGroup.alpha = 1f;

            // 2. 等待展示时间
            yield return new WaitForSeconds(displayDuration);

            // 3. 消失动画 (Shrink & Fade Out)
            timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                float progress = timer / fadeDuration;
                
                // 反向缩放
                float scale = Mathf.Lerp(1f, 0f, progress);
                if (panelRect != null) panelRect.localScale = originalScale * scale;
                
                // 淡出
                canvasGroup.alpha = 1f - progress;
                
                yield return null;
            }
            
            canvasGroup.alpha = 0f;
            if (panelRect != null) panelRect.localScale = Vector3.zero;

            // 4. 关闭面板
            Close();
        }

        public void Close()
        {
            // 如果 panel 不是自身，可以关掉；如果是自身，只隐藏 Alpha
            if (panel != null && panel != gameObject) 
            {
                panel.SetActive(false);
            }
        }
    }
}
