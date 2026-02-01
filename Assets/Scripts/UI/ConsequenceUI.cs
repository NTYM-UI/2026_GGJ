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
        [SerializeField] private float fadeInDuration = 0.5f;  // 淡入过程持续时间
        [SerializeField] private float fadeDuration = 0.5f;    // 淡出过程持续时间

        private CanvasGroup canvasGroup;
        private Coroutine currentCoroutine;

        private void Awake()
        {
            if (panel == null) panel = gameObject;
            
            // 获取或添加 CanvasGroup 用于控制透明度
            canvasGroup = panel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = panel.AddComponent<CanvasGroup>();
            }
            
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
            if (EventManager.HasInstance)
            {
                EventManager.Instance.Unsubscribe(GameEvents.SHOW_CONSEQUENCE, OnShowConsequence);
            }
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

            // 激活面板并重置透明度为0（准备淡入）
            panel.SetActive(true);
            canvasGroup.alpha = 0f;

            // 开始完整的显示流程（淡入 -> 等待 -> 淡出）
            currentCoroutine = StartCoroutine(ShowSequence());
        }

        private IEnumerator ShowSequence()
        {
            // 1. 淡入
            float timer = 0f;
            while (timer < fadeInDuration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(0f, 1f, timer / fadeInDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // 2. 等待展示时间
            yield return new WaitForSeconds(displayDuration);

            // 3. 淡出
            timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, timer / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 0f;

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
