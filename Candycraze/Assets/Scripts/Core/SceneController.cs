// ============================================================
// SceneController.cs
// Handles all scene navigation. Works both as a singleton
// (DontDestroyOnLoad) and via static fallback methods so
// navigation always works even if the singleton wasn't
// initialised from Bootstrap.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CandyCraze
{
    public class SceneController : MonoBehaviour
    {
        public static SceneController Instance { get; private set; }

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

        // ── Public navigation ────────────────────────────────

        public void GoToMainMenu() => Load(Constants.SCENE_MAIN_MENU);
        public void GoToLevelMap() => Load(Constants.SCENE_LEVEL_MAP);
        public void GoToGame()     => Load(Constants.SCENE_GAME);

        public void Load(string sceneName)
        {
            // Always works — uses coroutine if we have a MonoBehaviour,
            // otherwise loads directly
            if (this != null && gameObject.activeInHierarchy)
                StartCoroutine(LoadRoutine(sceneName));
            else
                SceneManager.LoadScene(sceneName);
        }

        // ── Static fallback — usable without an instance ─────

        public static void NavigateTo(string sceneName)
        {
            Debug.Log($"[SceneController] NavigateTo: {sceneName}");
            // Direct load — most reliable on mobile
            Time.timeScale = 1f;
            try
            {
                SceneManager.LoadScene(sceneName);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"[SceneController] Failed to load {sceneName}: {e.Message}");
            }
        }

        // ── Coroutine ────────────────────────────────────────
        private IEnumerator LoadRoutine(string sceneName)
        {
            // Verify scene exists
            bool found = false;
            for (int i = 0; i < SceneManager.sceneCountInBuildSettings; i++)
            {
                string path = SceneUtility.GetScenePathByBuildIndex(i);
                if (path.ToLower().Contains(sceneName.ToLower()))
                {
                    found = true;
                    break;
                }
            }

            if (!found)
            {
                Debug.LogError($"[SceneController] Scene '{sceneName}' not in Build Settings!\n" +
                               "Run: CandyCraze → Build All Scenes (Auto-Wire)\n" +
                               "Then: File → Build Settings → verify all 4 scenes are listed.");
                yield break;
            }

            Debug.Log($"[SceneController] Loading: {sceneName}");
            AsyncOperation op = SceneManager.LoadSceneAsync(sceneName);
            while (!op.isDone)
                yield return null;
        }
    }
}
