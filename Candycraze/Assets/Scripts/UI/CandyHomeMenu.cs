// ============================================================
// CandyHomeMenu.cs
// Builds the CANDYCRAZE home screen entirely at runtime as
// separate, interactive Unity UI elements (NOT a flat image).
//
// Layout recreates the reference art with 100% original,
// code-generated candy graphics (see MenuArt):
//   • Candy-land background: sky gradient, clouds, rainbow, grass
//   • Floating candy / gem decorations
//   • Multi-colour bubble "CANDYCRAZE" logo + lollipop
//   • "MATCH-3 ADVENTURE" ribbon banner
//   • PLAY / SHOTS / DAILY GIFT / SETTINGS buttons
//   • Working Settings, Daily Gift and Shots overlay panels
//
// Animations + click SFX are driven by HomeMenuFX.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    [DisallowMultipleComponent]
    public class CandyHomeMenu : MonoBehaviour
    {
        // Design space (matches CanvasScaler reference resolution).
        const float DW = 1080f, DH = 1920f;

        HomeMenuFX _fx;
        MainMenuController _ctrl;
        Font _font;

        GameObject _settingsPanel, _dailyPanel, _shotsPanel;
        Text _soundLabel, _musicLabel;

        // ── Palette ──────────────────────────────────────────
        static readonly Color SkyTop    = H("#2FB2F0");
        static readonly Color SkyBottom = H("#BDEEFF");
        static readonly Color Grass     = H("#7FC93C");
        static readonly Color GrassDark = H("#5DA82B");
        static readonly Color GrassLite = H("#9EE352");
        static readonly Color PathTan   = H("#E9C98B");

        static readonly Color PlayGreen = H("#7CC81C");
        static readonly Color ShotsBlue = H("#37ADE8");
        static readonly Color DailyPurp = H("#A24BD6");
        static readonly Color SetOrange = H("#F4A121");

        static readonly Color RibbonPink = H("#F45B9E");
        static readonly Color RibbonDark = H("#D33580");

        static readonly Color StripeRed  = H("#E43B3B");
        static readonly Color StripeWhite= Color.white;
        static readonly Color StripeBlueD= H("#2C86CC");

        static readonly Color OutlineDark = H("#6A2A86");
        static readonly Color TextOnBtn   = Color.white;

        // ════════════════════════════════════════════════════
        public void Build(Canvas cv)
        {
            _font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            _fx = gameObject.GetComponent<HomeMenuFX>() ?? gameObject.AddComponent<HomeMenuFX>();
            _ctrl = FindObjectOfType<MainMenuController>();

            // Match the design space so px sizes/positions are exact.
            var scaler = cv.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(DW, DH);
                scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;
            }

            var root = cv.GetComponent<RectTransform>();

            BuildBackground(root);
            BuildLogo(root);
            BuildButtons(root);
            BuildSettingsPanel(root);
            BuildDailyPanel(root);
            BuildShotsPanel(root);

            // Sparkle layer sits on top of the scene (below panels).
            var sparkles = Full("Sparkles", root);
            sparkles.GetComponent<Image>().raycastTarget = false;
            _fx.SetSparkleLayer(sparkles);
            // keep panels above sparkles
            _settingsPanel.transform.SetAsLastSibling();
            _dailyPanel.transform.SetAsLastSibling();
            _shotsPanel.transform.SetAsLastSibling();

            // Inject panels + wire the four main buttons via the controller.
            if (_ctrl != null)
                _ctrl.InjectMenuPanels(_settingsPanel, _dailyPanel, _shotsPanel);
        }

        // ════════════════════════════════════════════════════
        // BACKGROUND
        // ════════════════════════════════════════════════════
        void BuildBackground(RectTransform root)
        {
            // Sky gradient
            var sky = Full("Sky", root);
            var skyImg = sky.GetComponent<Image>();
            skyImg.sprite = MenuArt.VGradient(SkyTop, SkyBottom);
            skyImg.color = Color.white;
            skyImg.type = Image.Type.Simple;

            // Rainbow (top-right, partially off-screen)
            var rainbow = ImgNode("Rainbow", root, 0.86f, 0.80f, 900, 900);
            rainbow.sprite = MenuArt.Rainbow(512);
            rainbow.raycastTarget = false;
            rainbow.color = new Color(1, 1, 1, 0.9f);
            rainbow.transform.localRotation = Quaternion.Euler(0, 0, -18f);

            // Clouds
            MakeCloud(root, 0.20f, 0.90f, 380, 200);
            MakeCloud(root, 0.78f, 0.86f, 300, 160);
            MakeCloud(root, 0.52f, 0.955f, 300, 150);

            // Little sky stars
            MakeStar(root, 0.30f, 0.965f, 34, H("#FFE14D"));
            MakeStar(root, 0.68f, 0.90f, 30, Color.white);
            MakeStar(root, 0.12f, 0.83f, 24, H("#FFE14D"));

            // Grass hill band at the bottom
            var grass = Rect("Grass", root, new Vector2(0f, 0f), new Vector2(1f, 0.27f));
            var grassImg = grass.GetComponent<Image>();
            grassImg.sprite = MenuArt.RoundedRect(1080, 520, 40, Grass, false);
            grassImg.raycastTarget = false;
            // rolling bumps along the top edge
            for (int i = 0; i < 5; i++)
            {
                var bump = ImgNode($"Hill{i}", root, 0.1f + i * 0.2f, 0.265f, 300, 220);
                bump.sprite = MenuArt.Circle(GrassLite, 128);
                bump.raycastTarget = false;
            }
            // sandy path in the middle
            var path = ImgNode("Path", root, 0.5f, 0.12f, 360, 520);
            path.sprite = MenuArt.RoundedRect(360, 520, 120, PathTan, false);
            path.raycastTarget = false;

            // Floating candies / gems / lollipops (registered with FX)
            SpawnCandy(root, 0.10f, 0.44f, 96,  MenuArt.Gem(H("#B15CE0")), 14, 0.8f, 8, 6);
            SpawnCandy(root, 0.90f, 0.40f, 110, MenuArt.Gem(H("#E24D6B")), 16, 0.7f, -10, 8);
            SpawnCandy(root, 0.14f, 0.20f, 90,  MenuArt.Gem(H("#4D9DE0")), 12, 1.0f, 12, 5);
            SpawnCandy(root, 0.88f, 0.18f, 100, MenuArt.Gem(H("#F5A623")), 15, 0.9f, -8, 7);
            SpawnCandy(root, 0.07f, 0.63f, 120, MenuArt.Lollipop(H("#FF5FA2"), Color.white), 18, 0.6f, 20, 6);
            SpawnCandy(root, 0.93f, 0.60f, 150, MenuArt.Lollipop(H("#FF7A3C"), H("#FFE14D")), 20, 0.55f, -18, 8);
            SpawnCandy(root, 0.22f, 0.10f, 80,  MenuArt.Gem(H("#5FD11E")), 10, 1.1f, 16, 4);
            SpawnCandy(root, 0.80f, 0.09f, 84,  MenuArt.Gem(H("#29C2F0")), 11, 1.0f, -14, 5);
        }

        void MakeCloud(RectTransform root, float ax, float ay, float w, float h)
        {
            var c = ImgNode("Cloud", root, ax, ay, w, h);
            c.sprite = MenuArt.Cloud(220, 120);
            c.color = new Color(1, 1, 1, 0.95f);
            c.raycastTarget = false;
            _fx.RegisterFloater(c.rectTransform, 6f, 0.4f, 0f, 10f);
        }

        void MakeStar(RectTransform root, float ax, float ay, float sz, Color col)
        {
            var s = ImgNode("SkyStar", root, ax, ay, sz, sz);
            s.sprite = MenuArt.Star(col, 48);
            s.raycastTarget = false;
            _fx.RegisterFloater(s.rectTransform, 4f, 1.5f, 20f, 3f);
        }

        void SpawnCandy(RectTransform root, float ax, float ay, float sz, Sprite spr,
            float bob, float speed, float spin, float drift)
        {
            var c = ImgNode("Candy", root, ax, ay, sz, sz);
            c.sprite = spr;
            c.raycastTarget = false;
            _fx.RegisterFloater(c.rectTransform, bob, speed, spin, drift);
        }

        // ════════════════════════════════════════════════════
        // LOGO + RIBBON
        // ════════════════════════════════════════════════════
        void BuildLogo(RectTransform root)
        {
            // Bubble multi-colour title (rich-text per letter).
            var logo = TxtNode("Logo", root, 0.5f, 0.815f, 1020, 240,
                LogoRichText(), 190, Color.white, TextAnchor.MiddleCenter, true);
            logo.horizontalOverflow = HorizontalWrapMode.Overflow;
            logo.verticalOverflow   = VerticalWrapMode.Overflow;
            var ol = logo.gameObject.AddComponent<Outline>();
            ol.effectColor = OutlineDark; ol.effectDistance = new Vector2(6, -6);
            var sh = logo.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.35f); sh.effectDistance = new Vector2(4, -8);
            _fx.RegisterLogo(logo.rectTransform);

            // Lollipop resting on top of the title.
            var lolli = ImgNode("LogoLolli", root, 0.5f, 0.925f, 140, 140);
            lolli.sprite = MenuArt.Lollipop(H("#F45B9E"), Color.white);
            lolli.raycastTarget = false;
            lolli.transform.localRotation = Quaternion.Euler(0, 0, 12f);
            _fx.RegisterFloater(lolli.rectTransform, 5f, 1.2f, 0f, 3f);

            // Ribbon banner.
            var ribbon = ImgNode("Ribbon", root, 0.5f, 0.705f, 720, 118);
            ribbon.sprite = MenuArt.RoundedRect(720, 118, 26, RibbonPink);
            ribbon.raycastTarget = false;
            var rOut = ribbon.gameObject.AddComponent<Outline>();
            rOut.effectColor = RibbonDark; rOut.effectDistance = new Vector2(3, -3);
            // ribbon tails
            var tL = ImgNode("TailL", ribbon.rectTransform, 0.02f, 0.5f, 90, 150);
            tL.sprite = MenuArt.RoundedRect(90, 150, 12, RibbonDark, false);
            tL.raycastTarget = false; tL.transform.SetSiblingIndex(0);
            var tR = ImgNode("TailR", ribbon.rectTransform, 0.98f, 0.5f, 90, 150);
            tR.sprite = MenuArt.RoundedRect(90, 150, 12, RibbonDark, false);
            tR.raycastTarget = false; tR.transform.SetSiblingIndex(0);

            var rt = TxtNode("RibbonTxt", ribbon.rectTransform, 0.5f, 0.5f, 680, 90,
                "MATCH-3 ADVENTURE", 58, Color.white, TextAnchor.MiddleCenter, true);
            var rtOl = rt.gameObject.AddComponent<Outline>();
            rtOl.effectColor = RibbonDark; rtOl.effectDistance = new Vector2(2, -2);
            MakeStar(ribbon.rectTransform, 0.10f, 0.5f, 34, H("#FFE14D"));
            MakeStar(ribbon.rectTransform, 0.90f, 0.5f, 34, H("#FFE14D"));
        }

        string LogoRichText()
        {
            string[] letters = { "C", "A", "N", "D", "Y", "C", "R", "A", "Z", "E" };
            string[] colors =
            {
                "#FF4FA0", "#FFD21E", "#FF8A1E", "#5FD11E", "#29C2F0",
                "#9B5FE0", "#F2452F", "#FFAB1E", "#5FD11E", "#29A7F0"
            };
            var sb = new System.Text.StringBuilder();
            for (int i = 0; i < letters.Length; i++)
                sb.Append($"<color={colors[i]}>{letters[i]}</color>");
            return sb.ToString();
        }

        // ════════════════════════════════════════════════════
        // MAIN BUTTONS
        // ════════════════════════════════════════════════════
        void BuildButtons(RectTransform root)
        {
            // PLAY — green pill, candy-striped border, play icon.
            var play = PillButton(root, "PlayBtn", 0.5f, 0.495f, 760, 152, PlayGreen);
            AddCandyBorder(play, 760, 152, 74, 12f, StripeWhite, StripeRed, 20f);
            AddIcon(play, 0.285f, MenuArt.IconPlay(Color.white, 96), 76);
            AddLabel(play, "PLAY", 66, 0.57f);
            WireButton(play, () => _ctrl?.OnPlayPressed());

            // SHOTS — blue pill, camera icon.
            var shots = PillButton(root, "ShotsBtn", 0.5f, 0.355f, 720, 132, ShotsBlue);
            AddCandyBorder(shots, 720, 132, 64, 11f, StripeWhite, StripeBlueD, 20f);
            AddIcon(shots, 0.30f, MenuArt.IconCamera(Color.white, 96), 66);
            AddLabel(shots, "SHOTS", 58, 0.58f);
            WireButton(shots, () => _ctrl?.OnShotsPressed());

            // DAILY GIFT — purple square, gift icon + label.
            var daily = SquareButton(root, "DailyBtn", 0.325f, 0.165f, 300, 264, DailyPurp);
            AddIconCentered(daily, 0.60f, MenuArt.IconGift(128), 130);
            AddLabelBottom(daily, "DAILY GIFT", 34);
            WireButton(daily, () => _ctrl?.OnDailyRewardPressed());

            // SETTINGS — orange square, gear icon + label.
            var settings = SquareButton(root, "SettingsBtn", 0.675f, 0.165f, 300, 264, SetOrange);
            AddIconCentered(settings, 0.60f, MenuArt.IconGear(H("#7FB2E8"), 120), 128);
            AddLabelBottom(settings, "SETTINGS", 34);
            WireButton(settings, () => _ctrl?.OnSettingsPressed());
        }

        Button PillButton(RectTransform root, string name, float ax, float ay, int w, int h, Color fill)
        {
            var node = ImgNode(name, root, ax, ay, w, h);
            node.sprite = MenuArt.RoundedRect(w, h, h / 2 - 6, fill);
            var btn = node.gameObject.AddComponent<Button>();
            btn.targetGraphic = node;
            var cb = btn.colors;
            cb.normalColor = Color.white; cb.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            cb.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f); btn.colors = cb;
            var sh = node.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.28f); sh.effectDistance = new Vector2(0, -7);
            return btn;
        }

        Button SquareButton(RectTransform root, string name, float ax, float ay, int w, int h, Color fill)
        {
            var node = ImgNode(name, root, ax, ay, w, h);
            node.sprite = MenuArt.RoundedRect(w, h, 46, fill);
            var btn = node.gameObject.AddComponent<Button>();
            btn.targetGraphic = node;
            var cb = btn.colors;
            cb.normalColor = Color.white; cb.highlightedColor = new Color(1.05f, 1.05f, 1.05f, 1f);
            cb.pressedColor = new Color(0.9f, 0.9f, 0.9f, 1f); btn.colors = cb;
            var sh = node.gameObject.AddComponent<Shadow>();
            sh.effectColor = new Color(0, 0, 0, 0.28f); sh.effectDistance = new Vector2(0, -7);
            return btn;
        }

        void AddCandyBorder(Button btn, int w, int h, int radius, float thick, Color a, Color b, float stripeW)
        {
            var node = ImgNode("Border", btn.GetComponent<RectTransform>(), 0.5f, 0.5f, w, h);
            node.sprite = MenuArt.CandyBorder(w, h, radius, thick, a, b, stripeW);
            node.raycastTarget = false;
        }

        void AddIcon(Button btn, float ax, Sprite spr, float sz)
        {
            var node = ImgNode("Icon", btn.GetComponent<RectTransform>(), ax, 0.5f, sz, sz);
            node.sprite = spr; node.raycastTarget = false;
        }

        void AddIconCentered(Button btn, float ay, Sprite spr, float sz)
        {
            var node = ImgNode("Icon", btn.GetComponent<RectTransform>(), 0.5f, ay, sz, sz);
            node.sprite = spr; node.raycastTarget = false;
        }

        void AddLabel(Button btn, string txt, int size, float ax)
        {
            var t = TxtNode("Label", btn.GetComponent<RectTransform>(), ax, 0.5f, 460, 100,
                txt, size, TextOnBtn, TextAnchor.MiddleCenter, true);
            var ol = t.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.35f); ol.effectDistance = new Vector2(2, -2);
        }

        void AddLabelBottom(Button btn, string txt, int size)
        {
            var t = TxtNode("Label", btn.GetComponent<RectTransform>(), 0.5f, 0.16f, 290, 70,
                txt, size, TextOnBtn, TextAnchor.MiddleCenter, true);
            var ol = t.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.4f); ol.effectDistance = new Vector2(2, -2);
        }

        void WireButton(Button btn, UnityEngine.Events.UnityAction action)
        {
            btn.onClick.AddListener(action);
            _fx.RegisterButton(btn);
        }

        // ════════════════════════════════════════════════════
        // SETTINGS PANEL
        // ════════════════════════════════════════════════════
        void BuildSettingsPanel(RectTransform root)
        {
            _settingsPanel = Overlay("SettingsPanel", root, out RectTransform card, 760, 820, "SETTINGS");

            _soundLabel = PanelButton(card, 0.66f, "SOUND", () =>
            {
                AudioManager.Instance?.ToggleSound();
                RefreshAudioLabels();
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            });
            _musicLabel = PanelButton(card, 0.50f, "MUSIC", () =>
            {
                AudioManager.Instance?.ToggleMusic();
                RefreshAudioLabels();
            });
            RefreshAudioLabels();

            PanelButton(card, 0.20f, "CLOSE", () =>
            {
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
                _ctrl?.OnCloseAllPanels();
            }, SetOrange);

            _settingsPanel.SetActive(false);
        }

        void RefreshAudioLabels()
        {
            var am = AudioManager.Instance;
            if (_soundLabel != null) _soundLabel.text = "SOUND: " + (am != null && am.SoundOn ? "ON" : "OFF");
            if (_musicLabel != null) _musicLabel.text = "MUSIC: " + (am != null && am.MusicOn ? "ON" : "OFF");
        }

        // ════════════════════════════════════════════════════
        // DAILY GIFT PANEL
        // ════════════════════════════════════════════════════
        void BuildDailyPanel(RectTransform root)
        {
            _dailyPanel = Overlay("DailyPanel", root, out RectTransform card, 820, 900, "DAILY GIFT");

            var gift = ImgNode("BigGift", card, 0.5f, 0.68f, 220, 220);
            gift.sprite = MenuArt.IconGift(160);
            gift.raycastTarget = false;

            var reward = TxtNode("Reward", card, 0.5f, 0.5f, 720, 90,
                "", 44, H("#5A2A86"), TextAnchor.MiddleCenter, true);
            var status = TxtNode("Status", card, 0.5f, 0.38f, 720, 70,
                "", 34, H("#E24D6B"), TextAnchor.MiddleCenter, true);

            var claimLabel = PanelButton(card, 0.24f, "CLAIM", null, PlayGreen);
            var closeLabel = PanelButton(card, 0.09f, "CLOSE", () =>
            {
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
                _ctrl?.OnCloseAllPanels();
            });

            // Attach the runtime controller that refreshes on enable + claims.
            var rd = _dailyPanel.AddComponent<RuntimeDailyPanel>();
            rd.Setup(reward, status, claimLabel);

            _dailyPanel.SetActive(false);
        }

        // ════════════════════════════════════════════════════
        // SHOTS PANEL
        // ════════════════════════════════════════════════════
        void BuildShotsPanel(RectTransform root)
        {
            _shotsPanel = Overlay("ShotsPanel", root, out RectTransform card, 940, 1400, "SHOTS");

            // Scrollable gallery.
            var scrollGO = ImgNode("Scroll", card, 0.5f, 0.56f, 860, 900);
            scrollGO.color = new Color(0, 0, 0, 0.08f);
            scrollGO.sprite = MenuArt.RoundedRect(860, 900, 24, new Color(1, 1, 1, 1), false);
            var sr = scrollGO.gameObject.AddComponent<ScrollRect>();
            sr.horizontal = false; sr.vertical = true; sr.scrollSensitivity = 30f;
            scrollGO.gameObject.AddComponent<Mask>().showMaskGraphic = true;

            var vp = Rect("Viewport", scrollGO.rectTransform, Vector2.zero, Vector2.one);
            vp.GetComponent<Image>().color = new Color(1, 1, 1, 0);
            sr.viewport = vp;

            var content = new GameObject("Content");
            content.transform.SetParent(vp, false);
            var cRt = content.AddComponent<RectTransform>();
            cRt.anchorMin = new Vector2(0, 1); cRt.anchorMax = new Vector2(1, 1);
            cRt.pivot = new Vector2(0.5f, 1); cRt.anchoredPosition = Vector2.zero;
            var grid = content.AddComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(390, 220);
            grid.spacing = new Vector2(20, 20);
            grid.padding = new RectOffset(20, 20, 20, 20);
            grid.childAlignment = TextAnchor.UpperCenter;
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            sr.content = cRt;

            var empty = TxtNode("Empty", card, 0.5f, 0.56f, 700, 120,
                "No shots yet.\nTap CAPTURE to save one!", 38, new Color(0.4f, 0.4f, 0.5f, 1f),
                TextAnchor.MiddleCenter, true);

            var captureLabel = PanelButton(card, 0.10f, "CAPTURE", null, ShotsBlue);
            var closeLabel = PanelButton(card, 0.02f, "CLOSE", () =>
            {
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
                _ctrl?.OnCloseAllPanels();
            });

            var ctrl = _shotsPanel.AddComponent<ShotsPanelController>();
            ctrl.Setup(cRt, empty, captureLabel.GetComponentInParent<Button>(), _font);

            _shotsPanel.SetActive(false);
        }

        // ════════════════════════════════════════════════════
        // PANEL HELPERS
        // ════════════════════════════════════════════════════
        GameObject Overlay(string name, RectTransform root, out RectTransform card, int cardW, int cardH, string title)
        {
            var panel = Full(name, root);
            var dim = panel.GetComponent<Image>();
            dim.color = new Color(0.05f, 0.02f, 0.12f, 0.72f);
            dim.raycastTarget = true; // blocks clicks behind

            var cardImg = ImgNode("Card", panel, 0.5f, 0.5f, cardW, cardH);
            cardImg.sprite = MenuArt.RoundedRect(cardW, cardH, 48, H("#FFF3D6"));
            var cOut = cardImg.gameObject.AddComponent<Outline>();
            cOut.effectColor = RibbonPink; cOut.effectDistance = new Vector2(4, -4);
            card = cardImg.rectTransform;

            var t = TxtNode("Title", card, 0.5f, 0.9f, cardW - 60, 120,
                title, 72, RibbonPink, TextAnchor.MiddleCenter, true);
            var tOl = t.gameObject.AddComponent<Outline>();
            tOl.effectColor = OutlineDark; tOl.effectDistance = new Vector2(3, -3);
            return panel.gameObject;
        }

        // Returns the label Text so callers can update it (e.g. toggles).
        Text PanelButton(RectTransform card, float ay, string label, UnityEngine.Events.UnityAction action, Color? color = null)
        {
            Color fill = color ?? DailyPurp;
            var node = ImgNode("PBtn_" + label, card, 0.5f, ay, 560, 120);
            node.sprite = MenuArt.RoundedRect(560, 120, 56, fill);
            var btn = node.gameObject.AddComponent<Button>();
            btn.targetGraphic = node;
            var lbl = TxtNode("L", node.rectTransform, 0.5f, 0.5f, 520, 100,
                label, 48, Color.white, TextAnchor.MiddleCenter, true);
            var ol = lbl.gameObject.AddComponent<Outline>();
            ol.effectColor = new Color(0, 0, 0, 0.35f); ol.effectDistance = new Vector2(2, -2);
            if (action != null) btn.onClick.AddListener(action);
            _fx.RegisterButton(btn);
            return lbl;
        }

        // ════════════════════════════════════════════════════
        // PRIMITIVE UI HELPERS
        // ════════════════════════════════════════════════════
        RectTransform Rect(string name, Transform parent, Vector2 aMin, Vector2 aMax)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = aMin; rt.anchorMax = aMax;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
            g.AddComponent<Image>().color = Color.clear;
            return rt;
        }

        RectTransform Full(string name, Transform parent)
        {
            var rt = Rect(name, parent, Vector2.zero, Vector2.one);
            return rt;
        }

        Image ImgNode(string name, Transform parent, float ax, float ay, float w, float h)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
            var img = g.AddComponent<Image>();
            img.sprite = MenuArt.Solid();
            img.color = Color.white;
            return img;
        }

        Text TxtNode(string name, Transform parent, float ax, float ay, float w, float h,
            string txt, int size, Color col, TextAnchor align, bool bold)
        {
            var g = new GameObject(name);
            g.transform.SetParent(parent, false);
            var rt = g.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(ax, ay); rt.anchorMax = new Vector2(ax, ay);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.sizeDelta = new Vector2(w, h);
            var t = g.AddComponent<Text>();
            t.text = txt; t.fontSize = size; t.color = col; t.alignment = align;
            t.font = _font; t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Overflow;
            t.raycastTarget = false;
            t.supportRichText = true;
            return t;
        }

        static Color H(string hex)
        {
            ColorUtility.TryParseHtmlString(hex, out Color c);
            return c;
        }
    }

    // ========================================================
    // RuntimeDailyPanel — refreshes the daily reward panel when
    // shown and handles claiming, using DailyRewardManager.
    // ========================================================
    public class RuntimeDailyPanel : MonoBehaviour
    {
        Text _reward, _status, _claimLabel;
        Button _claimButton;

        public void Setup(Text reward, Text status, Text claimLabel)
        {
            _reward = reward; _status = status; _claimLabel = claimLabel;
            _claimButton = claimLabel != null ? claimLabel.GetComponentInParent<Button>() : null;
            if (_claimButton != null) _claimButton.onClick.AddListener(OnClaim);
        }

        void OnEnable() => Refresh();

        void Refresh()
        {
            var mgr = DailyRewardManager.Instance;
            if (mgr == null)
            {
                if (_reward != null) _reward.text = "Rewards unavailable";
                return;
            }
            bool canClaim = mgr.CanClaimToday();
            var todays = mgr.GetTodaysReward();
            if (_reward != null)
                _reward.text = canClaim
                    ? $"Day {mgr.GetCurrentDay()} reward:\n{todays.DisplayText}"
                    : "Come back tomorrow!";
            if (_claimButton != null) _claimButton.interactable = canClaim;
            if (_claimLabel != null) _claimLabel.text = canClaim ? "CLAIM" : "CLAIMED";
            if (_status != null) _status.text = "";
        }

        void OnClaim()
        {
            var mgr = DailyRewardManager.Instance;
            if (mgr == null) return;
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Coin);
            var reward = mgr.GetTodaysReward();
            if (mgr.ClaimReward())
            {
                if (_status != null) _status.text = $"+ {reward.DisplayText}!";
                Refresh();
            }
        }
    }
}
