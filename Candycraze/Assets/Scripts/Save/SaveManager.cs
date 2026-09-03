// ============================================================
// SaveManager.cs
// Serialises and deserialises SaveData to/from PlayerPrefs
// using JSON.  Survives scene transitions via DontDestroyOnLoad.
//
// Usage:
//   SaveManager.Instance.Data.Coins += 10;
//   SaveManager.Instance.Save();
// ============================================================

using System.IO;
using UnityEngine;

namespace CandyCraze
{
    public class SaveManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static SaveManager Instance { get; private set; }

        // ── Data ─────────────────────────────────────────────
        public SaveData Data { get; private set; } = new SaveData();

        // File path in persistent storage (reliable on Android; survives
        // app close/reopen far better than PlayerPrefs).
        private static string SavePath =>
            Path.Combine(Application.persistentDataPath, "candycraze_save.json");

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

            // Load immediately on Awake too, so data is ready even if some
            // scene forgets to call Load() (defensive).
            if (!_loaded) Load();
        }

        private bool _loaded;

        // ── Public API ───────────────────────────────────────

        /// <summary>Loads save data from a file (falls back to PlayerPrefs).</summary>
        public void Load()
        {
            _loaded = true;
            try
            {
                // 1. Preferred: JSON file in persistent storage.
                if (File.Exists(SavePath))
                {
                    string json = File.ReadAllText(SavePath);
                    Data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
                    Debug.Log($"[SaveManager] Loaded FILE save (level {Data.CurrentLevel}, " +
                              $"coins {Data.Coins}). Path: {SavePath}");
                    return;
                }

                // 2. Fallback: migrate an old PlayerPrefs save if present.
                if (PlayerPrefs.HasKey(Constants.PREF_SAVE_DATA))
                {
                    string json = PlayerPrefs.GetString(Constants.PREF_SAVE_DATA);
                    Data = JsonUtility.FromJson<SaveData>(json) ?? new SaveData();
                    Debug.Log($"[SaveManager] Migrated PlayerPrefs save (level {Data.CurrentLevel}).");
                    Save(); // write it to the file so future loads use the file
                    return;
                }

                Data = new SaveData();
                Debug.Log("[SaveManager] No save found — starting fresh.");
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[SaveManager] Corrupt/failed load — resetting. ({ex.Message})");
                Data = new SaveData();
            }
        }

        /// <summary>Persists current Data to BOTH a file and PlayerPrefs.</summary>
        public void Save()
        {
            string json;
            try { json = JsonUtility.ToJson(Data, prettyPrint: false); }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveManager] Serialize failed: {ex.Message}");
                return;
            }

            // 1. Write to file (primary, reliable on Android).
            try
            {
                File.WriteAllText(SavePath, json);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[SaveManager] File write failed: {ex.Message}");
            }

            // 2. Also write to PlayerPrefs as a secondary backup.
            try
            {
                PlayerPrefs.SetString(Constants.PREF_SAVE_DATA, json);
                PlayerPrefs.Save();
            }
            catch { /* non-fatal */ }

            Debug.Log($"[SaveManager] Saved (level {Data.CurrentLevel}). File: {SavePath}");
        }

        /// <summary>Wipes all save data (use for testing or a reset-game feature).</summary>
        public void DeleteSave()
        {
            try { if (File.Exists(SavePath)) File.Delete(SavePath); } catch { }
            PlayerPrefs.DeleteKey(Constants.PREF_SAVE_DATA);
            Data = new SaveData();
            Debug.Log("[SaveManager] Save deleted.");
        }

        // Auto-save when the app is paused (goes to background). On Android
        // this is the most RELIABLE save point — OnApplicationQuit is often
        // NOT called when the OS kills a backgrounded app.
        private void OnApplicationPause(bool pause)
        {
            if (pause) Save();
        }

        // Also save when the app loses focus (extra safety on mobile).
        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus) Save();
        }

        private void OnApplicationQuit()
        {
            Save();
        }
    }
}
