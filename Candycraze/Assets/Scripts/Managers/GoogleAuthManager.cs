using UnityEngine;
#if GPGS_PRESENT && UNITY_ANDROID && !UNITY_EDITOR
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif

public class GoogleAuthManager : MonoBehaviour
{
    public static GoogleAuthManager Instance { get; private set; }
    [SerializeField] private bool debugMode = true;
    private string authorizationCode;
    private string userId;
    private string userDisplayName;
    private bool isAuthenticated;
    public delegate void AuthEvent();
    public event AuthEvent OnLoginSuccess;
    public event AuthEvent OnLoginFailed;
    public event AuthEvent OnLogoutSuccess;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
#if GPGS_PRESENT && UNITY_ANDROID && !UNITY_EDITOR
        PlayGamesPlatform.Activate();
        PlayGamesPlatform.Instance.Authenticate(HandleAutomaticSignIn);
#else
        Log("Google Play Games is available only in an Android build. Editor stub is active.");
#endif
    }

#if GPGS_PRESENT && UNITY_ANDROID && !UNITY_EDITOR
    private void HandleAutomaticSignIn(SignInStatus status)
    {
        if (status == SignInStatus.Success) CompleteSignIn();
        else Log("Automatic sign-in did not succeed: " + status);
    }

    private void CompleteSignIn()
    {
        userId = PlayGamesPlatform.Instance.GetUserId();
        userDisplayName = PlayGamesPlatform.Instance.GetUserDisplayName();
        isAuthenticated = true;
        PlayGamesPlatform.Instance.RequestServerSideAccess(false, code =>
        {
            authorizationCode = code;
            Log("Google Play Games sign-in successful for " + userDisplayName + ".");
            OnLoginSuccess?.Invoke();
        });
    }
#endif

    public void SignInWithGoogle()
    {
#if GPGS_PRESENT && UNITY_ANDROID && !UNITY_EDITOR
        if (PlayGamesPlatform.Instance.IsAuthenticated()) { CompleteSignIn(); return; }
        PlayGamesPlatform.Instance.ManuallyAuthenticate(status =>
        {
            if (status == SignInStatus.Success) CompleteSignIn();
            else { Log("Google Play Games sign-in failed: " + status); OnLoginFailed?.Invoke(); }
        });
#else
        Log("Google Play Games sign-in can be tested only in an Android player build.");
        OnLoginFailed?.Invoke();
#endif
    }

    public void SignOut()
    {
        authorizationCode = null; userId = null; userDisplayName = null; isAuthenticated = false;
        OnLogoutSuccess?.Invoke();
    }

    public string GetAuthToken() => authorizationCode;
    public string GetUserId() => userId;
    public string GetUserEmail() => string.Empty;
    public string GetUserDisplayName() => userDisplayName;
    public bool IsAuthenticated() => isAuthenticated;
    private void Log(string message) { if (debugMode) Debug.Log("[GoogleAuthManager] " + message); }
}
