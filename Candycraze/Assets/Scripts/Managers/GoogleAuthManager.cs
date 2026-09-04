using UnityEngine;
using Google.Play.Games;
using Google.Play.Games.BasicApi;
using System.Collections;

/// <summary>
/// GoogleAuthManager handles Google Sign-In for CandyCraze.
/// Uses Google Play Games plugin for authentication.
/// </summary>
public class GoogleAuthManager : MonoBehaviour
{
    public static GoogleAuthManager Instance { get; private set; }

    [SerializeField] private string backendUrl = "https://candycraze.onrender.com";
    [SerializeField] private bool debugMode = true;

    private string authToken = null;
    private string userId = null;
    private string userEmail = null;
    private string userDisplayName = null;

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
        PlayGamesClientConfiguration config = new PlayGamesClientConfiguration.Builder()
            .RequestIdToken()
            .Build();

        PlayGamesPlatform.InitializeInstance(config);
        PlayGamesPlatform.Activate();

        Log("Google Play Games initialized");

        // Try silent sign-in
        SignInSilently();
    }

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

    /// <summary>
    /// Public method: Sign in with Google (interactive).
    /// Call this when user taps the login button.
    /// </summary>
    public void SignInWithGoogle()
    {
        if (PlayGamesPlatform.Instance.IsAuthenticated())
        {
            Log("Already authenticated");
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
                OnLoginSuccess?.Invoke();
            }
            else
            {
                Log("Interactive sign-in failed: " + status);
                OnLoginFailed?.Invoke();
            }
        });
    }

    /// <summary>
    /// Sign out.
    /// </summary>
    public void SignOut()
    {
        PlayGamesPlatform.Instance.SignOut();
        authToken = null;
        userId = null;
        userEmail = null;
        userDisplayName = null;
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
        return PlayGamesPlatform.Instance.IsAuthenticated();
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
