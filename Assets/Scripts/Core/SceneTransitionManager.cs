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

        public void LoadScene(string sceneName)
        {
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
