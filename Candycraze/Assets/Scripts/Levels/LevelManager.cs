// ============================================================
// LevelManager.cs
// Tracks which level is being played, loads LevelData from
// GameConfig, and provides it to other systems.
//
// The currently selected level number is stored in a static
// field so it survives scene transitions without DontDestroyOnLoad.
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    public class LevelManager : MonoBehaviour
    {
        // ── Static level selection ───────────────────────────
        /// <summary>
        /// Set this before loading the Game scene to choose which level to play.
        /// </summary>
        public static int SelectedLevelNumber = 1;

        // ── Runtime data ─────────────────────────────────────
        public LevelData CurrentLevel { get; private set; }

        private GameConfig _config;

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            _config = Resources.Load<GameConfig>("GameConfig");

            if (_config == null)
            {
                Debug.LogError("[LevelManager] GameConfig not found in Resources/!");
                return;
            }

            LoadLevel(SelectedLevelNumber);
        }

        // ── Public API ───────────────────────────────────────

        public void LoadLevel(int levelNumber)
        {
            if (_config == null) return;

            LevelData data = _config.GetLevelData(levelNumber);
            if (data == null)
            {
                Debug.LogError($"[LevelManager] No LevelData for level {levelNumber}. " +
                               $"Config has {_config.TotalLevels} levels.");
                return;
            }

            CurrentLevel = data;
            SelectedLevelNumber = levelNumber;
            Debug.Log($"[LevelManager] Loaded: {data.LevelName}");
        }

        public bool HasNextLevel()
        {
            if (_config == null) return false;
            return SelectedLevelNumber < _config.TotalLevels;
        }

        public void LoadNextLevel()
        {
            if (HasNextLevel())
                LoadLevel(SelectedLevelNumber + 1);
        }
    }
}
