// ============================================================
// SaveManager.cs
// Serialises and deserialises SaveData to/from PlayerPrefs
// using JSON.  Survives scene transitions via DontDestroyOnLoad.
//
// Usage:
//   SaveManager.Instance.Data.Coins += 10;
//   SaveManager.Instance.Save();
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    public class SaveManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static SaveManager Instance { get; private set; }

        // ── Data ─────────────────────────────────────────────
        public SaveData Data { get; private set; } = new SaveData();

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ───────────────────────────────────────

        /// <summary>Loads save data from PlayerPrefs.  Call during Bootstrap.</summary>
        public void Load()
        {
            if (!PlayerPrefs.HasKey(Constants.PREF_SAVE_DATA))
            {
                Data = new SaveData();
                Debug.Log("[SaveManager] No save found — starting fresh.");
                return;
            }

            try
            {
                string json = PlayerPrefs.GetString(Constants.PREF_SAVE_DATA);
                Data = JsonUtility.FromJson<SaveData>(json);
                Debug.Log($"[SaveManager] Loaded save (level {Data.CurrentLevel}, " +
                          $"coins {Data.Coins}).");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Corrupt save data — resetting. ({ex.Message})");
                Data = new SaveData();
            }
        }

        /// <summary>Persists current Data to PlayerPrefs.</summary>
        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(Data, prettyPrint: false);
                PlayerPrefs.SetString(Constants.PREF_SAVE_DATA, json);
                PlayerPrefs.Save();
                Debug.Log("[SaveManager] Saved.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveManager] Failed to save: {ex.Message}");
            }
        }

        /// <summary>Wipes all save data (use for testing or a reset-game feature).</summary>
        public void DeleteSave()
        {
            PlayerPrefs.DeleteKey(Constants.PREF_SAVE_DATA);
            Data = new SaveData();
            Debug.Log("[SaveManager] Save deleted.");
        }

        // Auto-save when the app is paused (goes to background)
        private void OnApplicationPause(bool pause)
        {
            if (pause) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
