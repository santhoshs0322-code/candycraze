using System;
using System.Collections;
using CandyCraze;
using UnityEngine;
using UnityEngine.Networking;
#if GPGS_PRESENT && UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
/// <summary>Explicit CandyCraze session; does not sign out the device's Play Games identity.</summary>
public class GoogleAuthManager : MonoBehaviour
{
    public static GoogleAuthManager Instance { get; private set; }
    private string userId, userDisplayName;
    private bool isAuthenticated;
    private int operation;
    private float deadline;
    public bool IsBusy { get; private set; }
    public string StatusMessage { get; private set; } = "Play as guest, or sign in to save your adventure.";
    public delegate void AuthEvent();
    public event AuthEvent OnLoginSuccess, OnLoginFailed, OnLogoutSuccess, OnStateChanged;
    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this; transform.SetParent(null); DontDestroyOnLoad(gameObject);
    }
    public void SignInWithGoogle()
    {
        if (IsBusy || isAuthenticated) return;
        ReportSignInStage("button-clicked");
        int current = ++operation;
        SetState(true, "Connecting to Google Play Games...");
        deadline = Time.realtimeSinceStartup + 100;
#if GPGS_PRESENT && UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            PlayGamesPlatform.DebugLogEnabled = true;
            PlayGamesPlatform.Activate().ManuallyAuthenticate(status =>
            {
                if (current != operation) return;
                if (status != SignInStatus.Success)
                {
                    ReportSignInStage("play-games-failed", status.ToString());
                    Debug.LogWarning("[GoogleAuth] Play Games authentication failed: " + status);
                    Fail(status == SignInStatus.Canceled
                        ? "Google Play Games sign-in was not completed. Please try again. Guest play is available."
                        : "Google Play Games could not sign in (" + status + "). Please contact support. Guest play is available.");
                    return;
                }
                ReportSignInStage("play-games-success");
                try
                {
                    SetState(true, "Restoring your saved adventure...");
                    ReportSignInStage("server-code-requested");
                    PlayGamesPlatform.Instance.RequestServerSideAccess(false, code =>
                    {
                        if (current != operation) return;
                        if (string.IsNullOrEmpty(code))
                        {
                            ReportSignInStage("server-code-missing");
                            Fail("No server authorization code. Check the Play Games web client setup.");
                            return;
                        }
                        ReportSignInStage("backend-signin-start");
                        if (CloudSaveManager.Instance == null) new GameObject("CloudSaveManager").AddComponent<CloudSaveManager>();
                        CloudSaveManager.Instance.SignInAndRestore(code, (success, message) =>
                        {
                            if (current != operation) return;
                            if (!success) { Fail(message); return; }
                            userId = CloudSaveManager.Instance.PlayerId;
                            userDisplayName = CloudSaveManager.Instance.DisplayName;
                            isAuthenticated = true;
                            SetState(false, message);
                            OnLoginSuccess?.Invoke();
                        });
                    });
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                    Fail("Unable to request cloud access: " + exception.Message);
                }
            });
        }
        catch (Exception exception)
        {
            Debug.LogException(exception);
            Fail("Google Play Games could not start: " + exception.Message);
        }
#else
        Fail("Google sign-in is available in the installed Android app.");
#endif
    }
    private void ReportSignInStage(string stage, string detail = "")
    {
        if (isActiveAndEnabled) StartCoroutine(SendSignInStage(stage, detail));
    }
    private IEnumerator SendSignInStage(string stage, string detail)
    {
        string backend = GoogleAuthConfig.Instance != null
            ? GoogleAuthConfig.Instance.backendUrl.TrimEnd('/')
            : "https://candycraze.onrender.com";
        string safeStage = stage.Replace("\\", "").Replace("\"", "");
        string safeDetail = detail.Replace("\\", "").Replace("\"", "");
        string safeVersion = Application.version.Replace("\\", "").Replace("\"", "");
        string safePackage = Application.identifier.Replace("\\", "").Replace("\"", "");
        byte[] body = System.Text.Encoding.UTF8.GetBytes(
            "{\"stage\":\"" + safeStage + "\",\"detail\":\"" + safeDetail +
            "\",\"packageName\":\"" + safePackage + "\",\"appVersion\":\"" + safeVersion + "\"}");
        using (var request = new UnityWebRequest(backend + "/api/diagnostics/signin-attempt", "POST"))
        {
            request.uploadHandler = new UploadHandlerRaw(body);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.timeout = 15;
            yield return request.SendWebRequest();
            if (request.result != UnityWebRequest.Result.Success)
                Debug.LogWarning("[GoogleAuth] Backend diagnostic did not arrive: " + request.error);
        }
    }
    public void SignOut()
    {
        ++operation;
        CloudSaveManager.Instance?.SignOut();
        userId = userDisplayName = null;
        isAuthenticated = false;
        SaveManager.Instance?.ResetToGuest();
        SetState(false, "Signed out. A fresh guest adventure is ready.");
        OnLogoutSuccess?.Invoke();
    }
    private void Update()
    {
        if (IsBusy && Time.realtimeSinceStartup > deadline) Fail("Sign-in timed out. Please retry.");
    }
    private void Fail(string message)
    {
        ++operation;
        CloudSaveManager.Instance?.SignOut();
        isAuthenticated = false; userId = userDisplayName = null;
        SetState(false, message);
        OnLoginFailed?.Invoke();
    }
    private void SetState(bool busy, string message) { IsBusy = busy; StatusMessage = message; OnStateChanged?.Invoke(); }
    public string GetAuthToken() => null; // Never retain a single-use authorization code.
    public string GetUserId() => userId;
    public string GetUserEmail() => string.Empty;
    public string GetUserDisplayName() => userDisplayName;
    public bool IsAuthenticated() => isAuthenticated;
}
