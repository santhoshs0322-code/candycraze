// ============================================================
// UIManager.cs
// Central UI coordinator for the Game scene.
// Uses standard UnityEngine.UI.Text (no TMP dependency).
// Swap Text → TextMeshProUGUI later once TMP is imported.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class UIManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static UIManager Instance { get; private set; }

        // ── HUD references ───────────────────────────────────
        [Header("HUD")]
        [SerializeField] private Text   _scoreText;
        [SerializeField] private Text   _movesText;
        [SerializeField] private Text   _objectiveText;
        [SerializeField] private Text   _livesText;

        [Header("Combo Text")]
        [SerializeField] private Text   _comboText;
        [SerializeField] private float  _comboTextDuration = 1.2f;

        [Header("Win Screen")]
        [SerializeField] private GameObject _winPanel;
        [SerializeField] private Text       _winScoreText;
        [SerializeField] private Image[]    _starImages;

        [Header("Lose Screen")]
        [SerializeField] private GameObject _losePanel;
        [SerializeField] private Text       _loseScoreText;

        [Header("Pause Screen")]
        [SerializeField] private GameObject _pausePanel;

        // ── Combo labels ─────────────────────────────────────
        private static readonly string[] _comboLabels =
        {
            "", "", "Nice!", "Great!", "Amazing!",
            "Fantastic!", "Incredible!", "UNSTOPPABLE!"
        };

        private Coroutine _comboCoroutine;

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMovesChanged.AddListener(UpdateMoves);
                GameManager.Instance.OnGameWon.AddListener(ShowWinScreen);
                GameManager.Instance.OnGameLost.AddListener(ShowLoseScreen);
            }

            var sm = FindObjectOfType<ScoreManager>();
            if (sm != null) sm.OnScoreChanged.AddListener(UpdateScore);

            var om = FindObjectOfType<ObjectiveManager>();
            if (om != null) om.OnObjectivesUpdated.AddListener(UpdateObjectives);

            HideAllPanels();
            if (_comboText != null) _comboText.gameObject.SetActive(false);
            UpdateLivesDisplay();
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.OnMovesChanged.RemoveListener(UpdateMoves);
                GameManager.Instance.OnGameWon.RemoveListener(ShowWinScreen);
                GameManager.Instance.OnGameLost.RemoveListener(ShowLoseScreen);
            }
        }

        // ── Public API ───────────────────────────────────────

        public void ShowComboText(int cascadeLevel)
        {
            if (_comboText == null) return;
            int idx = Mathf.Clamp(cascadeLevel, 0, _comboLabels.Length - 1);
            string label = _comboLabels[idx];
            if (string.IsNullOrEmpty(label)) return;

            if (_comboCoroutine != null) StopCoroutine(_comboCoroutine);
            _comboCoroutine = StartCoroutine(AnimateComboText(label));
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Combo);
        }

        // ── Button callbacks ─────────────────────────────────

        public void OnPausePressed()
        {
            GameManager.Instance?.PauseGame();
            if (_pausePanel != null) _pausePanel.SetActive(true);
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
        }

        public void OnResumePressed()
        {
            if (_pausePanel != null) _pausePanel.SetActive(false);
            GameManager.Instance?.ResumeGame();
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
        }

        public void OnRestartPressed()
        {
            HideAllPanels();
            GameManager.Instance?.RestartLevel();
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
        }

        public void OnNextLevelPressed()
        {
            var lm = FindObjectOfType<LevelManager>();
            if (lm != null && lm.HasNextLevel())
            {
                lm.LoadNextLevel();
                GameManager.Instance?.StartLevel();
                HideAllPanels();
            }
            else
            {
                SceneController.NavigateTo(Constants.SCENE_LEVEL_MAP);
            }
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
        }

        public void OnQuitToMapPressed()
        {
            Time.timeScale = 1f;
            SceneController.NavigateTo(Constants.SCENE_LEVEL_MAP);
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
        }

        // ── Private helpers ──────────────────────────────────

        private void UpdateScore(int score)
        {
            if (_scoreText != null) _scoreText.text = score.ToString("N0");
        }

        private void UpdateMoves(int moves)
        {
            if (_movesText != null) _movesText.text = moves.ToString();
        }

        private void UpdateObjectives()
        {
            if (_objectiveText == null) return;
            var om = FindObjectOfType<ObjectiveManager>();
            if (om == null) return;

            var sb = new System.Text.StringBuilder();
            foreach (var obj in om.GetAllObjectives())
            {
                int target  = obj.Target;
                int current = Mathf.Min(obj.Current, target);
                sb.AppendLine($"{obj.Data.Description}: {current}/{target}");
            }
            _objectiveText.text = sb.ToString().TrimEnd();
        }

        private void ShowWinScreen()
        {
            if (_winPanel == null) return;
            _winPanel.SetActive(true);

            var sm = FindObjectOfType<ScoreManager>();
            var lm = FindObjectOfType<LevelManager>();

            if (sm != null && _winScoreText != null)
                _winScoreText.text = sm.CurrentScore.ToString("N0");

            if (sm != null && lm != null && _starImages != null)
            {
                int stars = sm.GetStars(lm.CurrentLevel);
                for (int i = 0; i < _starImages.Length; i++)
                    if (_starImages[i] != null)
                        _starImages[i].color = i < stars ? Color.yellow : Color.grey.WithAlpha(0.4f);
            }

            if (sm != null && lm != null && SaveManager.Instance != null)
            {
                int stars = sm.GetStars(lm.CurrentLevel);
                int coins = CalculateCoins(stars);
                SaveManager.Instance.Data.SetLevelComplete(lm.CurrentLevel.LevelNumber, stars, sm.CurrentScore);
                SaveManager.Instance.Data.Coins += coins;
                SaveManager.Instance.Save();
            }

            AudioManager.Instance?.PlaySFX(AudioManager.SFX.LevelWin);
        }

        private void ShowLoseScreen()
        {
            if (_losePanel == null) return;
            _losePanel.SetActive(true);

            var sm = FindObjectOfType<ScoreManager>();
            if (sm != null && _loseScoreText != null)
                _loseScoreText.text = sm.CurrentScore.ToString("N0");

            if (SaveManager.Instance != null)
            {
                var data = SaveManager.Instance.Data;
                if (data.Lives > 0)
                {
                    data.Lives--;
                    data.LastLifeLostTicks = System.DateTime.UtcNow.Ticks;
                    SaveManager.Instance.Save();
                }
            }

            AudioManager.Instance?.PlaySFX(AudioManager.SFX.LevelFail);
        }

        private void UpdateLivesDisplay()
        {
            if (_livesText == null || SaveManager.Instance == null) return;
            int lives = SaveManager.Instance.Data.Lives;
            string hearts = "";
            for (int i = 0; i < Constants.MAX_LIVES; i++)
                hearts += i < lives ? "♥" : "♡";
            _livesText.text = hearts;
            _livesText.color = lives > 1 ? Color.red : new Color(1f,0.3f,0.3f);
        }

        private void HideAllPanels()
        {
            if (_winPanel   != null) _winPanel.SetActive(false);
            if (_losePanel  != null) _losePanel.SetActive(false);
            if (_pausePanel != null) _pausePanel.SetActive(false);
        }

        private int CalculateCoins(int stars)
        {
            var cfg = Resources.Load<GameConfig>("GameConfig");
            int perStar = cfg != null ? cfg.CoinsPerStar    : Constants.COINS_PER_STAR;
            int perWin  = cfg != null ? cfg.CoinsPerLevelWin : Constants.COINS_PER_LEVEL_WIN;
            return perWin + (stars * perStar);
        }

        private IEnumerator AnimateComboText(string label)
        {
            _comboText.text = label;
            _comboText.gameObject.SetActive(true);

            float elapsed = 0f, halfDur = _comboTextDuration * 0.3f;
            while (elapsed < halfDur)
            {
                elapsed += Time.deltaTime;
                _comboText.transform.localScale =
                    Vector3.Lerp(Vector3.zero, Vector3.one * 1.2f, elapsed / halfDur);
                yield return null;
            }

            yield return new WaitForSeconds(_comboTextDuration * 0.5f);

            elapsed = 0f;
            float fadeDur = _comboTextDuration * 0.2f;
            Color c = _comboText.color;
            while (elapsed < fadeDur)
            {
                elapsed += Time.deltaTime;
                _comboText.color = c.WithAlpha(Mathf.Lerp(1f, 0f, elapsed / fadeDur));
                yield return null;
            }

            _comboText.color = c.WithAlpha(1f);
            _comboText.gameObject.SetActive(false);
            _comboCoroutine = null;
        }
    }
}
