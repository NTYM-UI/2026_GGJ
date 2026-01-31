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

        private void OnEnable()
        {
            EventManager.Instance.Subscribe(GameEvents.GAME_WIN, OnGameWin);
            EventManager.Instance.Subscribe(GameEvents.GAME_FAIL, OnGameFail);
        }

        private void OnDisable()
        {
            if (EventManager.Instance != null)
            {
                EventManager.Instance.Unsubscribe(GameEvents.GAME_WIN, OnGameWin);
                EventManager.Instance.Unsubscribe(GameEvents.GAME_FAIL, OnGameFail);
            }
        }

        private void OnGameWin(object data)
        {
            Debug.Log("[GameResultUI] Game Win! Showing Win Panel.");
            if (winPanel != null)
            {
                winPanel.SetActive(true);
            }
            // 可以在这里添加停止游戏逻辑，例如 Time.timeScale = 0;
        }

        private void OnGameFail(object data)
        {
            Debug.Log("[GameResultUI] Game Fail! Showing Fail Panel.");
            if (failPanel != null)
            {
                failPanel.SetActive(true);
            }
            // 可以在这里添加停止游戏逻辑，例如 Time.timeScale = 0;
        }

        /// <summary>
        /// 返回主菜单（绑定到按钮点击事件）
        /// </summary>
        public void BackToMenu()
        {
            Debug.Log($"[GameResultUI] Loading Menu Scene: {menuSceneName}");
            // 恢复时间流速（以防之前暂停了）
            Time.timeScale = 1.0f;
            SceneManager.LoadScene(menuSceneName);
        }

        /// <summary>
        /// 重玩当前关卡（可选）
        /// </summary>
        public void RestartLevel()
        {
            Debug.Log("[GameResultUI] Restarting Level");
            Time.timeScale = 1.0f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
