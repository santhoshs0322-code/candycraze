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
        [SerializeField] private Image  _objectiveIcon;   // gem to collect
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

            _cachedOM = FindObjectOfType<ObjectiveManager>();
            if (_cachedOM != null) _cachedOM.OnObjectivesUpdated.AddListener(UpdateObjectives);

            HideAllPanels();
            if (_comboText != null) _comboText.gameObject.SetActive(false);
            UpdateLivesDisplay();

            // Show tasks and moves immediately
            Invoke(nameof(UpdateObjectives), 0.2f);
            if (GameManager.Instance != null)
                UpdateMoves(GameManager.Instance.MovesRemaining);
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
                HideAllPanels();
                // Re-cache objective manager for the new level
                _cachedOM = FindObjectOfType<ObjectiveManager>();
                GameManager.Instance?.StartLevel();
                UpdateLivesDisplay();
                Invoke(nameof(UpdateObjectives), 0.2f);
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

        private ObjectiveManager _cachedOM;

        private void UpdateObjectives()
        {
            if (_objectiveText == null) return;
            if (_cachedOM == null) _cachedOM = FindObjectOfType<ObjectiveManager>();
            if (_cachedOM == null) return;

            var objectives = _cachedOM.GetAllObjectives();
            if (objectives == null || objectives.Count == 0)
            {
                // No objectives loaded yet — keep a friendly placeholder
                _objectiveText.text = "Match the gems!";
                if (_objectiveIcon != null) _objectiveIcon.enabled = false;
                return;
            }

            // Show the gem icon for the first "collect gem" objective.
            UpdateObjectiveIcon(objectives);

            // Build a SHORT, stacked task list — one objective per line.
            // The score is already shown in the top SCORE chip, so we only
            // include a score line when there is NO collect objective.
            bool hasCollect = false;
            foreach (var o in objectives)
                if (o?.Data != null && o.Data.Type == ObjectiveType.CollectGemType)
                { hasCollect = true; break; }

            var lines = new System.Collections.Generic.List<string>();
            foreach (var obj in objectives)
            {
                if (obj?.Data == null) continue;

                // Skip the redundant score objective when a collect goal
                // exists (score is visible in the HUD chip already).
                if (obj.Data.Type == ObjectiveType.ReachScore && hasCollect)
                    continue;

                int target  = obj.Target;
                int current = Mathf.Min(obj.Current, target);
                string tick = obj.IsComplete ? "✓ " : "";

                // For collect-gem goals, show ONLY the count — the gem IMAGE
                // beside it already tells the player which gem. e.g. "0/9".
                // For other goals (score/blockers, no icon), keep a word label.
                if (obj.Data.Type == ObjectiveType.CollectGemType)
                    lines.Add($"{tick}{current}/{target}");
                else
                    lines.Add($"{tick}{ShortObjectiveLabel(obj)}  {current}/{target}");
            }

            if (lines.Count == 0) lines.Add("Match the gems!");
            _objectiveText.text = string.Join("\n", lines);
        }

        // Compact word label for objectives WITHOUT an icon (score/blockers).
        private string ShortObjectiveLabel(ObjectiveProgress obj)
        {
            switch (obj.Data.Type)
            {
                case ObjectiveType.ReachScore:
                    return "Reach Score";
                case ObjectiveType.ClearObstacles:
                    return "Clear Blockers";
                default:
                    return "Goal";
            }
        }

        // Cache gem definitions once (loaded from Resources/Gems).
        private GemDefinition[] _gemDefs;

        private void UpdateObjectiveIcon(System.Collections.Generic.List<ObjectiveProgress> objectives)
        {
            if (_objectiveIcon == null) return;

            // Find the first gem-collection objective
            ObjectiveProgress gemObj = null;
            foreach (var o in objectives)
            {
                if (o?.Data != null && o.Data.Type == ObjectiveType.CollectGemType)
                { gemObj = o; break; }
            }

            // Score-only level: hide the gem icon and let the text use the
            // FULL bar width, centered, so it isn't tiny/squished.
            if (gemObj == null)
            {
                _objectiveIcon.enabled = false;
                if (_objectiveText != null)
                {
                    var rt = _objectiveText.rectTransform;
                    rt.anchorMin = new Vector2(0.06f, 0.06f);
                    rt.anchorMax = new Vector2(0.94f, 0.62f);
                    rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                    _objectiveText.alignment = TextAnchor.MiddleCenter;
                }
                return;
            }

            // Collect-gem level: text sits in a narrow box to the RIGHT of
            // the gem icon, left-aligned so gem + count read as one unit.
            if (_objectiveText != null)
            {
                var rt = _objectiveText.rectTransform;
                rt.anchorMin = new Vector2(0.45f, 0.06f);
                rt.anchorMax = new Vector2(0.66f, 0.62f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                _objectiveText.alignment = TextAnchor.MiddleLeft;
            }

            if (_gemDefs == null || _gemDefs.Length == 0)
                _gemDefs = Resources.LoadAll<GemDefinition>("Gems");

            Sprite gemSprite = null;
            Color  gemColor  = Color.white;
            if (_gemDefs != null)
            {
                foreach (var def in _gemDefs)
                {
                    if (def != null && def.GemTypeID == gemObj.Data.GemTypeID)
                    {
                        gemSprite = def.NormalSprite;
                        gemColor  = def.GemColor;
                        break;
                    }
                }
            }

            if (gemSprite == null)
            {
                // No gem sprite found — use a colored circle so there is
                // always a visible gem indicator next to the count.
                gemSprite = Resources.Load<Sprite>("UI/Circle");
                _objectiveIcon.color = gemColor;
            }
            else
            {
                _objectiveIcon.color = Color.white;   // sprite already colored
            }

            _objectiveIcon.sprite  = gemSprite;
            _objectiveIcon.enabled = true;
        }

        // Build a readable task label when a level has no Description text.
        private string DescribeObjective(ObjectiveProgress obj)
        {
            if (obj?.Data == null) return "Complete the goal";
            switch (obj.Data.Type)
            {
                case ObjectiveType.ReachScore:     return "Reach score";
                case ObjectiveType.CollectGemType: return "Collect gems";
                case ObjectiveType.ClearObstacles: return "Clear blockers";
                default:                           return "Complete the goal";
            }
        }

        // Keep HUD fresh every frame — always show current moves & tasks
        private void Update()
        {
            if (GameManager.Instance == null) return;

            // Live moves counter
            if (_movesText != null)
                _movesText.text = GameManager.Instance.MovesRemaining.ToString();

            // Live level number
            if (_livesText != null)
                _livesText.text = $"Lv {LevelManager.SelectedLevelNumber}";

            // Live task progress
            UpdateObjectives();
        }

        private void ShowWinScreen()
        {
            if (_winPanel == null) return;
            _winPanel.SetActive(true);

            var sm = FindObjectOfType<ScoreManager>();
            var lm = FindObjectOfType<LevelManager>();

            if (sm != null && _winScoreText != null)
                _winScoreText.text = sm.CurrentScore.ToString("N0");

            // Calculate stars using moves efficiency + score
            int stars = 1;
            if (sm != null && lm != null && GameManager.Instance != null)
            {
                int limit = lm.CurrentLevel.MoveLimit;
                int used  = limit - GameManager.Instance.MovesRemaining;
                stars = sm.GetStarsWithMoves(lm.CurrentLevel, used, limit);
            }

            if (_starImages != null)
                StartCoroutine(AnimateWinStars(stars));

            if (sm != null && lm != null && SaveManager.Instance != null)
            {
                int coins = CalculateCoins(stars);
                SaveManager.Instance.Data.SetLevelComplete(lm.CurrentLevel.LevelNumber, stars, sm.CurrentScore);
                SaveManager.Instance.Data.Coins += coins;
                SaveManager.Instance.Save();
            }

            AudioManager.Instance?.PlaySFX(AudioManager.SFX.LevelWin);
        }

        // Star reveal: earned stars pop in one-by-one with a bounce
        private IEnumerator AnimateWinStars(int stars)
        {
            Color gold = new Color(1f, 0.85f, 0.2f);
            Color dim  = new Color(0.35f, 0.35f, 0.45f, 0.6f);

            // Reset: empty stars dim & normal, earned stars hidden (scale 0)
            for (int i = 0; i < _starImages.Length; i++)
            {
                if (_starImages[i] == null) continue;
                if (i < stars)
                {
                    _starImages[i].color = gold;
                    _starImages[i].transform.localScale = Vector3.zero;
                }
                else
                {
                    _starImages[i].color = dim;
                    _starImages[i].transform.localScale = Vector3.one;
                }
            }

            // Pop each earned star with a slight delay
            for (int i = 0; i < _starImages.Length && i < stars; i++)
            {
                if (_starImages[i] == null) continue;
                yield return new WaitForSecondsRealtime(0.28f);
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.Combo);
                yield return StartCoroutine(PopStar(_starImages[i].transform));
            }
        }

        private IEnumerator PopStar(Transform t)
        {
            float elapsed = 0f, dur = 0.45f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float p = Mathf.Clamp01(elapsed / dur);
                // ease-out back (overshoot then settle)
                float s = EaseOutBack(p);
                t.localScale = Vector3.one * s;
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        private static float EaseOutBack(float x)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            return 1f + c3 * Mathf.Pow(x - 1f, 3) + c1 * Mathf.Pow(x - 1f, 2);
        }

        private void ShowLoseScreen()
        {
            if (_losePanel == null) return;
            _losePanel.SetActive(true);

            var sm = FindObjectOfType<ScoreManager>();
            if (sm != null && _loseScoreText != null)
                _loseScoreText.text = sm.CurrentScore.ToString("N0");

            // Lives system removed — no deduction

            AudioManager.Instance?.PlaySFX(AudioManager.SFX.LevelFail);
        }

        private void UpdateLivesDisplay()
        {
            if (_livesText == null) return;
            // Read the currently-selected level (static, always current)
            int levelNum = LevelManager.SelectedLevelNumber;
            _livesText.text = $"Lv {levelNum}";
            _livesText.color = new Color(1f, 0.85f, 0.2f);
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
