// ============================================================
// Bootstrap.cs — First scene. Initialises all systems.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace CandyCraze
{
    public class Bootstrap : MonoBehaviour
    {
        [SerializeField] private float _minimumSplashDuration = 2.0f;
        [SerializeField] private bool  _goToMainMenu = true;
        [SerializeField] private Text  _loadingText;

        private IEnumerator Start()
        {
            // Performance settings
            Application.targetFrameRate = 60;
            QualitySettings.vSyncCount  = 0;
            Screen.sleepTimeout         = SleepTimeout.NeverSleep;

            if (_loadingText != null) _loadingText.text = "Loading...";

            // Load save data
            if (SaveManager.Instance != null)
                SaveManager.Instance.Load();
            else
            {
                // Create SaveManager if not in scene
                var smGO = new GameObject("SaveManager");
                smGO.AddComponent<SaveManager>();
                SaveManager.Instance.Load();
            }

            // Initialise DailyRewardManager
            if (DailyRewardManager.Instance == null)
            {
                var go = new GameObject("DailyRewardManager");
                go.AddComponent<DailyRewardManager>();
                DontDestroyOnLoad(go);
            }

            if (_loadingText != null) _loadingText.text = "CandyCraze";

            yield return new WaitForSeconds(_minimumSplashDuration);

            // Decide which scene to load
            string target = _goToMainMenu
                ? Constants.SCENE_MAIN_MENU
                : Constants.SCENE_GAME;

            // Check scene exists in build settings
            bool found = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string sp = SceneUtility.GetScenePathByBuildIndex(i);
                if (sp.ToLower().Contains(target.ToLower()))
                {
                    found = true;
                    break;
                }
            }

            if (found)
            {
                Debug.Log($"[Bootstrap] Loading: {target}");
                SceneManager.LoadScene(target);
            }
            else
            {
                // Fallback to Game scene if MainMenu not built yet
                Debug.LogWarning($"[Bootstrap] '{target}' not in build settings. " +
                                 "Run: CandyCraze → Build All Scenes (Auto-Wire)");

                if (_loadingText != null)
                    _loadingText.text = "Run:\nCandyCraze → Build All Scenes";

                // Try loading Game directly
                SceneManager.LoadScene(Constants.SCENE_GAME);
            }
        }
    }
}
