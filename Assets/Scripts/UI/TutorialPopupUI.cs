using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class TutorialPopupUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Button closeButton;
        [SerializeField] private GameObject contentPanel; // 教程内容的父物体

        [Header("Animation")]
        [SerializeField] private float popupDuration = 0.5f; // 弹出动画时长
        [SerializeField] private AnimationCurve popupCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 弹出曲线
        [SerializeField] private float closeDuration = 0.3f; // 关闭动画时长
        [SerializeField] private AnimationCurve closeCurve = AnimationCurve.EaseInOut(0, 0, 1, 1); // 关闭曲线

        private void Awake()
        {
            // 在游戏开始时，通知 ChatController 暂停消息发送
            // (双重保险：ChatController 也会主动查，这里保留是为了防止 ChatController 先初始化完后教程才被动态加载的情况)
            if (ChatSystem.ChatController.Instance != null)
            {
                // 同样需要检查是否应该显示教程
                bool showTutorial = true;
                if (Core.SceneTransitionManager.Instance != null)
                {
                    showTutorial = Core.SceneTransitionManager.Instance.IsFirstTimeFromMenu;
                }

                if (showTutorial)
                {
                    ChatSystem.ChatController.Instance.SetTutorialActive(true);
                }
            }
            
            if (closeButton != null)
            {
                closeButton.onClick.AddListener(OnCloseButtonClicked);
            }

            // 确保一开始 Scale 为 0，避免在 Start 运行前闪烁
            if (contentPanel != null)
            {
                contentPanel.transform.localScale = Vector3.zero;
                contentPanel.SetActive(true); 
            }
        }

        private void Start()
        {
            // 再次检查是否应该显示
            bool showTutorial = true;
            if (Core.SceneTransitionManager.Instance != null)
            {
                showTutorial = Core.SceneTransitionManager.Instance.IsFirstTimeFromMenu;
            }

            if (!showTutorial)
            {
                gameObject.SetActive(false);
                return;
            }

            // 确保教程面板是显示的
            if (contentPanel != null)
            {
                contentPanel.SetActive(true);
                // 开始弹出动画
                StartCoroutine(AnimatePopup());
            }
        }

        private System.Collections.IEnumerator AnimatePopup()
        {
            float timer = 0f;
            Vector3 startScale = Vector3.zero;
            Vector3 endScale = Vector3.one;

            contentPanel.transform.localScale = startScale;

            while (timer < popupDuration)
            {
                timer += Time.deltaTime;
                float t = timer / popupDuration;
                float curveValue = popupCurve.Evaluate(t);
                
                contentPanel.transform.localScale = Vector3.LerpUnclamped(startScale, endScale, curveValue);
                yield return null;
            }
            
            contentPanel.transform.localScale = endScale;
        }

        private void OnCloseButtonClicked()
        {
            // 避免重复点击
            if (closeButton != null) closeButton.interactable = false;

            if (contentPanel != null)
            {
                StartCoroutine(AnimateClose());
            }
            else
            {
                OnCloseAnimationFinished();
            }
        }

        private System.Collections.IEnumerator AnimateClose()
        {
            float timer = 0f;
            Vector3 startScale = contentPanel.transform.localScale;
            Vector3 endScale = Vector3.zero;

            while (timer < closeDuration)
            {
                timer += Time.deltaTime;
                float t = timer / closeDuration;
                float curveValue = closeCurve.Evaluate(t);
                
                contentPanel.transform.localScale = Vector3.LerpUnclamped(startScale, endScale, curveValue);
                yield return null;
            }
            
            contentPanel.transform.localScale = endScale;
            OnCloseAnimationFinished();
        }

        private void OnCloseAnimationFinished()
        {
            // 关闭面板
            if (contentPanel != null)
            {
                contentPanel.SetActive(false);
            }
            else
            {
                gameObject.SetActive(false);
            }

            // 通知 ChatController 教程结束，可以开始发消息了
            if (ChatSystem.ChatController.Instance != null)
            {
                ChatSystem.ChatController.Instance.CompleteTutorial();
            }
        }
    }
}
