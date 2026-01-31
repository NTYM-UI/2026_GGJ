namespace Core.EventSystem
{
    /// <summary>
    /// 定义所有的事件名称常量，避免使用字符串硬编码
    /// </summary>
    public static class GameEvents
    {
        // 游戏流程相关
        public const string ON_GAME_START = "OnGameStart";
        public const string ON_GAME_PAUSE = "OnGamePause";
        public const string ON_GAME_OVER = "OnGameOver";

        // 玩家相关
        public const string ON_PLAYER_SPAWN = "OnPlayerSpawn";
        public const string ON_PLAYER_DEATH = "OnPlayerDeath";

        // UI 相关
        public const string ON_UI_OPEN = "OnUIOpen";
        public const string ON_UI_CLOSE = "OnUIClose";

        // 对话系统相关
        public const string DIALOG_START = "DialogStart";
        public const string DIALOG_END = "DialogEnd";
        public const string ON_DIALOG = "OnDialog"; // 对话进行中（如下一句）
        public const string DIALOG_TYPE_SOUND = "DialogTypeSound"; // 打字音效
    }
}
