using UnityEngine;

/// <summary>
/// GoogleAuthConfig stores Google authentication configuration.
/// Update these values with your Google Cloud credentials.
/// </summary>
public class GoogleAuthConfig : ScriptableObject
{
    [Header("Google Cloud Credentials")]
    [SerializeField] public string googleClientId = "787050963672-n0c7i29los0rshojcve4ckal097bhagd.apps.googleusercontent.com";
    [SerializeField] public string packageName = "com.CandyCraze.Game";

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
