using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// LoginUI manages the login UI panel and profile icon.
/// Shows login button when offline, profile when logged in.
/// </summary>
public class LoginUI : MonoBehaviour
{
    [SerializeField] private Button profileIconButton;
    [SerializeField] private Image profileIconImage;
    [SerializeField] private TextMeshProUGUI userNameText;

    [SerializeField] private GameObject loginPanel;
    [SerializeField] private Button signInButton;
    [SerializeField] private Button signOutButton;
    [SerializeField] private TextMeshProUGUI statusText;

    [SerializeField] private Color onlineColor = Color.green;
    [SerializeField] private Color offlineColor = Color.red;

    private void Start()
    {
        // Hook up button listeners
        if (profileIconButton != null)
            profileIconButton.onClick.AddListener(OnProfileIconTapped);

        if (signInButton != null)
            signInButton.onClick.AddListener(OnSignInButtonPressed);

        if (signOutButton != null)
            signOutButton.onClick.AddListener(OnSignOutButtonPressed);

        // Hook up auth events
        if (GoogleAuthManager.Instance != null)
        {
            GoogleAuthManager.Instance.OnLoginSuccess += UpdateUI;
            GoogleAuthManager.Instance.OnLoginFailed += OnLoginFailed;
            GoogleAuthManager.Instance.OnLogoutSuccess += UpdateUI;
        }

        // Initial UI state
        UpdateUI();
    }

    /// <summary>
    /// Called when profile icon is tapped.
    /// Opens/closes login panel.
    /// </summary>
    private void OnProfileIconTapped()
    {
        if (loginPanel != null)
        {
            bool isActive = loginPanel.activeSelf;
            loginPanel.SetActive(!isActive);
        }
    }

    /// <summary>
    /// Called when Sign In button is pressed.
    /// </summary>
    private void OnSignInButtonPressed()
    {
        Debug.Log("[LoginUI] Sign In button pressed");
        if (GoogleAuthManager.Instance != null)
            GoogleAuthManager.Instance.SignInWithGoogle();
    }

    /// <summary>
    /// Called when Sign Out button is pressed.
    /// </summary>
    private void OnSignOutButtonPressed()
    {
        Debug.Log("[LoginUI] Sign Out button pressed");
        if (GoogleAuthManager.Instance != null)
            GoogleAuthManager.Instance.SignOut();

        if (loginPanel != null)
            loginPanel.SetActive(false);
    }

    /// <summary>
    /// Called when login fails.
    /// </summary>
    private void OnLoginFailed()
    {
        if (statusText != null)
            statusText.text = "Login failed. Try again.";
    }

    /// <summary>
    /// Update UI based on auth state.
    /// </summary>
    private void UpdateUI()
    {
        bool isAuthenticated = GoogleAuthManager.Instance.IsAuthenticated();

        // Show/hide profile icon
        if (profileIconButton != null)
            profileIconButton.gameObject.SetActive(isAuthenticated);

        // Update user name
        if (userNameText != null)
        {
            string displayName = GoogleAuthManager.Instance.GetUserDisplayName();
            userNameText.text = isAuthenticated ? displayName : "Guest";
        }

        // Update status text in login panel
        if (statusText != null)
        {
            statusText.text = isAuthenticated 
                ? $"Logged in as {GoogleAuthManager.Instance.GetUserDisplayName()}" 
                : "Not logged in";
        }

        // Update network status color
        UpdateNetworkStatus();
    }

    /// <summary>
    /// Update network status indicator.
    /// </summary>
    private void UpdateNetworkStatus()
    {
        NetworkReachability reachability = Application.internetReachability;
        bool isOnline = reachability != NetworkReachability.NotReachable;

        if (profileIconImage != null)
            profileIconImage.color = isOnline ? onlineColor : offlineColor;
    }

    private void OnDestroy()
    {
        // Unhook events
        if (GoogleAuthManager.Instance != null)
        {
            GoogleAuthManager.Instance.OnLoginSuccess -= UpdateUI;
            GoogleAuthManager.Instance.OnLoginFailed -= OnLoginFailed;
            GoogleAuthManager.Instance.OnLogoutSuccess -= UpdateUI;
        }
    }
}
