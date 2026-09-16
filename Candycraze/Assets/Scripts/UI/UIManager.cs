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
        private Text _boosterHint;
        private RectTransform _goalContent;
        private int _goalCount=-1;
        private readonly System.Collections.Generic.List<Text> _goalLabels=new System.Collections.Generic.List<Text>();
        private readonly Text[] _premiumBoosterCounts = new Text[5];
        private readonly Button[] _premiumBoosters = new Button[5];

        private void BuildPremiumUI()
        {
            var root = CandyTheme.Page("PremiumGameHUD", false);
            var background = GameObject.Find("GameBG");
            if (background != null && background.TryGetComponent<SpriteRenderer>(out var backdrop))
            {
                backdrop.sprite = CandyTheme.GameBackdrop;
                backdrop.color = Color.white;
                background.SendMessage("Fit", SendMessageOptions.DontRequireReceiver);
            }
            var header = CandyTheme.Card(root,"GameHeader",.035f,.872f,.965f,.985f,CandyTheme.Cream);
            _livesText = CandyTheme.Label(header.transform,"LEVEL",.04f,.56f,.32f,.96f,32,CandyTheme.Pink);
            CandyTheme.Button(header.transform,"II",.83f,.18f,.96f,.82f,CandyTheme.Purple,OnPausePressed,36);
            CandyTheme.Label(header.transform,"SCORE",.04f,.31f,.32f,.57f,21);
            _scoreText = CandyTheme.Label(header.transform,"0",.04f,.02f,.32f,.35f,36);
            var moves = CandyTheme.Card(header.transform,"Moves",.4f,.10f,.76f,.91f,CandyTheme.Pink,false);
            CandyTheme.Label(moves.transform,"MOVES",.05f,.63f,.95f,.95f,23,Color.white);
            _movesText = CandyTheme.Label(moves.transform,"0",.05f,.04f,.95f,.66f,62,Color.white);
            var goals = CandyTheme.Card(root,"Goals",.07f,.774f,.93f,.86f,CandyTheme.Cream);
            CandyTheme.Label(goals.transform,"LEVEL TASKS",.04f,.73f,.96f,.98f,22);
            _goalContent=CandyTheme.Rect(goals.transform,"TaskCards",.03f,.04f,.97f,.74f);
            _objectiveIcon = CandyTheme.SpriteImage(goals.transform,null,.06f,.10f,.21f,.65f);
            _objectiveText = CandyTheme.Label(goals.transform,"Match the candies!",.24f,.06f,.94f,.66f,30);
            _comboText = CandyTheme.Label(root,"Sweet!",.08f,.45f,.92f,.58f,85,CandyTheme.Gold);
            var tray = CandyTheme.Card(root,"BoosterTray",.03f,.022f,.97f,.153f,CandyTheme.Cream);
            _boosterHint = CandyTheme.Label(tray.transform,"A LITTLE HELP GOES A LONG WAY",.02f,.75f,.98f,.98f,20);
            string[] names = { "HAMMER", "BLAST", "SHUFFLE", "+5 MOVES", "COLOR" };
            for (int i = 0; i < 5; i++)
            {
                var type = (BoosterType)i;
                var button = CandyTheme.Button(tray.transform,names[i],.025f+i*.194f,.29f,.199f+i*.194f,.72f,
                    CandyTheme.Hex("F5DFEF"),() => {
                        var manager = BoosterManager.Instance;
                        if (manager == null) return;
                        if (manager.ActiveBooster == type) { manager.Cancel(); _boosterHint.text="Booster cancelled"; }
                        else if (manager.TryActivate(type)) _boosterHint.text=manager.ActiveBooster.HasValue?"Tap a candy • tap booster again to cancel":"Sweet boost!";
                        else if (manager.GetCount(type) <= 0) ShowBoosterPurchase(type);
                        else _boosterHint.text="Wait until the board finishes moving.";
                    },20);
                var caption=button.GetComponentInChildren<Text>();
                caption.gameObject.SetActive(false);
                string[] icons={"Booster_Hammer","Booster_Blast","Booster_Shuffle","Booster_Moves","Booster_Color"};
                CandyTheme.SpriteImage(button.transform,Resources.Load<Sprite>("CandySprites/"+icons[i]),.04f,.04f,.96f,.96f);
                var badge=CandyTheme.Card(button.transform,"Stock",.65f,-.1f,1.06f,.26f,CandyTheme.Purple,false);
                _premiumBoosters[i] = button;
                _premiumBoosterCounts[i] = CandyTheme.Label(badge.transform,"0",0,0,1,1,23,Color.white);
                CandyTheme.Label(tray.transform,names[i],.025f+i*.194f,.01f,.199f+i*.194f,.22f,18);
            }
            var win = CandyTheme.Modal(root,"Sweet victory!",out _winPanel);
            _starImages = new Image[3];
            for(int i=0;i<3;i++) _starImages[i]=CandyTheme.SpriteImage(win,Resources.Load<Sprite>("UI/Star"),.16f+i*.23f,.51f,.38f+i*.23f,.75f);
            _winScoreText = CandyTheme.Label(win,"0",.1f,.37f,.9f,.51f,50);
            CandyTheme.Button(win,"NEXT ADVENTURE",.1f,.19f,.9f,.35f,CandyTheme.Pink,OnNextLevelPressed,36);
            CandyTheme.Button(win,"BACK TO MAP",.1f,.035f,.9f,.16f,CandyTheme.Purple,OnQuitToMapPressed,30);
            var lose = CandyTheme.Modal(root,"So close!",out _losePanel);
            CandyTheme.Label(lose,"Every try makes you sweeter.\nGive this puzzle another go.",.08f,.52f,.92f,.74f,32);
            _loseScoreText = CandyTheme.Label(lose,"0",.1f,.36f,.9f,.5f,48);
            CandyTheme.Button(lose,"TRY AGAIN",.1f,.19f,.9f,.34f,CandyTheme.Pink,OnRestartPressed);
            CandyTheme.Button(lose,"BACK TO MAP",.1f,.035f,.9f,.16f,CandyTheme.Purple,OnQuitToMapPressed,30);
            var pause = CandyTheme.Modal(root,"Take a sweet break",out _pausePanel);
            CandyTheme.Button(pause,"KEEP PLAYING",.1f,.56f,.9f,.70f,CandyTheme.Pink,OnResumePressed);
            CandyTheme.Button(pause,"RESTART",.1f,.38f,.9f,.52f,CandyTheme.Purple,OnRestartPressed);
            CandyTheme.Button(pause,"CANDY POWER GUIDE",.1f,.20f,.9f,.34f,CandyTheme.Purple,() => CandyPowerGuide.Show(root),30);
            CandyTheme.Button(pause,"BACK TO MAP",.1f,.03f,.9f,.17f,CandyTheme.Mint,OnQuitToMapPressed);
            HideAllPanels();
        }

        private void ShowBoosterPurchase(BoosterType type)
        {
            string[] names = { "Hammer", "Blast", "Shuffle", "+5 Moves", "Color Blast" };
            int[] costs = { 30, 40, 25, 50, 45 };
            int index = (int)type, cost = costs[index];
            var hud = GameObject.Find("PremiumGameHUD");
            if (hud == null) return;
            var card = CandyTheme.Modal(hud.transform, names[index] + " booster", out var overlay);
            int crystals = SaveManager.Instance != null ? SaveManager.Instance.Data.Coins : 0;
            var message = CandyTheme.Label(card,
                "You have none left.\nBuy 1 for " + cost + " crystals?\nBalance: " + crystals,
                .08f,.48f,.92f,.72f,30);
            CandyTheme.Button(card,"BUY 1 - " + cost,.1f,.25f,.9f,.42f,CandyTheme.Pink,() => {
                if (SaveManager.Instance == null || SaveManager.Instance.Data.Coins < cost)
                { message.text = "Not enough crystals.\nBuy a crystal pack from the home shop."; return; }
                switch (type)
                {
                    case BoosterType.Hammer: ShopManager.Instance?.BuyHammer(); break;
                    case BoosterType.RowBlast: ShopManager.Instance?.BuyRowBlast(); break;
                    case BoosterType.Shuffle: ShopManager.Instance?.BuyShuffle(); break;
                    case BoosterType.ExtraMoves: ShopManager.Instance?.BuyExtraMoves(); break;
                    case BoosterType.ColorBlast: ShopManager.Instance?.BuyColorBlast(); break;
                }
                _boosterHint.text = names[index] + " added!";
                Destroy(overlay);
            },30);
            CandyTheme.Button(card,"NOT NOW",.1f,.07f,.9f,.21f,CandyTheme.Purple,() => Destroy(overlay),26);
        }

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            BuildPremiumUI();
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
            if (_cachedOM != null) _cachedOM.OnObjectivesUpdated.RemoveListener(UpdateObjectives);
            var scoreManager = FindObjectOfType<ScoreManager>();
            if (scoreManager != null) scoreManager.OnScoreChanged.RemoveListener(UpdateScore);
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

        public void ShowMoveBonus(int amount)
        {
            if (_comboText == null) return;
            if (_comboCoroutine != null) StopCoroutine(_comboCoroutine);
            _comboText.color = CandyTheme.Gold;
            _comboCoroutine = StartCoroutine(AnimateComboText("+" + amount + " MOVES!"));
        }

        // ── Button callbacks ─────────────────────────────────

        public void OnPausePressed()
        {
            if (GameManager.Instance == null ||
                (GameManager.Instance.State != GameState.Playing && GameManager.Instance.State != GameState.WaitingForBoard)) return;
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

            if(_goalContent!=null)
            {
                _objectiveText.gameObject.SetActive(false);
                _objectiveIcon.gameObject.SetActive(false);
                if(_goalCount!=objectives.Count)
                {
                    foreach(Transform child in _goalContent) Destroy(child.gameObject);
                    _goalLabels.Clear(); _goalCount=objectives.Count;
                    for(int i=0;i<objectives.Count;i++)
                    {
                        var data=objectives[i].Data;
                        var card=CandyTheme.Rect(_goalContent,"Task"+i,(float)i/objectives.Count,0,(float)(i+1)/objectives.Count,1);
                        Sprite icon=data.Type==ObjectiveType.CollectGemType?CandyArtwork.GetNormal(data.GemTypeID):
                            Resources.Load<Sprite>(data.Type==ObjectiveType.ReachScore?"UI/Star":"CandySprites/Booster_Blast");
                        CandyTheme.SpriteImage(card,icon,.02f,.10f,.31f,.92f);
                        _goalLabels.Add(CandyTheme.Label(card,"",.32f,.03f,.98f,.97f,29));
                    }
                }
                for(int i=0;i<objectives.Count;i++)
                {
                    var obj=objectives[i];
                    int left=Mathf.Max(0,obj.Target-obj.Current);
                    _goalLabels[i].text=obj.IsComplete?"DONE!":obj.Data.Type==ObjectiveType.ReachScore?$"{left:N0} points left":
                        obj.Data.Type==ObjectiveType.ClearObstacles?$"{left} blockers left":$"{left} to collect";
                    _goalLabels[i].color=obj.IsComplete?CandyTheme.Mint:CandyTheme.Ink;
                }
                return;
            }

            // Show the gem icon for the first "collect gem" objective.
            UpdateObjectiveIcon(objectives);

            // Build a stacked task list — one objective per line. EVERY
            // objective is shown (including score) so the player always knows
            // exactly what's left to finish the level. A ✓ marks completed
            // goals; the level only ends when ALL show ✓.
            var lines = new System.Collections.Generic.List<string>();
            foreach (var obj in objectives)
            {
                if (obj?.Data == null) continue;

                int target  = obj.Target;
                int current = Mathf.Min(obj.Current, target);
                string tick = obj.IsComplete ? "✓ " : "";

                if (obj.Data.Type == ObjectiveType.CollectGemType)
                    // Gem image beside it shows which gem; text is the count.
                    lines.Add($"{tick}Collect {current}/{target}");
                else
                    lines.Add($"{tick}{ShortObjectiveLabel(obj)}  {current:N0}/{target:N0}");
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
                rt.anchorMin = new Vector2(0.24f, 0.06f);
                rt.anchorMax = new Vector2(0.94f, 0.66f);
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
                        gemSprite = def.CandySprite;
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
            for (int i = 0; i < _premiumBoosterCounts.Length; i++)
            {
                if (_premiumBoosterCounts[i] == null) continue;
                var manager = BoosterManager.Instance;
                _premiumBoosterCounts[i].text = "x" + (manager != null ? manager.GetCount((BoosterType)i) : 0);
                if (_premiumBoosters[i] != null)
                    _premiumBoosters[i].image.color = manager != null && manager.ActiveBooster == (BoosterType)i
                        ? CandyTheme.Mint : i % 2 == 0 ? CandyTheme.Purple : CandyTheme.Pink;
            }
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
                if (GoogleAuthManager.Instance != null && GoogleAuthManager.Instance.IsAuthenticated())
                    CloudSaveManager.Instance?.UploadCurrentSave();
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
            _livesText.color = CandyTheme.Pink;
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
