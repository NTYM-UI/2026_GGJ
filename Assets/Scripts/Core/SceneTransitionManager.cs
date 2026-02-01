using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

namespace Core
{
    public class SceneTransitionManager : MonoBehaviour
    {
        private static SceneTransitionManager instance;
        public static SceneTransitionManager Instance
        {
            get
            {
                if (instance == null)
                {
                    // Check if it already exists in scene
                    instance = FindObjectOfType<SceneTransitionManager>();
                    
                    if (instance == null)
                    {
                        GameObject go = new GameObject("SceneTransitionManager");
                        instance = go.AddComponent<SceneTransitionManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return instance;
            }
        }

        private Canvas fadeCanvas;
        private Image fadeImage;
        private CanvasGroup canvasGroup;
        
        [SerializeField] private float fadeDuration = 0.5f;

        private void Awake()
        {
            if (instance != null && instance != this)
            {
                Destroy(gameObject);
                return;
            }
            
            instance = this;
            DontDestroyOnLoad(gameObject);
            
            SetupUI();
        }

        private void SetupUI()
        {
            if (fadeCanvas != null) return;

            // Create Canvas
            GameObject canvasObj = new GameObject("TransitionCanvas");
            canvasObj.transform.SetParent(transform);
            
            fadeCanvas = canvasObj.AddComponent<Canvas>();
            fadeCanvas.renderMode = RenderMode.ScreenSpaceOverlay;
            fadeCanvas.sortingOrder = 9999; // Ensure it's on top
            
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            
            canvasGroup = canvasObj.AddComponent<CanvasGroup>();
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false; // Initially allow clicks

            // Create Image
            GameObject imageObj = new GameObject("FadeImage");
            imageObj.transform.SetParent(canvasObj.transform, false);
            
            fadeImage = imageObj.AddComponent<Image>();
            fadeImage.color = Color.black;
            
            RectTransform rect = imageObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.sizeDelta = Vector2.zero;
            rect.anchoredPosition = Vector2.zero;
        }

        // 全局标志位：是否是从主菜单进入
        // 默认为 true (假设第一次运行就是从主菜单或者直接进游戏场景算作第一次)
        public bool IsFirstTimeFromMenu { get; private set; } = true;

        public void LoadScene(string sceneName)
        {
            // 如果是从 MainMenu 加载 GameScene，则标记为 FirstTime
            if (SceneManager.GetActiveScene().name == "MainMenu" && sceneName == "GameScene")
            {
                IsFirstTimeFromMenu = true;
            }
            // 否则（比如 Restart），标记为 false
            else if (sceneName == SceneManager.GetActiveScene().name)
            {
                IsFirstTimeFromMenu = false;
            }

            StartCoroutine(TransitionRoutine(sceneName));
        }

        private IEnumerator TransitionRoutine(string sceneName)
        {
            // Block input
            canvasGroup.blocksRaycasts = true;

            // Fade In (to black)
            float timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(timer / fadeDuration);
                yield return null;
            }
            canvasGroup.alpha = 1f;

            // Load Scene
            // Use LoadSceneAsync to avoid freeze, though for small scenes it might be fast
            AsyncOperation asyncOp = SceneManager.LoadSceneAsync(sceneName);
            asyncOp.allowSceneActivation = false;

            // Wait until loaded
            while (!asyncOp.progress.Equals(0.9f))
            {
                yield return null;
            }
            
            // Allow activation
            asyncOp.allowSceneActivation = true;
            
            // Wait for scene to actually activate
            while (!asyncOp.isDone)
            {
                yield return null;
            }

            // Fade Out (from black)
            timer = 0f;
            while (timer < fadeDuration)
            {
                timer += Time.deltaTime;
                canvasGroup.alpha = Mathf.Clamp01(1f - (timer / fadeDuration));
                yield return null;
            }
            canvasGroup.alpha = 0f;
            
            // Unblock input
            canvasGroup.blocksRaycasts = false;
        }
    }
}
