// ============================================================
// RuntimeUIBuilder.cs
// Builds ALL UI at RUNTIME — no editor scene setup needed.
// Attach to an empty GameObject in MainMenu or LevelMap scene.
// Set SceneTarget to match the scene.
// ============================================================

using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public enum UISceneTarget { MainMenu, LevelMap }

    [DisallowMultipleComponent]
    public class RuntimeUIBuilder : MonoBehaviour
    {
        [Tooltip("Which scene's UI to build.")]
        public UISceneTarget SceneTarget = UISceneTarget.MainMenu;

        // Palette
        static readonly Color C_BG     = Hex("#1A0A2E");
        static readonly Color C_PANEL  = Hex("#2D1B5E");
        static readonly Color C_DARK   = Hex("#1F0F45");
        static readonly Color C_GOLD   = Hex("#FFD700");
        static readonly Color C_GREEN  = Hex("#2ECC71");
        static readonly Color C_GDARK  = Hex("#27AE60");
        static readonly Color C_RED    = Hex("#E74C3C");
        static readonly Color C_CYAN   = Hex("#33E6FF");
        static readonly Color C_PUR    = Hex("#8E44E8");
        static readonly Color C_WHITE  = Color.white;
        static readonly Color C_W70    = new Color(1,1,1,0.7f);
        static readonly Color C_W40    = new Color(1,1,1,0.4f);
        static readonly Color C_LOCK   = Hex("#555577");
        static readonly Color C_LOCKD  = Hex("#3A3355");
        static readonly Color C_TRANS  = new Color(0,0,0,0);

        [HideInInspector] public Button    PlayButton;
        [HideInInspector] public Button    ShopButton;
        [HideInInspector] public Button    DailyButton;
        [HideInInspector] public Button    SettingsButton;
        [HideInInspector] public Text      CoinsText;
        [HideInInspector] public Text      LivesText;
        [HideInInspector] public Button    BackButton;
        [HideInInspector] public Transform LevelContainer;
        [HideInInspector] public ScrollRect LevelScroll;
        [HideInInspector] public Text      TotalStarsText;

        float W, H, SafeTop, SafeBottom;

        void Awake()
        {
            W = Screen.width; H = Screen.height;
            Rect safe = Screen.safeArea;
            SafeTop    = H - safe.yMax;
            SafeBottom = safe.yMin;
            switch (SceneTarget)
            {
                case UISceneTarget.MainMenu: BuildMainMenu(GetOrMakeCanvas()); break;
                case UISceneTarget.LevelMap: BuildLevelMap(GetOrMakeCanvas());  break;
            }
        }

        void Start()
        {
            // Inject refs after all Awakes have run
            if (SceneTarget == UISceneTarget.LevelMap)
            {
                var mc = GetComponent<LevelMapController>() ?? FindObjectOfType<LevelMapController>();
                if (mc != null)
                {
                    mc.InjectRefs(BackButton, LevelContainer, TotalStarsText);
                    Debug.Log("[RuntimeUIBuilder] Injected refs to LevelMapController.");
                }
            }
        }

        // ════════════════════════════════════════════════════
        // MAIN MENU
        // ════════════════════════════════════════════════════
        void BuildMainMenu(Canvas cv)
        {
            var root = cv.GetComponent<RectTransform>();

            // ── BG — full-screen candy kingdom ───────────────
            var bg = Pnl("BG", root, V2(0,0), V2(1,1), V2(0,0), V2(0,0));
            var bgImg = bg.GetComponent<Image>();
            var menuBg = Resources.Load<Sprite>("UI/BG_Menu");
            if (menuBg != null) { bgImg.sprite = menuBg; bgImg.color = Color.white; }
            else Clr(bg, C_BG);

            // ── HUD — coins OVAL chip (top-left) ─────────────
            // (Lives pill removed — the game no longer uses a lives system,
            //  so the top-right red circle was decorative/confusing.)
            var coinsPill = OvalPanel("CoinsPill", root, V2(0.04f,0.90f), V2(0.42f,0.965f), C_GOLD);
            CoinsText = Txt("Coins", coinsPill.RT(), V2(0,0),V2(1,1),V2(0,0),V2(0,0),
                "✦ 0", H*0.025f, Color.white, TextAnchor.MiddleCenter, bold:true);
            LivesText = null;

            // ── Multi-colour candy title ─────────────────────
            BuildCandyTitle(root);

            // Tagline ribbon banner — rounded candy pill
            var banner = Pnl("TagBanner", root, V2(0.18f,0.675f), V2(0.82f,0.74f), Vector2.zero, Vector2.zero);
            var bnImg = banner.GetComponent<Image>();
            var bnSprite = Resources.Load<Sprite>("UI/LogoBanner")
                        ?? Resources.Load<Sprite>("UI/BtnPurple")
                        ?? Resources.Load<Sprite>("UI/Panel");
            if (bnSprite != null)
            {
                bnImg.sprite = bnSprite;
                bnImg.type = Image.Type.Sliced;
                bnImg.color = Hex("#E84393");
            }
            else Clr(banner, Hex("#E84393"));
            Outline(banner, C_WHITE.WithAlpha(0.7f), 2f);
            Txt("Tag", banner.RT(), V2(0,0),V2(1,1),V2(0,0),V2(0,0),
                "MATCH-3 ADVENTURE", W*0.045f, C_WHITE, TextAnchor.MiddleCenter, bold:true);

            // Gem deco row under title
            GemRow(root, new Vector2(0, H*0.10f));

            // ── PLAY — big pill button, centered ─────────────
            PlayButton = Btn("PlayBtn", root,
                V2(0.12f,0.40f), V2(0.88f,0.50f), Vector2.zero, Vector2.zero,
                "▶  PLAY", W*0.085f, C_GREEN, C_GDARK, C_WHITE);

            // ── SHOP + DAILY — side by side pills ────────────
            ShopButton = Btn("ShopBtn", root,
                V2(0.12f,0.29f), V2(0.49f,0.375f), Vector2.zero, Vector2.zero,
                "🏪 SHOP", W*0.05f, C_GOLD, C_GOLD*0.7f, C_WHITE);
            DailyButton = Btn("DayBtn", root,
                V2(0.51f,0.29f), V2(0.88f,0.375f), Vector2.zero, Vector2.zero,
                "🎁 DAILY", W*0.05f, C_PUR, C_PUR*0.7f, C_WHITE);

            // ── SETTINGS — pill button ───────────────────────
            SettingsButton = Btn("SetBtn", root,
                V2(0.12f,0.18f), V2(0.88f,0.265f), Vector2.zero, Vector2.zero,
                "⚙ SETTINGS", W*0.05f, C_DARK, C_BG, C_WHITE);

            // ── SETTINGS PANEL ───────────────────────────────
            var setPanel = Pnl("SettingsPanel", root, V2(0,0), V2(1,1), V2(0,0), V2(0,0));
            Clr(setPanel, new Color(0.05f,0.03f,0.15f,0.97f));
            Txt("SetTitle", setPanel.transform, V2(0.5f,0.5f),V2(0.5f,0.5f),
                V2(0,H*0.32f), V2(W*0.8f,80), "SETTINGS", 52, C_GOLD,
                TextAnchor.MiddleCenter, bold:true);
            var soundBtn = Btn("SoundBtn", setPanel.transform, V2(0.5f,0.5f),V2(0.5f,0.5f),
                V2(0,H*0.13f), V2(W*0.82f,140), "Sound: ON", 46, C_GREEN, C_GDARK, C_WHITE);
            var musicBtn = Btn("MusicBtn", setPanel.transform, V2(0.5f,0.5f),V2(0.5f,0.5f),
                V2(0,-H*0.01f), V2(W*0.82f,140), "Music: ON", 46, C_GREEN, C_GDARK, C_WHITE);
            Txt("Ver", setPanel.transform, V2(0.5f,0.5f),V2(0.5f,0.5f),
                V2(0,-H*0.14f), V2(W*0.7f,50), "Version 1.0.0", 26, C_W70);
            var closeSet = Btn("CloseSet", setPanel.transform, V2(0.5f,0.5f),V2(0.5f,0.5f),
                V2(0,-H*0.26f), V2(W*0.6f,120), "CLOSE", 42, C_RED, C_RED*0.7f, C_WHITE);
            setPanel.SetActive(false);

            // ── SHOP PANEL ───────────────────────────────────
            var shopPanel = Pnl("ShopPanel", root, V2(0,0), V2(1,1), V2(0,0), V2(0,0));
            Clr(shopPanel, new Color(0.05f,0.03f,0.15f,0.97f));
            Txt("ShopTitle", shopPanel.transform, V2(0.5f,0.5f),V2(0.5f,0.5f),
                V2(0,H*0.35f), V2(W*0.8f,80), "SHOP", 52, C_GOLD,
                TextAnchor.MiddleCenter, bold:true);
            Txt("ShopMsg", shopPanel.transform, V2(0.5f,0.5f),V2(0.5f,0.5f),
                V2(0,H*0.05f), V2(W*0.8f,120), "Coin packs & boosters\ncoming soon!",
                34, C_CYAN);
            // Free coins button for testing
            var freeCoins = Btn("FreeCoins", shopPanel.transform, V2(0.5f,0.5f),V2(0.5f,0.5f),
                V2(0,-H*0.06f), V2(W*0.82f,130), "Get 500 Coins (Free)", 38, C_GOLD, C_GOLD*0.7f, C_WHITE);
            var closeShop = Btn("CloseShop", shopPanel.transform, V2(0.5f,0.5f),V2(0.5f,0.5f),
                V2(0,-H*0.26f), V2(W*0.6f,120), "CLOSE", 42, C_RED, C_RED*0.7f, C_WHITE);
            shopPanel.SetActive(false);

            // Store panel refs for the controller
            SettingsPanel = setPanel;
            ShopPanel = shopPanel;

            // ── Wire MainMenuController ──────────────────────
            var ctrl = FindObjectOfType<MainMenuController>();
            if (ctrl != null)
            {
                PlayButton?.onClick.AddListener(ctrl.OnPlayPressed);
                SettingsButton?.onClick.AddListener(() => setPanel.SetActive(true));
                ShopButton?.onClick.AddListener(() => shopPanel.SetActive(true));
                DailyButton?.onClick.AddListener(ctrl.OnDailyRewardPressed);
                ctrl.UpdateHUDRefs(CoinsText, LivesText);
            }

            // Panel button wiring
            closeSet?.onClick.AddListener(() => setPanel.SetActive(false));
            closeShop?.onClick.AddListener(() => shopPanel.SetActive(false));

            soundBtn?.onClick.AddListener(() => {
                AudioManager.Instance?.ToggleSound();
                var t = soundBtn.transform.Find("Label")?.GetComponent<Text>();
                if (t != null) t.text = (AudioManager.Instance?.SoundOn ?? true) ? "Sound: ON" : "Sound: OFF";
            });
            musicBtn?.onClick.AddListener(() => {
                AudioManager.Instance?.ToggleMusic();
                var t = musicBtn.transform.Find("Label")?.GetComponent<Text>();
                if (t != null) t.text = (AudioManager.Instance?.MusicOn ?? true) ? "Music: ON" : "Music: OFF";
            });
            freeCoins?.onClick.AddListener(() => {
                if (SaveManager.Instance != null)
                {
                    SaveManager.Instance.Data.Coins += 500;
                    SaveManager.Instance.Data.BoosterHammer += 3;
                    SaveManager.Instance.Data.BoosterShuffle += 3;
                    SaveManager.Instance.Data.BoosterColorBlast += 3;
                    SaveManager.Instance.Save();
                    if (CoinsText != null) CoinsText.text = $"✦ {SaveManager.Instance.Data.Coins}";
                }
            });
        }

        [HideInInspector] public GameObject SettingsPanel;
        [HideInInspector] public GameObject ShopPanel;

        // ════════════════════════════════════════════════════
        // LEVEL MAP
        // ════════════════════════════════════════════════════
        void BuildLevelMap(Canvas cv)
        {
            var root = cv.GetComponent<RectTransform>();

            // BG
            var bg = Pnl("BG", root, V2(0,0),V2(1,1),V2(0,0),V2(0,0));
            Clr(bg, C_BG);

            // Header
            float hdrH = Mathf.Clamp(H*0.09f, 66f, 110f);
            var hdr = Pnl("Header", root, V2(0,1),V2(1,1),
                V2(0,-SafeTop-hdrH), V2(0,-SafeTop));
            Clr(hdr, C_DARK);
            Outline(hdr, C_GOLD.WithAlpha(0.5f), 2f);

            float bkW = Mathf.Clamp(W*0.18f,70,110);
            BackButton = Btn("BackBtn", hdr.RT(), V2(0,0),V2(0,1),
                V2(bkW*0.5f+8,0), V2(bkW,hdrH),
                "◀", W*0.06f, C_TRANS, C_TRANS, C_GOLD);

            Txt("HdrTitle", hdr.RT(), V2(0.15f,0),V2(0.85f,1),V2(0,0),V2(0,0),
                "Select Level", W*0.065f, C_GOLD, TextAnchor.MiddleCenter, bold:true);

            TotalStarsText = Txt("Stars", hdr.RT(), V2(0.75f,0),V2(1,1),V2(-8,0),V2(0,0),
                "⭐ 0", W*0.045f, C_GOLD, TextAnchor.MiddleCenter);

            // Scroll
            float topPad = SafeTop+hdrH+H*0.01f;
            float botPad = SafeBottom+H*0.01f;
            // Scroll — DON'T use Pnl (which adds Image) — create manually
            var scrollGO = new GameObject("ScrollView");
            scrollGO.transform.SetParent(root, false);
            var scrollRt = scrollGO.AddComponent<RectTransform>();
            scrollRt.anchorMin = V2(0,0); scrollRt.anchorMax = V2(1,1);
            scrollRt.offsetMin = V2(0,botPad); scrollRt.offsetMax = V2(0,-topPad);

            // Add Image for mask (only one!)
            var scrollImg = scrollGO.AddComponent<Image>();
            scrollImg.color = C_TRANS;
            scrollGO.AddComponent<Mask>().showMaskGraphic = false;

            var sr = scrollGO.AddComponent<ScrollRect>();
            sr.horizontal=false; sr.vertical=true;
            sr.scrollSensitivity=30f; sr.inertia=true;
            sr.decelerationRate=0.135f;
            sr.movementType=ScrollRect.MovementType.Elastic;
            sr.elasticity=0.1f;

            // Viewport — just a RectTransform, no Image needed
            var vp = new GameObject("Viewport");
            vp.transform.SetParent(scrollGO.transform, false);
            var vpRt = vp.AddComponent<RectTransform>();
            vpRt.anchorMin = V2(0,0); vpRt.anchorMax = V2(1,1);
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            sr.viewport = vpRt;

            // Content
            var ct = new GameObject("Content"); ct.transform.SetParent(vp.transform, false);
            var ctRt = ct.AddComponent<RectTransform>();
            ctRt.anchorMin=V2(0,1); ctRt.anchorMax=V2(1,1);
            ctRt.pivot=V2(0.5f,1); ctRt.anchoredPosition=Vector2.zero;
            ctRt.sizeDelta=Vector2.zero;
            var vlg=ct.AddComponent<VerticalLayoutGroup>();
            vlg.padding=new RectOffset(12,12,12,20);
            vlg.spacing=0; vlg.childAlignment=TextAnchor.UpperCenter;
            vlg.childControlWidth=true; vlg.childControlHeight=false;
            vlg.childForceExpandWidth=true; vlg.childForceExpandHeight=false;
            ct.AddComponent<ContentSizeFitter>().verticalFit=ContentSizeFitter.FitMode.PreferredSize;
            sr.content=ctRt;
            LevelContainer=ct.transform; LevelScroll=sr;
        }

        // ════════════════════════════════════════════════════
        // PUBLIC STATIC — Level Card Factory
        // ════════════════════════════════════════════════════
        public static GameObject CreateLevelCard(
            Transform parent, int num, bool unlocked, int stars,
            float cw, float ch, Action<int> onTap)
        {
            var card = new GameObject($"Level_{num:000}");
            card.transform.SetParent(parent,false);
            var rt = card.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(cw, ch);

            var img = card.AddComponent<Image>();
            img.color = unlocked ? new Color(0.25f, 0.15f, 0.50f) : new Color(0.15f, 0.10f, 0.25f);

            if (unlocked)
            {
                var ol = card.AddComponent<Outline>();
                ol.effectColor = stars>0
                    ? Hex("#FFD700").WithAlpha(0.9f)
                    : Hex("#FFD700").WithAlpha(0.3f);
                ol.effectDistance = new Vector2(2,-2);
            }

            // Level number
            float nsz = Mathf.Clamp(ch*0.38f, 20, 52);
            AddTxt(card, "Num", V2(0.5f,0.5f),V2(0.5f,0.5f),
                V2(0,ch*0.1f), V2(cw*0.9f,nsz+8),
                num.ToString(), nsz,
                unlocked ? Color.white : new Color(1,1,1,0.4f),
                TextAnchor.MiddleCenter, true);

            // Stars
            float stSz=Mathf.Clamp(ch*0.18f,10,22);
            float stGap=stSz*1.3f;
            for(int s=0;s<3;s++)
            {
                var sg=new GameObject($"S{s}"); sg.transform.SetParent(card.transform,false);
                var srt=sg.AddComponent<RectTransform>();
                srt.anchorMin=V2(0.5f,0.5f); srt.anchorMax=V2(0.5f,0.5f);
                srt.anchoredPosition=new Vector2(-stGap+(s*stGap),-ch*0.28f);
                srt.sizeDelta=new Vector2(stSz,stSz);
                var st=sg.AddComponent<Text>();
                st.text="★"; st.fontSize=Mathf.RoundToInt(stSz);
                st.color=s<stars?Hex("#FFD700"):new Color(1,1,1,0.35f);
                st.alignment=TextAnchor.MiddleCenter;
                st.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                st.raycastTarget=false;
            }

            // Lock overlay
            if (!unlocked)
            {
                var lk=new GameObject("Lock"); lk.transform.SetParent(card.transform,false);
                var lkRt=lk.AddComponent<RectTransform>();
                lkRt.anchorMin=Vector2.zero; lkRt.anchorMax=Vector2.one;
                lkRt.offsetMin=Vector2.zero; lkRt.offsetMax=Vector2.zero;
                lk.AddComponent<Image>().color=new Color(0,0,0,0.55f);
                var lkT=new GameObject("LkT"); lkT.transform.SetParent(lk.transform,false);
                var lkTRt=lkT.AddComponent<RectTransform>();
                lkTRt.anchorMin=Vector2.zero; lkTRt.anchorMax=Vector2.one;
                lkTRt.offsetMin=Vector2.zero; lkTRt.offsetMax=Vector2.zero;
                var lkTxt=lkT.AddComponent<Text>();
                lkTxt.text="🔒"; lkTxt.fontSize=Mathf.RoundToInt(ch*0.35f);
                lkTxt.color=new Color(1,1,1,0.6f);
                lkTxt.alignment=TextAnchor.MiddleCenter;
                lkTxt.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                lkTxt.raycastTarget=false;
            }

            // Button
            var btn=card.AddComponent<Button>();
            btn.interactable=unlocked;
            int cap=num;
            btn.onClick.AddListener(()=>onTap?.Invoke(cap));
            var cb=btn.colors;
            cb.normalColor=Color.white; cb.highlightedColor=new Color(1,1,1,0.85f);
            cb.pressedColor=new Color(0.7f,0.7f,0.7f,1); cb.disabledColor=Color.white;
            btn.colors=cb; btn.targetGraphic=img;
            return card;
        }

        // ════════════════════════════════════════════════════
        // CANVAS
        // ════════════════════════════════════════════════════
        Canvas GetOrMakeCanvas()
        {
            Canvas c = GetComponent<Canvas>();
            if (c == null) c = gameObject.AddComponent<Canvas>();
            c.renderMode = RenderMode.ScreenSpaceOverlay;

            CanvasScaler cs = GetComponent<CanvasScaler>();
            if (cs == null) cs = gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Portrait baseline shared by every scene. Match WIDTH (0) so the
            // layout is identical from 16:9 up to tall 20:9 phones — extra
            // height simply becomes vertical breathing room instead of
            // squashing/stretching the UI.
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight = 0f;

            if (GetComponent<GraphicRaycaster>() == null)
                gameObject.AddComponent<GraphicRaycaster>();

            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                var esGO = new GameObject("EventSystem");
                esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
                esGO.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
            return c;
        }

        // ════════════════════════════════════════════════════
        // PRIMITIVE HELPERS
        // ════════════════════════════════════════════════════
        static GameObject Pnl(string n,RectTransform p,Vector2 amin,Vector2 amax,Vector2 omin,Vector2 omax)
        {
            var g=new GameObject(n); g.transform.SetParent(p,false);
            var r=g.AddComponent<RectTransform>();
            r.anchorMin=amin; r.anchorMax=amax; r.offsetMin=omin; r.offsetMax=omax;
            g.AddComponent<Image>().color=Color.clear; return g;
        }
        static GameObject Pnl(string n,Transform p,Vector2 amin,Vector2 amax,Vector2 omin,Vector2 omax)
            =>Pnl(n,p.GetComponent<RectTransform>(),amin,amax,omin,omax);
        static void Clr(GameObject g,Color c){var i=g.GetComponent<Image>();if(i)i.color=c;}
        static Text Txt(string n,RectTransform p,Vector2 amin,Vector2 amax,Vector2 apos,Vector2 sd,
            string txt,float fsz,Color col,TextAnchor align=TextAnchor.MiddleCenter,bool bold=false)
        {
            var g=new GameObject(n); g.transform.SetParent(p,false);
            var r=g.AddComponent<RectTransform>();
            r.anchorMin=amin; r.anchorMax=amax; r.anchoredPosition=apos; r.sizeDelta=sd;
            var t=g.AddComponent<Text>(); t.text=txt; t.fontSize=Mathf.RoundToInt(fsz);
            t.color=col; t.alignment=align;
            t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontStyle=bold?FontStyle.Bold:FontStyle.Normal; t.raycastTarget=false; return t;
        }
        static Text Txt(string n,Transform p,Vector2 amin,Vector2 amax,Vector2 apos,Vector2 sd,
            string txt,float fsz,Color col,TextAnchor align=TextAnchor.MiddleCenter,bool bold=false)
            =>Txt(n,p.GetComponent<RectTransform>(),amin,amax,apos,sd,txt,fsz,col,align,bold);
        // Pick the closest rounded button sprite for a colour
        static Sprite PickBtnSprite(Color c)
        {
            // Choose by dominant hue
            if (c.g > c.r && c.g > c.b) return Resources.Load<Sprite>("UI/BtnGreen");
            if (c.b > c.r && c.b > c.g) return Resources.Load<Sprite>("UI/BtnBlue");
            if (c.r > 0.7f && c.g > 0.5f) return Resources.Load<Sprite>("UI/BtnGold");
            if (c.r > c.g && c.r > c.b) return Resources.Load<Sprite>("UI/BtnRed");
            if (c.r > 0.3f && c.b > 0.5f) return Resources.Load<Sprite>("UI/BtnPurple");
            return Resources.Load<Sprite>("UI/BtnDark");
        }

        static Button Btn(string n,RectTransform p,Vector2 amin,Vector2 amax,Vector2 apos,Vector2 sd,
            string lbl,float fsz,Color nc,Color pc,Color tc)
        {
            var g=new GameObject(n); g.transform.SetParent(p,false);
            var r=g.AddComponent<RectTransform>();
            r.anchorMin=amin; r.anchorMax=amax; r.anchoredPosition=apos; r.sizeDelta=sd;
            var img=g.AddComponent<Image>();

            // Use rounded sprite if available (9-sliced)
            Sprite btnSprite = PickBtnSprite(nc);
            if (btnSprite != null)
            {
                img.sprite = btnSprite;
                img.type = Image.Type.Sliced;
                img.color = Color.white;
            }
            else img.color = nc;

            var btn=g.AddComponent<Button>();
            var cb=btn.colors; cb.normalColor=Color.white; cb.highlightedColor=new Color(1.1f,1.1f,1.1f);
            cb.pressedColor=new Color(0.8f,0.8f,0.8f); cb.disabledColor=new Color(0.6f,0.6f,0.6f);
            btn.colors=cb; btn.targetGraphic=img;
            var tg=new GameObject("Label"); tg.transform.SetParent(g.transform,false);
            var tr=tg.AddComponent<RectTransform>();
            tr.anchorMin=Vector2.zero; tr.anchorMax=Vector2.one;
            tr.offsetMin=Vector2.zero; tr.offsetMax=Vector2.zero;
            var t=tg.AddComponent<Text>(); t.text=lbl; t.fontSize=Mathf.RoundToInt(fsz);
            t.color=tc; t.alignment=TextAnchor.MiddleCenter;
            t.font=Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontStyle=FontStyle.Bold; t.raycastTarget=false; return btn;
        }
        static Button Btn(string n,Transform p,Vector2 amin,Vector2 amax,Vector2 apos,Vector2 sd,
            string lbl,float fsz,Color nc,Color pc,Color tc)
            =>Btn(n,p.GetComponent<RectTransform>(),amin,amax,apos,sd,lbl,fsz,nc,pc,tc);

        // ── Multi-colour candy title (each letter a color) ───
        void BuildCandyTitle(RectTransform root)
        {
            string word = "CandyCraze";
            Color[] candyColors = {
                Hex("#FF4D6D"), Hex("#FF9F1C"), Hex("#FFD60A"), Hex("#06D6A0"),
                Hex("#118AB2"), Hex("#9B5DE5"), Hex("#FF4D6D"), Hex("#FF9F1C"),
                Hex("#06D6A0"), Hex("#118AB2"),
            };

            int n = word.Length;
            float letterW = 1f / n * 0.9f;      // relative width per letter
            float startX = 0.5f - (letterW * n) / 2f;
            float titleY = 0.80f;
            float fsz = Mathf.Clamp(W * 0.13f, 55f, 110f);

            for (int i = 0; i < n; i++)
            {
                float cx = startX + letterW * (i + 0.5f);
                Color col = candyColors[i % candyColors.Length];

                // Shadow
                var sh = new GameObject($"L{i}_sh");
                sh.transform.SetParent(root, false);
                var shRt = sh.AddComponent<RectTransform>();
                shRt.anchorMin = new Vector2(cx - letterW*0.7f, titleY - 0.08f);
                shRt.anchorMax = new Vector2(cx + letterW*0.7f, titleY + 0.08f);
                shRt.offsetMin = new Vector2(4,-4); shRt.offsetMax = new Vector2(4,-4);
                var shT = sh.AddComponent<Text>();
                shT.text = word[i].ToString(); shT.fontSize = Mathf.RoundToInt(fsz);
                shT.color = new Color(0,0,0,0.5f); shT.alignment = TextAnchor.MiddleCenter;
                shT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                shT.fontStyle = FontStyle.Bold; shT.raycastTarget = false;

                // Coloured letter
                var g = new GameObject($"L{i}");
                g.transform.SetParent(root, false);
                var rt = g.AddComponent<RectTransform>();
                rt.anchorMin = new Vector2(cx - letterW*0.7f, titleY - 0.08f);
                rt.anchorMax = new Vector2(cx + letterW*0.7f, titleY + 0.08f);
                rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
                var t = g.AddComponent<Text>();
                t.text = word[i].ToString(); t.fontSize = Mathf.RoundToInt(fsz);
                t.color = col; t.alignment = TextAnchor.MiddleCenter;
                t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
                t.fontStyle = FontStyle.Bold; t.raycastTarget = false;

                // White outline for candy pop
                var ol = g.AddComponent<Outline>();
                ol.effectColor = Color.white;
                ol.effectDistance = new Vector2(2, -2);
            }
        }

        // Oval-shaped chip (stretched circle) for HUD corners
        static GameObject OvalPanel(string n, RectTransform p, Vector2 amin, Vector2 amax, Color col)
        {
            var g = new GameObject(n); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = amin; r.anchorMax = amax;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var img = g.AddComponent<Image>();
            var circle = Resources.Load<Sprite>("UI/Circle");
            if (circle != null) { img.sprite = circle; img.color = col; }
            else img.color = col;
            // Glossy white rim for an attractive candy look
            var ol = g.AddComponent<Outline>();
            ol.effectColor = C_WHITE.WithAlpha(0.6f);
            ol.effectDistance = new Vector2(2f, -2f);
            return g;
        }

        // Rounded pill panel (no button) for HUD chips
        static GameObject BtnPanel(string n, RectTransform p, Vector2 amin, Vector2 amax, Color col)
        {
            var g = new GameObject(n); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = amin; r.anchorMax = amax;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var img = g.AddComponent<Image>();
            var sp = PickBtnSprite(col);
            if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; img.color = col; }
            else img.color = col;
            return g;
        }
        static void Outline(GameObject g,Color c,float w)
        { var o=g.AddComponent<Outline>(); o.effectColor=c; o.effectDistance=new Vector2(w,-w); }
        static void Shadow(GameObject g)
        { var s=g.AddComponent<Shadow>(); s.effectColor=new Color(0,0,0,0.55f); s.effectDistance=new Vector2(3,-3); }
        static Text AddTxt(GameObject p,string n,Vector2 amin,Vector2 amax,Vector2 apos,Vector2 sd,
            string txt,float fsz,Color col,TextAnchor align,bool bold)
            =>Txt(n,p.GetComponent<RectTransform>(),amin,amax,apos,sd,txt,fsz,col,align,bold);
        void GemRow(Transform p,Vector2 offset)
        {
            Color[] gc={Hex("#FF6B6B"),Hex("#FF9F45"),Hex("#FFD700"),
                        Hex("#2ECC71"),Hex("#4D9DE0"),Hex("#9B59B6")};
            float gsz=Mathf.Clamp(W*0.055f,22,36), gap=gsz*1.5f, tot=gap*5;
            for(int i=0;i<gc.Length;i++)
            {
                var g=new GameObject($"G{i}"); g.transform.SetParent(p,false);
                var r=g.AddComponent<RectTransform>();
                r.anchorMin=V2(0.5f,0.5f); r.anchorMax=V2(0.5f,0.5f);
                r.anchoredPosition=offset+new Vector2(-tot*0.5f+i*gap,0);
                r.sizeDelta=new Vector2(gsz,gsz);
                var img=g.AddComponent<Image>(); img.color=gc[i]; img.raycastTarget=false;
                var ol=g.AddComponent<Outline>(); ol.effectColor=Color.white.WithAlpha(0.5f);
                ol.effectDistance=new Vector2(1.5f,-1.5f);
            }
        }
        static Color Hex(string h)
        { ColorUtility.TryParseHtmlString(h,out Color c); return c; }
        static Vector2 V2(float x,float y)=>new Vector2(x,y);
    }

    // Extension for .RT()
    static class GOExt { public static RectTransform RT(this GameObject g)=>g.GetComponent<RectTransform>(); }
}
