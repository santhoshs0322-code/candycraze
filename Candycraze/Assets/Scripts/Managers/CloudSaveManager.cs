using System;
using System.Collections;
using CandyCraze;
using UnityEngine;
using UnityEngine.Networking;

public class CloudSaveManager : MonoBehaviour
{
    public static CloudSaveManager Instance { get; private set; }
    [SerializeField] private string backendUrl = "https://candycraze.onrender.com";
    private string sessionToken;
    private bool syncing, conflict;
    private float nextRetry;
    public string PlayerId { get; private set; }
    public string DisplayName { get; private set; }
    public bool IsSignedIn => !string.IsNullOrEmpty(sessionToken);
    public string StatusMessage { get; private set; } = "Guest progress stays on this device.";
    public delegate void SaveEvent(bool success, string message);
    public event SaveEvent OnSaveComplete, OnLoadComplete;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; transform.SetParent(null); DontDestroyOnLoad(gameObject);
    }
    public void SignInAndRestore(string code, Action<bool, string> completed)
    {
        if (syncing) { completed?.Invoke(false, "A connection is already in progress."); return; }
        if (Application.internetReachability == NetworkReachability.NotReachable)
        { completed?.Invoke(false, "No internet connection. Please try again when online."); return; }
        StartCoroutine(Login(code, completed));
    }
    private IEnumerator Login(string code, Action<bool, string> completed)
    {
        syncing = true;
        using (var request = Request("/api/auth/play-games", JsonUtility.ToJson(new LoginRequest { authorizationCode = code, appVersion = Application.version })))
        {
            yield return request.SendWebRequest();
            LoginResponse response = null;
            try { response = JsonUtility.FromJson<LoginResponse>(request.downloadHandler.text); } catch (Exception) { }
            if (request.result != UnityWebRequest.Result.Success || response == null || !response.success ||
                string.IsNullOrEmpty(response.sessionToken) || string.IsNullOrEmpty(response.user?.playerId) ||
                SaveManager.Instance == null)
            {
                syncing = false;
                completed?.Invoke(false, response?.error ?? "Cloud sign-in unavailable. Please retry.");
                yield break;
            }
            if (!SaveManager.Instance.RestoreAccount(response.user.playerId, response.user.saveData, response.revision))
            {
                syncing = false;
                completed?.Invoke(false, "Cloud progress is invalid. Your guest progress is unchanged.");
                yield break;
            }
            sessionToken = response.sessionToken;
            PlayerId = response.user.playerId; DisplayName = response.user.displayName;
            conflict = false; syncing = false;
            StatusMessage = SaveManager.Instance.HasPendingSave ? "Syncing device progress..." : "Cloud progress is up to date.";
            OnLoadComplete?.Invoke(true, StatusMessage);
            completed?.Invoke(true, "Your saved adventure is ready.");
            UploadCurrentSave();
        }
    }
    public void UploadCurrentSave()
    {
        if (!IsSignedIn || syncing || conflict || SaveManager.Instance == null || !SaveManager.Instance.HasPendingSave) return;
        if (Application.internetReachability == NetworkReachability.NotReachable)
        { StatusMessage = "Offline. Progress backed up on this device."; return; }
        StartCoroutine(Upload());
    }
    private IEnumerator Upload()
    {
        syncing = true;
        string json = SaveManager.Instance.SerializeData();
        int generation = SaveManager.Instance.SaveGeneration;
        var body = new UploadRequest { sessionToken = sessionToken, saveData = json, revision = SaveManager.Instance.CloudRevision };
        StatusMessage = "Saving your adventure...";
        using (var request = Request("/api/save/upload", JsonUtility.ToJson(body)))
        {
            yield return request.SendWebRequest();
            Response response = null;
            try { response = JsonUtility.FromJson<Response>(request.downloadHandler.text); } catch (Exception) { }
            syncing = false;
            nextRetry = Time.realtimeSinceStartup + 15;
            if (request.result == UnityWebRequest.Result.Success && response != null && response.success)
            {
                SaveManager.Instance.AcknowledgeUpload(response.revision, generation);
                StatusMessage = "Cloud progress is up to date.";
                OnSaveComplete?.Invoke(true, StatusMessage);
                UploadCurrentSave(); // Do not drop changes made while the previous save was in flight.
            }
            else
            {
                conflict = request.responseCode == 409 || request.responseCode == 401;
                StatusMessage = conflict ? "Session changed. Sign out and sign in to load the latest cloud save."
                    : "Upload delayed. Progress backed up on this device; retrying.";
                OnSaveComplete?.Invoke(false, StatusMessage);
            }
        }
    }
    private void Update()
    {
        if (Time.realtimeSinceStartup >= nextRetry)
        { nextRetry = Time.realtimeSinceStartup + 15; UploadCurrentSave(); }
    }
    public void SignOut()
    {
        if (IsSignedIn && SaveManager.Instance != null) SaveManager.Instance.Save();
        string oldToken = sessionToken;
        StopAllCoroutines(); // Cancels restore/upload callbacks before switching profile.
        sessionToken = null; PlayerId = DisplayName = null; syncing = conflict = false;
        StatusMessage = "Guest progress stays on this device.";
        if (!string.IsNullOrEmpty(oldToken)) StartCoroutine(Revoke(oldToken));
    }
    private IEnumerator Revoke(string token)
    {
        using (var request = Request("/api/auth/logout", JsonUtility.ToJson(new LogoutRequest { sessionToken = token })))
            yield return request.SendWebRequest();
    }
    private UnityWebRequest Request(string path, string json)
    {
        var request = new UnityWebRequest(backendUrl.TrimEnd('/') + path, "POST")
        {
            uploadHandler = new UploadHandlerRaw(System.Text.Encoding.UTF8.GetBytes(json)),
            downloadHandler = new DownloadHandlerBuffer(), timeout = 60
        };
        request.SetRequestHeader("Content-Type", "application/json");
        return request;
    }
    [Serializable] private class LoginRequest { public string authorizationCode, appVersion; }
    [Serializable] private class LogoutRequest { public string sessionToken; }
    [Serializable] private class UploadRequest { public string sessionToken, saveData; public int revision; }
    [Serializable] private class Response { public bool success; public string error; public int revision; }
    [Serializable] private class LoginResponse : Response { public string sessionToken; public CloudUser user; }
    [Serializable] private class CloudUser { public string playerId, displayName, saveData; }
}
