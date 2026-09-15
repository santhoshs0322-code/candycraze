using System;
using CandyCraze;
using UnityEngine;
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
        int current = ++operation;
        SetState(true, "Connecting to Google Play Games...");
        deadline = Time.realtimeSinceStartup + 100;
#if GPGS_PRESENT && UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            PlayGamesPlatform.Activate().ManuallyAuthenticate(status =>
            {
                if (current != operation) return;
                if (status != SignInStatus.Success)
                {
                    Debug.LogWarning("[GoogleAuth] Play Games authentication failed: " + status);
                    Fail(status == SignInStatus.Canceled
                        ? "Google Play Games sign-in was not completed. Please try again. Guest play is available."
                        : "Google Play Games could not sign in (" + status + "). Please contact support. Guest play is available.");
                    return;
                }
                try
                {
                    SetState(true, "Restoring your saved adventure...");
                    PlayGamesPlatform.Instance.RequestServerSideAccess(false, code =>
                    {
                        if (current != operation) return;
                        if (string.IsNullOrEmpty(code)) { Fail("No server authorization code. Check the Play Games web client setup."); return; }
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
                catch (Exception) { Fail("Unable to request cloud access. Please try again."); }
            });
        }
        catch (Exception) { Fail("Google Play Games could not start. Please try again."); }
#else
        Fail("Google sign-in is available in the installed Android app.");
#endif
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
