using UnityEngine;

/// <summary>
/// GoogleAuthConfig stores Google authentication configuration.
/// Update these values with your Google Cloud credentials.
/// </summary>
public class GoogleAuthConfig : ScriptableObject
{
    [Header("Google Cloud Credentials")]
    // Keep these in sync with ProjectSettings/GooglePlayGameSettings.txt.
    // The previous values belonged to a different Google project/package, which
    // made a Play-installed build fail authentication after Google Play re-signed it.
    [SerializeField] public string googleClientId = "144163133862-78cv89qoiv4stk446c606586ro04o49q.apps.googleusercontent.com";
    [SerializeField] public string packageName = "com.gamixtv.Candycraze";

    [Header("Backend Configuration")]
    [SerializeField] public string backendUrl = "https://candycraze.onrender.com";

    [Header("Features")]
    [SerializeField] public bool enableCloudSave = true;
    [SerializeField] public bool enableAutoSync = true;
    [SerializeField] public bool debugMode = true;

    private static GoogleAuthConfig instance;

    public static GoogleAuthConfig Instance
    {
        get
        {
            if (instance == null)
            {
                instance = Resources.Load<GoogleAuthConfig>("Config/GoogleAuthConfig");
                if (instance == null)
                {
                    Debug.LogError("[GoogleAuthConfig] Config not found in Resources/Config/GoogleAuthConfig.asset");
                }
            }
            return instance;
        }
    }
}
