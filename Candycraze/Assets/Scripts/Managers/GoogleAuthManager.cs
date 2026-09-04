using UnityEngine;

#if GPGS_PRESENT
using Google.Play.Games;
using Google.Play.Games.BasicApi;
#endif

/// <summary>
/// GoogleAuthManager handles Google Sign-In for CandyCraze.
/// Uses Google Play Games plugin for authentication.
///
/// NOTE: This script compiles even before the Google Play Games plugin is
/// imported. Once you import the plugin (Assets > Import Package or via
/// Package Manager), add the scripting define symbol "GPGS_PRESENT" in
/// Project Settings > Player > Other Settings > Scripting Define Symbols
/// to activate the real authentication code below.
/// </summary>
public class GoogleAuthManager : MonoBehaviour
{
    public static GoogleAuthManager Instance { get; private set; }

    [SerializeField] private string backendUrl = "https://candycraze.onrender.com";
    [SerializeField] private bool debugMode = true;

    // Your Google Client ID
    private const string GOOGLE_CLIENT_ID = "787050963672-n0c7i29los0rshojcve4ckal097bhagd.apps.googleusercontent.com";

    private string authToken = null;
    private string userId = null;
    private string userEmail = null;
    private string userDisplayName = null;
    private bool isAuthenticated = false;

    // Events
    public delegate void AuthEvent();
    public event AuthEvent OnLoginSuccess;
    public event AuthEvent OnLoginFailed;
    public event AuthEvent OnLogoutSuccess;

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

    private void Start()
    {
        InitializeGooglePlayGames();
    }

    /// <summary>
    /// Initialize Google Play Games and sign in silently if possible.
    /// </summary>
    private void InitializeGooglePlayGames()
    {
#if GPGS_PRESENT
        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
            .RequestIdToken()
            .Build();

        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.Activate();

        Log("Google Play Games initialized");

        // Try silent sign-in
        SignInSilently();
#else
        Log("Google Play Games plugin not imported yet. " +
            "Import the plugin and add 'GPGS_PRESENT' to Scripting Define Symbols to enable real sign-in.");
#endif
    }

#if GPGS_PRESENT
    /// <summary>
    /// Attempt silent sign-in (user already logged in on device).
    /// </summary>
    private void SignInSilently()
    {
        PlayGamesPlatform.Instance.Authenticate(SignInCallback);
    }

    /// <summary>
    /// Callback for silent sign-in attempt.
    /// </summary>
    private void SignInCallback(SignInStatus status)
    {
        if (status == SignInStatus.Success)
        {
            Log("Silent sign-in successful");
            ExtractUserInfo();
            isAuthenticated = true;
            OnLoginSuccess?.Invoke();
        }
        else
        {
            Log("Silent sign-in failed: " + status);
            // User will need to tap login button
        }
    }

    /// <summary>
    /// Extract user info after successful authentication.
    /// </summary>
    private void ExtractUserInfo()
    {
        userId = PlayGamesPlatform.Instance.GetUserId();
        userDisplayName = PlayGamesPlatform.Instance.GetUserDisplayName();
        userEmail = PlayGamesPlatform.Instance.GetUserEmail();

        // Get ID token for backend verification
        string idToken = PlayGamesPlatform.Instance.GetIdToken();
        authToken = idToken;

        Log($"User: {userDisplayName} | Email: {userEmail} | ID: {userId}");
    }
#endif

    /// <summary>
    /// Public method: Sign in with Google (interactive).
    /// Call this when user taps the login button.
    /// </summary>
    public void SignInWithGoogle()
    {
#if GPGS_PRESENT
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            Log("Already authenticated");
            isAuthenticated = true;
            OnLoginSuccess?.Invoke();
            return;
        }

        Log("Starting interactive sign-in...");
        PlayGamesPlatform.Instance.Authenticate(status =>
        {
            if (status == SignInStatus.Success)
            {
                Log("Interactive sign-in successful");
                ExtractUserInfo();
                isAuthenticated = true;
                OnLoginSuccess?.Invoke();
            }
            else
            {
                Log("Interactive sign-in failed: " + status);
                isAuthenticated = false;
                OnLoginFailed?.Invoke();
            }
        });
#else
        Log("Cannot sign in: Google Play Games plugin not imported yet.");
        OnLoginFailed?.Invoke();
#endif
    }

    /// <summary>
    /// Sign out.
    /// </summary>
    public void SignOut()
    {
#if GPGS_PRESENT
        PlayGamesPlatform.Instance.SignOut();
#endif
        authToken = null;
        userId = null;
        userEmail = null;
        userDisplayName = null;
        isAuthenticated = false;
        Log("Signed out");
        OnLogoutSuccess?.Invoke();
    }

    /// <summary>
    /// Get current auth token (for backend requests).
    /// </summary>
    public string GetAuthToken()
    {
        return authToken;
    }

    /// <summary>
    /// Get current user ID.
    /// </summary>
    public string GetUserId()
    {
        return userId;
    }

    /// <summary>
    /// Get current user email.
    /// </summary>
    public string GetUserEmail()
    {
        return userEmail;
    }

    /// <summary>
    /// Get current user display name.
    /// </summary>
    public string GetUserDisplayName()
    {
        return userDisplayName;
    }

    /// <summary>
    /// Check if user is authenticated.
    /// </summary>
    public bool IsAuthenticated()
    {
#if GPGS_PRESENT
        return PlayGamesPlatform.Instance.IsAuthenticated();
#else
        return isAuthenticated;
#endif
    }

    /// <summary>
    /// Debug logging.
    /// </summary>
    private void Log(string message)
    {
        if (debugMode)
            Debug.Log("[GoogleAuthManager] " + message);
    }
}
