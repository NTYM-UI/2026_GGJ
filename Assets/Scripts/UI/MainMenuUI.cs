using UnityEngine;
using UnityEngine.SceneManagement;

namespace UI
{
    public class MainMenuUI : MonoBehaviour
    {
        [Header("Scene Settings")]
        [Tooltip("点击开始游戏时加载的场景名称")]
        [SerializeField] private string gameSceneName = "GameScene"; 

        /// <summary>
        /// 开始游戏按钮点击事件
        /// </summary>
        public void OnStartButtonClick()
        {
            Debug.Log($"[MainMenuUI] Start Game clicked. Loading scene: {gameSceneName}");
            // Play sound
            Core.AudioManager.Instance?.PlayButtonSound();
            // Use Transition Manager
            Core.SceneTransitionManager.Instance.LoadScene(gameSceneName);
        }

        /// <summary>
        /// 退出游戏按钮点击事件
        /// </summary>
        public void OnExitButtonClick()
        {
            Debug.Log("[MainMenuUI] Exit Game clicked.");
            // Play sound
            Core.AudioManager.Instance?.PlayButtonSound();
            Application.Quit();
            
            #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
            #endif
        }
    }
}
