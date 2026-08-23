// ============================================================
// LoseScreenController.cs
// Lose screen — shows score, objectives missed, retry/quit.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class LoseScreenController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text       _scoreText;
        [SerializeField] private Text       _objectivesText;
        [SerializeField] private Text       _livesText;

        [Header("Buttons")]
        [SerializeField] private Button _retryButton;
        [SerializeField] private Button _quitButton;
        [SerializeField] private Button _watchAdButton;  // Extra moves via rewarded ad

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);

            if (_retryButton   != null) _retryButton.onClick.AddListener(OnRetry);
            if (_quitButton    != null) _quitButton.onClick.AddListener(OnQuit);
            if (_watchAdButton != null) _watchAdButton.onClick.AddListener(OnWatchAd);
        }

        // ── Public API ───────────────────────────────────────

        public void Show(int score)
        {
            if (_panel == null) return;
            _panel.SetActive(true);
            StartCoroutine(AnimateLose(score));
        }

        // ── Animation ────────────────────────────────────────

        private IEnumerator AnimateLose(int score)
        {
            _panel.transform.localScale = Vector3.zero;

            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                elapsed += Time.deltaTime;
                _panel.transform.localScale = Vector3.Lerp(Vector3.zero, Vector3.one, elapsed / 0.3f);
                yield return null;
            }
            _panel.transform.localScale = Vector3.one;

            if (_scoreText != null) _scoreText.text = score.ToString("N0");

            // Show objectives progress
            if (_objectivesText != null)
            {
                var om = FindObjectOfType<ObjectiveManager>();
                if (om != null)
                {
                    var sb = new System.Text.StringBuilder();
                    foreach (var obj in om.GetAllObjectives())
                    {
                        int cur = Mathf.Min(obj.Current, obj.Target);
                        string status = obj.IsComplete ? "✓" : "✗";
                        sb.AppendLine($"{status} {obj.Data.Description}: {cur}/{obj.Target}");
                    }
                    _objectivesText.text = sb.ToString().TrimEnd();
                }
            }

            // Lives remaining
            if (_livesText != null && SaveManager.Instance != null)
            {
                int lives = SaveManager.Instance.Data.Lives;
                _livesText.text = lives > 0
                    ? $"♥ {lives} lives remaining"
                    : "No lives left!";
            }

            // Show watch-ad button only if ad is available
            if (_watchAdButton != null)
                _watchAdButton.gameObject.SetActive(
                    AdManager.Instance != null && AdManager.Instance.IsRewardedAdReady());

            AudioManager.Instance?.PlaySFX(AudioManager.SFX.LevelFail);
        }

        // ── Button Callbacks ─────────────────────────────────

        private void OnRetry()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);

            // Check lives
            if (SaveManager.Instance != null && SaveManager.Instance.Data.Lives <= 0)
            {
                Debug.Log("[LoseScreen] No lives — cannot retry.");
                // TODO Phase 6: Show lives shop
                return;
            }

            // Deduct a life
            if (SaveManager.Instance != null)
            {
                SaveManager.Instance.Data.Lives--;
                SaveManager.Instance.Save();
            }

            if (_panel != null) _panel.SetActive(false);
            GameManager.Instance?.RestartLevel();
        }

        private void OnQuit()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            Time.timeScale = 1f;
            SceneController.Instance?.GoToLevelMap();
        }

        private void OnWatchAd()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            AdManager.Instance?.ShowRewardedAd(
                onRewarded: () =>
                {
                    // Give 5 extra moves
                    Debug.Log("[LoseScreen] Rewarded — granting 5 extra moves.");
                    if (_panel != null) _panel.SetActive(false);
                    // TODO: add extra moves to GameManager
                },
                onFailed: () =>
                {
                    Debug.Log("[LoseScreen] Ad not available.");
                });
        }
    }
}
