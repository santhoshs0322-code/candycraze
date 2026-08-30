// ============================================================
// LevelMapController.cs
// Builds its own Canvas + manually-positioned level buttons
// in a scrollable content area. No layout groups (they were
// the source of invisible buttons). Pure manual positioning.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class LevelMapController : MonoBehaviour
    {
        [SerializeField] private Button    _backButton;
        [SerializeField] private Transform _nodeContainer;
        [SerializeField] private Text      _totalStarsText;

        private GameConfig _config;
        private RectTransform _content;
        private ScrollRect    _scrollRect;

        public void InjectRefs(Button b, Transform c, Text s) { }

        void Start()
        {
            _config = Resources.Load<GameConfig>("GameConfig");
            int total = (_config != null && _config.TotalLevels > 0) ? _config.TotalLevels : 100;
            Debug.Log($"[LevelMap] Starting. Levels={total}");

            BuildUI(total);
        }

        void BuildUI(int totalLevels)
        {
            // ── Canvas ───────────────────────────────────────
            var canvasGO = new GameObject("LevelMapCanvas");
            var canvas = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGO.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Shared portrait baseline, match WIDTH for consistent phone layout
            scaler.referenceResolution = new Vector2(1080, 1920);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0f;
            canvasGO.AddComponent<GraphicRaycaster>();

            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var es = new GameObject("EventSystem");
                es.AddComponent<UnityEngine.EventSystems.EventSystem>();
                es.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            var root = canvasGO.GetComponent<RectTransform>();

            // ── Background — candy map image ─────────────────
            var bgGO = MakeImage("BG", root, Vector2.zero, Vector2.one,
                Vector2.zero, Vector2.zero, new Color(0.08f, 0.05f, 0.20f));
            var mapBg = Resources.Load<Sprite>("UI/BG_Map");
            if (mapBg != null)
            {
                var bgImg = bgGO.GetComponent<Image>();
                bgImg.sprite = mapBg;
                bgImg.color = Color.white;
            }

            // ── Header bar ───────────────────────────────────
            var header = MakeImage("Header", root,
                new Vector2(0, 0.92f), Vector2.one, Vector2.zero, Vector2.zero,
                new Color(0.15f, 0.08f, 0.30f));

            // Title — gold with shadow (same style as home page)
            UIStyle.Title(header.transform, "SELECT LEVEL", new Vector2(0.5f, 0.5f), 44);

            // Back button — same rounded style as everywhere
            _backButton = UIStyle.Button(header.transform, "BackBtn", "‹ BACK",
                new Vector2(0.02f, 0.2f), new Vector2(0.24f, 0.8f), UIStyle.Blue, 28);
            _backButton.onClick.AddListener(() => SceneController.NavigateTo(Constants.SCENE_MAIN_MENU));

            // ── ScrollRect setup ─────────────────────────────
            var scrollGO = new GameObject("Scroll");
            scrollGO.transform.SetParent(root, false);
            var scrollRt = scrollGO.AddComponent<RectTransform>();
            scrollRt.anchorMin = new Vector2(0.05f, 0.02f);
            scrollRt.anchorMax = new Vector2(0.95f, 0.90f);
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = Vector2.zero;
            // ScrollRect goes on the outer object. It needs a raycastable
            // graphic so touch drags register on mobile.
            var scrollImg = scrollGO.AddComponent<Image>();
            scrollImg.color = new Color(0,0,0,0.01f); // near-invisible but raycastable
            _scrollRect = scrollGO.AddComponent<ScrollRect>();
            _scrollRect.horizontal = false;
            _scrollRect.vertical = true;
            _scrollRect.movementType = ScrollRect.MovementType.Elastic;
            _scrollRect.elasticity = 0.1f;
            _scrollRect.inertia = true;
            _scrollRect.decelerationRate = 0.135f;
            _scrollRect.scrollSensitivity = 45f;

            // ── Viewport — MUST be a child of the ScrollRect (holds the
            //    Mask). Using the ScrollRect's own transform as the viewport
            //    breaks touch dragging on mobile. ──
            var viewportGO = new GameObject("Viewport");
            viewportGO.transform.SetParent(scrollGO.transform, false);
            var viewportRt = viewportGO.AddComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = Vector2.zero;
            viewportRt.pivot = new Vector2(0.5f, 0.5f);
            var viewportImg = viewportGO.AddComponent<Image>();
            viewportImg.color = new Color(0,0,0,0.01f);
            viewportGO.AddComponent<Mask>().showMaskGraphic = false;

            // ── Content — height calculated manually ─────────
            int cols = 2;
            int rows = Mathf.CeilToInt(totalLevels / (float)cols);
            float cellH = 200f;   // reference-resolution units
            float cellGap = 25f;
            float contentHeight = rows * (cellH + cellGap) + cellGap;

            var contentGO = new GameObject("Content");
            contentGO.transform.SetParent(viewportGO.transform, false);
            _content = contentGO.AddComponent<RectTransform>();
            _content.anchorMin = new Vector2(0, 1);
            _content.anchorMax = new Vector2(1, 1);
            _content.pivot = new Vector2(0.5f, 1);
            _content.anchoredPosition = Vector2.zero;
            _content.sizeDelta = new Vector2(0, contentHeight);
            _scrollRect.content = _content;
            _scrollRect.viewport = viewportRt;

            // ── Create level buttons (manual grid position) ──
            float refW = 1080f * 0.9f;  // scroll width
            float cellW = (refW - cellGap * (cols + 1)) / cols;

            for (int i = 1; i <= totalLevels; i++)
            {
                int idx = i - 1;
                int col = idx % cols;
                int row = idx / cols;

                float x = cellGap + col * (cellW + cellGap) + cellW * 0.5f - refW * 0.5f;
                float y = -(cellGap + row * (cellH + cellGap) + cellH * 0.5f);

                CreateButton(i, cellW, cellH, x, y);
            }

            UpdateStars();
            Debug.Log($"[LevelMap] Built {totalLevels} buttons. Content height={contentHeight}");
        }

        void CreateButton(int level, float w, float h, float x, float y)
        {
            bool unlocked = level <= 3;
            int stars = 0;
            if (SaveManager.Instance != null)
            {
                unlocked = SaveManager.Instance.Data.IsLevelUnlocked(level);
                stars = SaveManager.Instance.Data.GetStars(level);
            }

            var go = new GameObject($"Lvl_{level}");
            go.transform.SetParent(_content, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 1f);
            rt.anchorMax = new Vector2(0.5f, 1f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);

            var img = go.AddComponent<Image>();
            // Rounded card sprite (same style as buttons)
            var cardSp = Resources.Load<Sprite>(unlocked ? "UI/BtnBlue" : "UI/BtnDark");
            if (cardSp != null)
            {
                img.sprite = cardSp; img.type = Image.Type.Sliced;
                img.color = unlocked ? Color.white : new Color(0.7f,0.7f,0.7f);
            }
            else
                img.color = unlocked ? new Color(0.30f,0.50f,0.95f) : new Color(0.25f,0.25f,0.35f);

            // Level number
            MakeText("Num", go.transform,
                new Vector2(0, 0.4f), new Vector2(1, 1), Vector2.zero, Vector2.zero,
                level.ToString(), 60,
                unlocked ? Color.white : new Color(1,1,1,0.4f), FontStyle.Bold);

            // Stars or lock
            string sub = "";
            Color subCol;
            if (unlocked)
            {
                for (int s = 0; s < 3; s++) sub += s < stars ? "★" : "☆";
                subCol = new Color(1f, 0.85f, 0.2f);
            }
            else
            {
                sub = "LOCKED";
                subCol = new Color(1,1,1,0.5f);
            }
            MakeText("Sub", go.transform,
                new Vector2(0, 0), new Vector2(1, 0.4f), Vector2.zero, Vector2.zero,
                sub, 32, subCol, FontStyle.Normal);

            var btn = go.AddComponent<Button>();
            btn.interactable = unlocked;
            btn.targetGraphic = img;
            int cap = level;
            btn.onClick.AddListener(() => OnLevel(cap));
        }

        void OnLevel(int n)
        {
            bool unlocked = n <= 3 || (SaveManager.Instance != null && SaveManager.Instance.Data.IsLevelUnlocked(n));
            if (!unlocked) return;
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            LevelManager.SelectedLevelNumber = n;
            SceneController.NavigateTo(Constants.SCENE_GAME);
        }

        void UpdateStars()
        {
            if (_totalStarsText == null) return;
            int s = SaveManager.Instance != null ? SaveManager.Instance.Data.TotalStars : 0;
            _totalStarsText.text = $"⭐ {s}";
        }

        // ── Helpers ──────────────────────────────────────────
        GameObject MakeImage(string n, Transform p, Vector2 amin, Vector2 amax,
            Vector2 omin, Vector2 omax, Color c)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.offsetMin = omin; rt.offsetMax = omax;
            go.AddComponent<Image>().color = c;
            return go;
        }

        Text MakeText(string n, Transform p, Vector2 amin, Vector2 amax,
            Vector2 omin, Vector2 omax, string txt, int size, Color c, FontStyle fs)
        {
            var go = new GameObject(n); go.transform.SetParent(p, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = amin; rt.anchorMax = amax;
            rt.offsetMin = omin; rt.offsetMax = omax;
            var t = go.AddComponent<Text>();
            t.text = txt; t.fontSize = size; t.color = c;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontStyle = fs;
            t.raycastTarget = false;
            return t;
        }
    }
}
