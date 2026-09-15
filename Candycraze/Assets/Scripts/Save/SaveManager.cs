using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
namespace CandyCraze
{
    public class SaveManager : MonoBehaviour
    {
        public static SaveManager Instance { get; private set; }
        public SaveData Data { get; private set; } = new SaveData();
        public event Action OnDataChanged;
        public string AccountId { get; private set; }
        public int CloudRevision { get; private set; }
        public bool HasPendingSave { get; private set; }
        public int SaveGeneration { get; private set; }
        private bool loaded;
        private string Key => string.IsNullOrEmpty(AccountId) ? "guest_v2" : AccountKey(AccountId);
        private string SavePath => Path.Combine(Application.persistentDataPath, Key + ".json");
        [Serializable] private class LocalProfile { public SaveData data; public int revision; public bool pending; }
        private static string AccountKey(string id)
        {
            using (var sha = SHA256.Create())
                return "player_" + BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(id))).Replace("-", "");
        }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            Load();
        }
        // The old shared file is preserved, but is not imported because its owner is unknown.
        public void Load()
        {
            if (loaded) return;
            loaded = true;
            Data = new SaveData();
            Data = ReadProfile(Key, out _)?.data ?? new SaveData();
            Data.Normalise();
            if (Data.GrantStarterBoosters()) Persist();
        }
        private LocalProfile ReadProfile(string key, out string sourceJson)
        {
            string path = Path.Combine(Application.persistentDataPath, key + ".json");
            for (int source = 0; source < 3; source++)
            {
                try
                {
                    string json = source == 1 ? PlayerPrefs.GetString(key, "") :
                        File.Exists(source == 0 ? path : path + ".bak") ? File.ReadAllText(source == 0 ? path : path + ".bak") : "";
                    if (string.IsNullOrWhiteSpace(json)) continue;
                    var profile = JsonUtility.FromJson<LocalProfile>(json);
                    if (profile?.data == null) continue;
                    profile.data.Normalise();
                    sourceJson = json;
                    return profile;
                }
                catch (Exception) { /* Try the independent backup before resetting. */ }
            }
            sourceJson = null;
            return null;
        }
        public void Save()
        {
            SaveGeneration++;
            Data.UpdatedAtUtc = DateTime.UtcNow.ToString("O");
            Data.AppVersion = Application.version;
            if (!string.IsNullOrEmpty(AccountId)) HasPendingSave = true;
            Persist();
            OnDataChanged?.Invoke();
            if (!string.IsNullOrEmpty(AccountId)) CloudSaveManager.Instance?.UploadCurrentSave();
        }
        private void Persist()
        {
            string json = JsonUtility.ToJson(new LocalProfile { data = Data, revision = CloudRevision, pending = HasPendingSave });
            try
            {
                File.WriteAllText(SavePath + ".tmp", json);
                if (File.Exists(SavePath)) File.Replace(SavePath + ".tmp", SavePath, SavePath + ".bak");
                else File.Move(SavePath + ".tmp", SavePath);
            }
            catch (Exception) { Debug.LogWarning("[Save] File backup failed; using preferences backup."); }
            PlayerPrefs.SetString(Key, json);
            PlayerPrefs.Save();
        }
        public bool RestoreAccount(string playerId, string json, int revision)
        {
            if (string.IsNullOrWhiteSpace(playerId)) return false;
            SaveData restored;
            try
            {
                restored = new SaveData();
                if (!string.IsNullOrWhiteSpace(json) && json.Trim() != "{}")
                {
                    if (!json.TrimStart().StartsWith("{")) return false;
                    JsonUtility.FromJsonOverwrite(json, restored);
                }
                restored.Normalise();
            }
            catch (Exception) { return false; }
            bool pending = false;
            string accountPath = Path.Combine(Application.persistentDataPath, AccountKey(playerId) + ".json");
            try
            {
                var local = ReadProfile(AccountKey(playerId), out string localJson);
                if (local?.data != null && local.pending)
                {
                    if (local.revision == revision) { restored = local.data; restored.Normalise(); pending = true; }
                    else
                    {
                        File.WriteAllText(accountPath + ".conflict", localJson);
                        Debug.LogWarning("[Save] Newer cloud progress loaded. Unsent device progress retained in recovery file.");
                    }
                }
            }
            catch (Exception) { Debug.LogWarning("[Save] Account backup unavailable; loading cloud progress."); }
            AccountId = playerId;
            CloudRevision = revision;
            HasPendingSave = pending;
            Data = restored;
            if (Data.GrantStarterBoosters()) { HasPendingSave = true; SaveGeneration++; }
            Persist();
            ApplySettings();
            OnDataChanged?.Invoke();
            return true;
        }
        public void AcknowledgeUpload(int revision, int uploadedGeneration)
        {
            CloudRevision = revision;
            // Live play-time counters change every frame; only explicit Save calls
            // enqueue another upload, preventing a continuous upload loop.
            HasPendingSave = SaveGeneration != uploadedGeneration;
            Persist();
        }
        public void ApplyVerifiedCrystals(int totalPurchased, int revision)
        {
            if (totalPurchased < Data.PurchasedCrystalsTotal || revision < CloudRevision)
                throw new InvalidOperationException("Purchase restore needs a fresh cloud sign-in.");
            Data.Coins = checked(Data.Coins + (totalPurchased - Data.PurchasedCrystalsTotal));
            Data.PurchasedCrystalsTotal = totalPurchased;
            CloudRevision = revision;
            Save();
        }
        public void ResetToGuest()
        {
            AccountId = null; CloudRevision = 0; HasPendingSave = false;
            Data = new SaveData();
            LevelManager.SelectedLevelNumber = 1;
            Data.GrantStarterBoosters();
            Persist();
            ApplySettings();
            OnDataChanged?.Invoke();
        }
        private void ApplySettings()
        {
            AudioManager.Instance?.SetSoundOn(Data.SoundOn);
            AudioManager.Instance?.SetMusicOn(Data.MusicOn);
        }
        public bool TryApplyCloudSave(string json)
        {
            try
            {
                var restored = JsonUtility.FromJson<SaveData>(json);
                if (restored == null) return false;
                restored.Normalise(); restored.GrantStarterBoosters(); Data = restored; Save(); return true;
            }
            catch (Exception) { return false; }
        }
        public string SerializeData() => JsonUtility.ToJson(Data);
        public void DeleteSave() { Data = new SaveData(); Data.GrantStarterBoosters(); Save(); }
        private void OnApplicationPause(bool paused) { if (paused) Save(); }
        private void OnApplicationQuit() { Save(); }
    }
}
