using UnityEngine;
using UnityEngine.Networking;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// CloudSaveManager handles cloud save/load to the CandyCraze backend.
/// Only uploads/downloads when online (network reachability check).
/// </summary>
public class CloudSaveManager : MonoBehaviour
{
    public static CloudSaveManager Instance { get; private set; }

    [SerializeField] private string backendUrl = "https://candycraze.onrender.com";
    [SerializeField] private bool debugMode = true;

    private const string SAVE_ENDPOINT = "/api/save/upload";
    private const string LOAD_ENDPOINT = "/api/save/download";

    // Events
    public delegate void SaveEvent(bool success, string message);
    public event SaveEvent OnSaveComplete;
    public event SaveEvent OnLoadComplete;

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

    /// <summary>
    /// Upload game save to backend.
    /// Only works if online.
    /// </summary>
    public void UploadSave(string saveData)
    {
        // Check network connectivity
        if (!IsOnline())
        {
            Log("Offline: save not uploaded. Will retry when online.");
            OnSaveComplete?.Invoke(false, "Offline - save queued locally");
            return;
        }

        StartCoroutine(UploadSaveCoroutine(saveData));
    }

    /// <summary>
    /// Download game save from backend.
    /// Only works if online.
    /// </summary>
    public void DownloadSave()
    {
        if (!IsOnline())
        {
            Log("Offline: cannot download save");
            OnLoadComplete?.Invoke(false, "Offline - cannot download");
            return;
        }

        StartCoroutine(DownloadSaveCoroutine());
    }

    /// <summary>
    /// Coroutine: Upload save.
    /// </summary>
    private IEnumerator UploadSaveCoroutine(string saveData)
    {
        string token = GoogleAuthManager.Instance.GetAuthToken();

        if (string.IsNullOrEmpty(token))
        {
            Log("No auth token - cannot upload");
            OnSaveComplete?.Invoke(false, "Not authenticated");
            yield break;
        }

        string url = backendUrl + SAVE_ENDPOINT;
        Log($"Uploading to {url}");

        // Prepare JSON payload
        SaveUploadPayload payload = new SaveUploadPayload { saveData = saveData };
        string jsonData = JsonUtility.ToJson(payload);

        using (UnityWebRequest request = new UnityWebRequest(url, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonData);
            request.uploadHandler = new UploadRawData(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Log("Save uploaded successfully");
                OnSaveComplete?.Invoke(true, "Save uploaded");
            }
            else
            {
                Log($"Upload failed: {request.error} - {request.downloadHandler.text}");
                OnSaveComplete?.Invoke(false, "Upload failed: " + request.error);
            }
        }
    }

    /// <summary>
    /// Coroutine: Download save.
    /// </summary>
    private IEnumerator DownloadSaveCoroutine()
    {
        string token = GoogleAuthManager.Instance.GetAuthToken();

        if (string.IsNullOrEmpty(token))
        {
            Log("No auth token - cannot download");
            OnLoadComplete?.Invoke(false, "Not authenticated");
            yield break;
        }

        string url = backendUrl + LOAD_ENDPOINT;
        Log($"Downloading from {url}");

        using (UnityWebRequest request = UnityWebRequest.Get(url))
        {
            request.SetRequestHeader("Authorization", "Bearer " + token);

            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                string responseText = request.downloadHandler.text;
                Log($"Save downloaded: {responseText}");

                // Parse response
                SaveDownloadPayload response = JsonUtility.FromJson<SaveDownloadPayload>(responseText);
                OnLoadComplete?.Invoke(true, response.saveData);
            }
            else
            {
                Log($"Download failed: {request.error} - {request.downloadHandler.text}");
                OnLoadComplete?.Invoke(false, "Download failed: " + request.error);
            }
        }
    }

    /// <summary>
    /// Check if device is online.
    /// </summary>
    private bool IsOnline()
    {
        NetworkReachability reachability = Application.internetReachability;
        bool online = reachability != NetworkReachability.NotReachable;

        if (!online)
            Log("Device is OFFLINE");

        return online;
    }

    /// <summary>
    /// Debug logging.
    /// </summary>
    private void Log(string message)
    {
        if (debugMode)
            Debug.Log("[CloudSaveManager] " + message);
    }

    // ============ Serializable Payload Classes ============
    [System.Serializable]
    private class SaveUploadPayload
    {
        public string saveData;
    }

    [System.Serializable]
    private class SaveDownloadPayload
    {
        public string saveData;
    }
}
