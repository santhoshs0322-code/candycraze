// ============================================================
// SceneBuilder.cs (EDITOR ONLY)
// CandyCraze → Build All Scenes
// Clean minimal version — RuntimeUIBuilder handles all UI.
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CandyCraze.Editor
{
    public static class SceneBuilder
    {
        [MenuItem("CandyCraze/Build All Scenes (Auto-Wire)")]
        public static void BuildAll()
        {
            EnsureFolder("Assets/Scenes");
            EnsureFolder("Assets/Prefabs/UI");

            BuildBootstrap();
            BuildMainMenu();
            BuildLevelMap();
            BuildGame();
            SetBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Done!",
                "4 scenes built!\n\nBootstrap → MainMenu → LevelMap → Game",
                "OK");
        }

        // ════════════════════════════════════════════════════
        static void BuildBootstrap()
        {
            var sc = NewScene();
            MakeCam(new Color(0.05f,0.02f,0.12f));
            MakeES();

            // Persistent managers
            var mgr = MakeGO("Managers");
            AddComp<SaveManager>      (Child("SaveManager",     mgr));
            AddComp<AudioManager>     (Child("AudioManager",    mgr));
            AddComp<SceneController>  (Child("SceneController", mgr));
            AddComp<ObjectPool>       (Child("ObjectPool",      mgr));
            AddComp<LivesManager>     (Child("LivesManager",    mgr));
            AddComp<CurrencyManager>  (Child("CurrencyManager", mgr));
            AddComp<AdManager>        (Child("AdManager",       mgr));
            AddComp<IAPManager>       (Child("IAPManager",      mgr));
            AddComp<DailyRewardManager>(Child("DailyRwdMgr",   mgr));
            AddComp<ShopManager>      (Child("ShopManager",     mgr));
            AddComp<PremiumUIAnimator>(Child("PremiumUI",       mgr));

            AddComp<Bootstrap>(MakeGO("BootstrapRunner"));

            // Simple splash canvas
            var cv = MakeCanvas("SplashCanvas");
            var bg = MakePanel("BG", cv.transform);
            bg.GetComponent<Image>().color = new Color(0.05f,0.02f,0.12f);
            SetStretch(bg.GetComponent<RectTransform>());

            SaveScene(sc, "Bootstrap");
        }

        // ════════════════════════════════════════════════════
        static void BuildMainMenu()
        {
            var sc = NewScene();
            MakeCam(new Color(0.05f,0.02f,0.12f));
            MakeES();

            var mgr = MakeGO("Managers");
            AddComp<SaveManager>      (Child("SM",  mgr));
            AddComp<AudioManager>     (Child("AM",  mgr));
            AddComp<SceneController>  (Child("SC",  mgr));
            AddComp<DailyRewardManager>(Child("DR", mgr));
            AddComp<ShopManager>      (Child("SH",  mgr));
            AddComp<LivesManager>     (Child("LM",  mgr));
            AddComp<CurrencyManager>  (Child("CM",  mgr));
            AddComp<AdManager>        (Child("AD",  mgr));
            AddComp<IAPManager>       (Child("IP",  mgr));
            AddComp<PremiumUIAnimator>(Child("PU",  mgr));

            // RuntimeUIBuilder builds the entire UI at runtime
            var uiGO = MakeGO("UIBuilder");
            var rb   = AddComp<RuntimeUIBuilder>(uiGO);
            rb.SceneTarget = UISceneTarget.MainMenu;
            AddComp<MainMenuController>(uiGO);

            SaveScene(sc, "MainMenu");
        }

        // ════════════════════════════════════════════════════
        static void BuildLevelMap()
        {
            var sc = NewScene();
            MakeCam(new Color(0.05f,0.02f,0.12f));
            MakeES();

            var mgr = MakeGO("Managers");
            AddComp<SaveManager>   (Child("SM", mgr));
            AddComp<AudioManager>  (Child("AM", mgr));
            AddComp<SceneController>(Child("SC",mgr));

            // LevelMapController builds its own canvas and UI
            var mapGO = MakeGO("LevelMapController");
            AddComp<LevelMapController>(mapGO);

            SaveScene(sc, "LevelMap");
        }

        // ════════════════════════════════════════════════════
        static void BuildGame()
        {
            var sc = NewScene();

            // Camera + BoardScaler
            var camGO = MakeGO("Main Camera"); camGO.tag = "MainCamera";
            var cam   = camGO.AddComponent<Camera>();
            cam.orthographic = true; cam.orthographicSize = 5.5f;
            cam.backgroundColor = new Color(0.05f,0.02f,0.12f);
            cam.clearFlags = CameraClearFlags.SolidColor;
            camGO.AddComponent<AudioListener>();
            camGO.AddComponent<ScreenShake>();
            camGO.AddComponent<BoardScaler>();
            camGO.transform.position = new Vector3(3.5f, 3.5f, -10f);
            MakeES();

            var board = MakeGO("BoardRoot");
            board.transform.position = Vector3.zero;

            // Managers as ROOT objects (DontDestroyOnLoad only works on roots)
            var gmGO = MakeGO("GameManager");
            var lmGO = MakeGO("LevelManager");
            var bmGO = MakeGO("BoardManager");
            var tmGO = MakeGO("TileManager");
            var mdGO = MakeGO("MatchDetector");
            var gcGO = MakeGO("GravityCtrl");
            var smGO = MakeGO("ScoreManager");
            var omGO = MakeGO("ObjectiveMgr");
            var swGO = MakeGO("SwapController");
            var uiGO = MakeGO("UIManager");
            var spGO = MakeGO("SpecialHandler");

            MakeGO("SaveMgr").AddComponent<SaveManager>();
            MakeGO("AudMgr").AddComponent<AudioManager>();
            MakeGO("ScnCtrl").AddComponent<SceneController>();
            MakeGO("ObjPool").AddComponent<ObjectPool>();
            MakeGO("BstMgr").AddComponent<BoosterManager>();
            MakeGO("PrtMgr").AddComponent<ParticleManager>();
            MakeGO("BlstAn").AddComponent<BlastAnimator>();
            MakeGO("PremUI").AddComponent<PremiumUIAnimator>();

            var bm = AddComp<BoardManager>(bmGO);
            var tm = AddComp<TileManager>(tmGO);
            var sw = AddComp<SwapController>(swGO);
            var ui = AddComp<UIManager>(uiGO);
            AddComp<GameManager>(gmGO);
            AddComp<LevelManager>(lmGO);
            AddComp<MatchDetector>(mdGO);
            AddComp<GravityController>(gcGO);
            AddComp<ScoreManager>(smGO);
            AddComp<ObjectiveManager>(omGO);
            AddComp<SpecialPieceHandler>(spGO);

            // Wire BoardManager
            SetProp(bm, "_boardRoot", board.transform);

            // Wire TileManager
            var gem = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Gems/DefaultGem.prefab");
            if (gem != null) SetProp(tm, "_defaultGemPrefab", gem);

            // Wire SwapController
            SetProp(sw, "_boardManager", bm);
            SetProp(sw, "_gameCamera",   cam);

            // ── Candy-style WORLD-SPACE background (behind gems) ──
            // NOTE: must be a SpriteRenderer, NOT a ScreenSpaceOverlay UI
            // Image — an overlay image would render ON TOP of the board.
            // Parented to the camera so it always stays centered even after
            // BoardScaler repositions the camera at runtime.
            var bgSprite = Resources.Load<Sprite>("UI/BG_Game");
            if (bgSprite != null)
            {
                var bgGO = new GameObject("GameBG");
                bgGO.transform.SetParent(camGO.transform, false);
                bgGO.transform.localPosition = new Vector3(0f, 0f, 20f); // in front of far, behind gems
                var bgSr = bgGO.AddComponent<SpriteRenderer>();
                bgSr.sprite = bgSprite;
                bgSr.sortingOrder = -100;         // well behind gems (order 5)
                bgSr.color = Color.white;
                // Auto-fit to cover the camera on ANY aspect ratio (16:9 → 20:9).
                bgGO.AddComponent<CameraBackgroundFitter>();
            }

            // ── HUD Canvas — anchor-based, adaptive ──────────
            var cv = MakeCanvas("GameCanvas");

            // ── Top HUD — moved DOWN from the very top edge, three
            //    rounded candy chips for LEVEL / SCORE / MOVES. ──
            // The bar sits at 0.90–0.965 (below the safe-area/notch zone).
            var topBar = AnchorPanel(cv.transform, "TopBar",
                new Color(0,0,0,0f), new Vector2(0,0.90f), new Vector2(1,0.965f));

            // LEVEL / SCORE / MOVES chips — each builds its own label + value.
            // Left ~85% of the width; the pause button takes the right corner.
            var lvT = HudStatChip(topBar.transform, "LvChip",
                new Vector2(0.03f,0f), new Vector2(0.30f,1f),
                new Color(0.20f,0.45f,0.90f), "LEVEL", "Lv 1", Color.white);

            var scT = HudStatChip(topBar.transform, "ScChip",
                new Vector2(0.315f,0f), new Vector2(0.585f,1f),
                new Color(0.60f,0.30f,0.90f), "SCORE", "0", new Color(1f,0.9f,0.3f));

            var mvT = HudStatChip(topBar.transform, "MvChip",
                new Vector2(0.60f,0f), new Vector2(0.83f,1f),
                new Color(0.95f,0.45f,0.15f), "MOVES", "25", Color.white);

            // Pause button — round, in the top-right corner beside the chips.
            var pbGO = RoundButton(cv.transform,"PauseBtn","❚❚",
                new Vector2(0.85f,0.905f),new Vector2(0.955f,0.965f),
                new Color(0.95f,0.65f,0.10f),30);

            // ── Objective / Task bar — candy panel with a GEM ICON.
            //    Fully redesigned: header pill + a centered icon+text group. ──
            var objBar = AnchorPanel(cv.transform,"ObjBar",
                new Color(0.14f,0.08f,0.30f,0.98f),new Vector2(0.05f,0.775f),new Vector2(0.95f,0.895f));
            var objImg = objBar.GetComponent<Image>();
            var panelSp = Resources.Load<Sprite>("UI/Panel");
            if (panelSp != null) { objImg.sprite = panelSp; objImg.type = Image.Type.Sliced; objImg.color = new Color(0.14f,0.08f,0.30f,0.98f); }
            var objOutline = objBar.AddComponent<Outline>();
            objOutline.effectColor = new Color(1f,0.85f,0.2f,0.9f);
            objOutline.effectDistance = new Vector2(2,-2);

            // ── Header pill: "GOAL" chip centered at the top ──
            var goalPill = new GameObject("GoalPill");
            goalPill.transform.SetParent(objBar.transform, false);
            var gpRt = goalPill.AddComponent<RectTransform>();
            gpRt.anchorMin = new Vector2(0.36f, 0.72f);
            gpRt.anchorMax = new Vector2(0.64f, 0.98f);
            gpRt.offsetMin = Vector2.zero; gpRt.offsetMax = Vector2.zero;
            var gpImg = goalPill.AddComponent<Image>();
            var goldSp = Resources.Load<Sprite>("UI/BtnGold") ?? Resources.Load<Sprite>("UI/Panel");
            if (goldSp != null) { gpImg.sprite = goldSp; gpImg.type = Image.Type.Sliced; gpImg.color = Color.white; }
            else gpImg.color = new Color(0.95f,0.65f,0.10f);
            gpImg.raycastTarget = false;
            var gpTxt = new GameObject("T"); gpTxt.transform.SetParent(goalPill.transform, false);
            var gpTxtRt = gpTxt.AddComponent<RectTransform>();
            gpTxtRt.anchorMin = Vector2.zero; gpTxtRt.anchorMax = Vector2.one;
            gpTxtRt.offsetMin = Vector2.zero; gpTxtRt.offsetMax = Vector2.zero;
            var gpT = gpTxt.AddComponent<Text>();
            gpT.text = "GOAL"; gpT.color = new Color(0.15f,0.08f,0.02f);
            gpT.alignment = TextAnchor.MiddleCenter;
            gpT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            gpT.fontStyle = FontStyle.Bold;
            gpT.resizeTextForBestFit = true; gpT.resizeTextMinSize = 8; gpT.resizeTextMaxSize = 22;
            gpT.raycastTarget = false;

            // ── Centered content ROW: gem icon + text grouped together ──
            // Gem icon — fixed square just LEFT of center. Always active so
            // it reliably renders; UIManager sets its sprite (and hides it via
            // color alpha only for score-only levels).
            var objIconGO = new GameObject("ObjIcon");
            objIconGO.transform.SetParent(objBar.transform, false);
            var oiRt = objIconGO.AddComponent<Image>().rectTransform;
            oiRt.anchorMin = new Vector2(0.34f, 0.14f);
            oiRt.anchorMax = new Vector2(0.44f, 0.58f);
            oiRt.offsetMin = Vector2.zero; oiRt.offsetMax = Vector2.zero;
            var objIcon = objIconGO.GetComponent<Image>();
            objIcon.preserveAspect = true;
            objIcon.raycastTarget = false;

            // Objective text — DEFAULT is full-width & centered (good for the
            // score-only line). UIManager narrows it beside the gem icon only
            // for collect-gem levels. Fixed size (no tiny best-fit shrink).
            var obTGO = new GameObject("OT");
            obTGO.transform.SetParent(objBar.transform, false);
            var obtRt = obTGO.AddComponent<RectTransform>();
            obtRt.anchorMin = new Vector2(0.06f, 0.06f);
            obtRt.anchorMax = new Vector2(0.94f, 0.62f);
            obtRt.offsetMin = Vector2.zero; obtRt.offsetMax = Vector2.zero;
            var obT = obTGO.AddComponent<Text>();
            obT.text = "Loading...";
            obT.color = Color.white;
            obT.alignment = TextAnchor.MiddleCenter;
            obT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            obT.fontStyle = FontStyle.Bold;
            obT.fontSize = 40;                    // big, fixed
            obT.resizeTextForBestFit = true;
            obT.resizeTextMinSize = 22;           // never shrink below this
            obT.resizeTextMaxSize = 44;
            obT.horizontalOverflow = HorizontalWrapMode.Overflow;
            obT.verticalOverflow = VerticalWrapMode.Overflow;
            var obtShadow = obTGO.AddComponent<Shadow>();
            obtShadow.effectColor = new Color(0,0,0,0.6f);
            obtShadow.effectDistance = new Vector2(1.5f,-1.5f);

            // Combo text
            var cbGO = new GameObject("ComboTxt");
            cbGO.transform.SetParent(cv.transform,false);
            var cbRt = cbGO.AddComponent<RectTransform>();
            cbRt.anchorMin = new Vector2(0.05f,0.45f);
            cbRt.anchorMax = new Vector2(0.95f,0.60f);
            cbRt.offsetMin = cbRt.offsetMax = Vector2.zero;
            var cbT = cbGO.AddComponent<Text>();
            cbT.text = "COMBO!"; cbT.fontSize = 62;
            cbT.color = new Color(1f,0.85f,0.2f);
            cbT.alignment = TextAnchor.MiddleCenter;
            cbT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            cbT.fontStyle = FontStyle.Bold;
            cbGO.SetActive(false);

            // Booster bar — a contained rounded card (inset from edges),
            // matching the Level/Score/Moves chip style.
            var bstBar = AnchorPanel(cv.transform,"BoosterBar",
                new Color(0.12f,0.06f,0.26f,0.98f),
                new Vector2(0.03f,0.015f), new Vector2(0.97f,0.145f));
            var bstBarImg = bstBar.GetComponent<Image>();
            var bstPanelSp = Resources.Load<Sprite>("UI/Panel");
            if (bstPanelSp != null) { bstBarImg.sprite = bstPanelSp; bstBarImg.type = Image.Type.Sliced; }
            var bstOutline = bstBar.AddComponent<Outline>();
            bstOutline.effectColor = new Color(0.2f,0.9f,1f,0.8f);
            bstOutline.effectDistance = new Vector2(2,-2);

            // Header strip label (auto-fit, like the stat chips)
            var bstLblGO = new GameObject("BstLbl");
            bstLblGO.transform.SetParent(bstBar.transform, false);
            var bstLblRt = bstLblGO.AddComponent<RectTransform>();
            bstLblRt.anchorMin = new Vector2(0.2f, 0.84f);
            bstLblRt.anchorMax = new Vector2(0.8f, 0.99f);
            bstLblRt.offsetMin = Vector2.zero; bstLblRt.offsetMax = Vector2.zero;
            var bstLblT = bstLblGO.AddComponent<Text>();
            bstLblT.text = "★  BOOSTERS  ★";
            bstLblT.color = new Color(1f,0.85f,0.2f);
            bstLblT.alignment = TextAnchor.MiddleCenter;
            bstLblT.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            bstLblT.fontStyle = FontStyle.Bold;
            bstLblT.resizeTextForBestFit = true;
            bstLblT.resizeTextMinSize = 8; bstLblT.resizeTextMaxSize = 22;
            bstLblT.raycastTarget = false;

            var bstHUD = bstBar.AddComponent<BoosterHUDController>();
            var hB = BoostBtn(bstBar.transform,"H","Hammer\nx0",0, new Color(0.25f,0.50f,0.95f));
            var rB = BoostBtn(bstBar.transform,"R","Row\nx0",  1, new Color(0.98f,0.55f,0.15f));
            var sB = BoostBtn(bstBar.transform,"S","Shuffle\nx0",2,new Color(0.20f,0.80f,0.40f));
            var eB = BoostBtn(bstBar.transform,"E","+Moves\nx0",3, new Color(0.62f,0.30f,0.95f));
            var cB = BoostBtn(bstBar.transform,"C","Color\nx0", 4, new Color(0.95f,0.25f,0.60f));
            var bhSo = new SerializedObject(bstHUD);
            bhSo.FindProperty("_hammerBtn").objectReferenceValue     = hB.GetComponent<Button>();
            bhSo.FindProperty("_rowBlastBtn").objectReferenceValue   = rB.GetComponent<Button>();
            bhSo.FindProperty("_shuffleBtn").objectReferenceValue    = sB.GetComponent<Button>();
            bhSo.FindProperty("_extraMovesBtn").objectReferenceValue = eB.GetComponent<Button>();
            bhSo.FindProperty("_colorBlastBtn").objectReferenceValue = cB.GetComponent<Button>();
            bhSo.ApplyModifiedProperties();

            // Win panel
            var winP = FullPanel(cv.transform,"WinPanel",new Color(0.03f,0.20f,0.05f,0.97f));
            winP.SetActive(false);
            AnchorText(winP.transform,"WT","YOU WIN!",     60,new Color(1f,0.85f,0.2f),new Vector2(0.5f,0.90f),bold:true);
            AnchorText(winP.transform,"WS","Level Complete",26,Color.cyan,new Vector2(0.5f,0.82f));
            var s1 = StarImg(winP.transform, new Vector2(0.22f,0.72f));
            var s2 = StarImg(winP.transform, new Vector2(0.50f,0.75f));
            var s3 = StarImg(winP.transform, new Vector2(0.78f,0.72f));
            AnchorText(winP.transform,"WSL","SCORE",22,Color.cyan,new Vector2(0.5f,0.64f));
            var wSc = AnchorText(winP.transform,"WSV","0",50,new Color(1f,0.85f,0.2f),new Vector2(0.5f,0.56f),bold:true);
            var nxt = AnchorButton(winP.transform,"NBtn","NEXT LEVEL", new Vector2(0.08f,0.30f),new Vector2(0.92f,0.41f),new Color(0.1f,0.7f,0.3f),36);
            var rpl = AnchorButton(winP.transform,"RBtn","REPLAY",     new Vector2(0.08f,0.17f),new Vector2(0.92f,0.27f),new Color(0.1f,0.3f,0.8f),30);
            var wMp = AnchorButton(winP.transform,"WBtn","QUIT",       new Vector2(0.08f,0.05f),new Vector2(0.92f,0.14f),new Color(0.1f,0.06f,0.22f),26);

            // Lose panel
            var loseP = FullPanel(cv.transform,"LosePanel",new Color(0.20f,0.02f,0.02f,0.97f));
            loseP.SetActive(false);
            AnchorText(loseP.transform,"LT","LEVEL FAILED",50,Color.red,     new Vector2(0.5f,0.90f),bold:true);
            AnchorText(loseP.transform,"LS","Don't give up!",24,Color.white,  new Vector2(0.5f,0.82f));
            AnchorText(loseP.transform,"LL","SCORE",22,Color.cyan,            new Vector2(0.5f,0.72f));
            var lSc = AnchorText(loseP.transform,"LV","0",48,new Color(1f,0.85f,0.2f),new Vector2(0.5f,0.63f),bold:true);
            var lOb = AnchorText(loseP.transform,"LO","",20,Color.white,      new Vector2(0.5f,0.53f));
            lOb.alignment = TextAnchor.UpperCenter;
            var ret = AnchorButton(loseP.transform,"Rt","RETRY",      new Vector2(0.08f,0.33f),new Vector2(0.92f,0.44f),new Color(0.1f,0.7f,0.3f),36);
            var lMp = AnchorButton(loseP.transform,"Lp","QUIT",       new Vector2(0.08f,0.07f),new Vector2(0.92f,0.17f),new Color(0.7f,0.1f,0.1f),28);

            // Pause panel
            var pauseP = FullPanel(cv.transform,"PausePanel",new Color(0,0,0,0.96f));
            pauseP.SetActive(false);
            AnchorText(pauseP.transform,"PT","PAUSED",56,new Color(1f,0.85f,0.2f),new Vector2(0.5f,0.88f),bold:true);
            var pAudio = AddComp<MainMenuController>(MakeGO("AudHlp"));
            var pSnd = AnchorButton(pauseP.transform,"PS","Sound: ON",   new Vector2(0.08f,0.67f),new Vector2(0.92f,0.77f),new Color(0.12f,0.07f,0.28f),28);
            var pMus = AnchorButton(pauseP.transform,"PM","Music: ON",   new Vector2(0.08f,0.54f),new Vector2(0.92f,0.64f),new Color(0.12f,0.07f,0.28f),28);
            var res  = AnchorButton(pauseP.transform,"Re","RESUME",      new Vector2(0.08f,0.38f),new Vector2(0.92f,0.50f),new Color(0.1f,0.7f,0.3f),36);
            var rst  = AnchorButton(pauseP.transform,"Rs","RESTART",     new Vector2(0.08f,0.24f),new Vector2(0.92f,0.34f),new Color(0.1f,0.3f,0.8f),30);
            var pMp  = AnchorButton(pauseP.transform,"Pp","QUIT TO MAP", new Vector2(0.08f,0.10f),new Vector2(0.92f,0.20f),new Color(0.7f,0.1f,0.1f),26);

            // Wire UIManager
            var uiSo = new SerializedObject(ui);
            uiSo.FindProperty("_scoreText").objectReferenceValue     = scT;
            uiSo.FindProperty("_movesText").objectReferenceValue     = mvT;
            uiSo.FindProperty("_objectiveText").objectReferenceValue = obT;
            uiSo.FindProperty("_objectiveIcon").objectReferenceValue = objIcon;
            uiSo.FindProperty("_comboText").objectReferenceValue     = cbT;
            uiSo.FindProperty("_winPanel").objectReferenceValue      = winP;
            uiSo.FindProperty("_winScoreText").objectReferenceValue  = wSc;
            uiSo.FindProperty("_losePanel").objectReferenceValue     = loseP;
            uiSo.FindProperty("_loseScoreText").objectReferenceValue = lSc;
            uiSo.FindProperty("_pausePanel").objectReferenceValue    = pauseP;
            uiSo.FindProperty("_livesText").objectReferenceValue     = lvT;
            var stP = uiSo.FindProperty("_starImages"); stP.arraySize = 3;
            stP.GetArrayElementAtIndex(0).objectReferenceValue = s1;
            stP.GetArrayElementAtIndex(1).objectReferenceValue = s2;
            stP.GetArrayElementAtIndex(2).objectReferenceValue = s3;
            uiSo.ApplyModifiedProperties();

            // Wire buttons
            Wire(pbGO,  ui,    "OnPausePressed");
            Wire(nxt,   ui,    "OnNextLevelPressed");
            Wire(rpl,   ui,    "OnRestartPressed");
            Wire(wMp,   ui,    "OnQuitToMapPressed");
            Wire(ret,   ui,    "OnRestartPressed");
            Wire(lMp,   ui,    "OnQuitToMapPressed");
            Wire(res,   ui,    "OnResumePressed");
            Wire(rst,   ui,    "OnRestartPressed");
            Wire(pMp,   ui,    "OnQuitToMapPressed");
            Wire(pSnd,  pAudio,"OnSoundToggle");
            Wire(pMus,  pAudio,"OnMusicToggle");

            SaveScene(sc, "Game");
        }

        // ════════════════════════════════════════════════════
        static void SetBuildSettings()
        {
            EditorBuildSettings.scenes = new[] {
                new EditorBuildSettingsScene("Assets/Scenes/Bootstrap.unity", true),
                new EditorBuildSettingsScene("Assets/Scenes/MainMenu.unity",  true),
                new EditorBuildSettingsScene("Assets/Scenes/LevelMap.unity",  true),
                new EditorBuildSettingsScene("Assets/Scenes/Game.unity",      true),
            };
        }

        // ════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════
        static UnityEngine.SceneManagement.Scene NewScene()
            => EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        static void SaveScene(UnityEngine.SceneManagement.Scene sc, string name)
        {
            EditorSceneManager.SaveScene(sc, $"Assets/Scenes/{name}.unity");
            Debug.Log($"[SceneBuilder] Saved {name}");
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string p = System.IO.Path.GetDirectoryName(path).Replace('\\','/');
                string f = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(p, f);
            }
        }

        static void MakeCam(Color bg)
        {
            var g = new GameObject("Main Camera"); g.tag = "MainCamera";
            var c = g.AddComponent<Camera>(); c.backgroundColor = bg;
            g.AddComponent<AudioListener>();
            g.transform.position = new Vector3(0,0,-10);
        }

        static void MakeES()
        {
            var e = new GameObject("EventSystem");
            e.AddComponent<EventSystem>();
            e.AddComponent<StandaloneInputModule>();
        }

        static GameObject MakeCanvas(string name)
        {
            var g  = new GameObject(name);
            var cv = g.AddComponent<Canvas>();
            cv.renderMode = RenderMode.ScreenSpaceOverlay;
            var cs = g.AddComponent<CanvasScaler>();
            cs.uiScaleMode         = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            // Shared portrait baseline, match WIDTH for consistent phone layout
            cs.referenceResolution = new Vector2(1080,1920);
            cs.screenMatchMode     = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight  = 0f;
            g.AddComponent<GraphicRaycaster>();
            return g;
        }

        static GameObject MakeGO(string n) => new GameObject(n);

        static GameObject Child(string n, GameObject p)
        {
            var g = new GameObject(n);
            g.transform.SetParent(p.transform, false);
            return g;
        }

        static T AddComp<T>(GameObject g) where T : UnityEngine.Component
            => g.AddComponent<T>();

        static GameObject MakePanel(string n, Transform p)
        {
            var g = new GameObject(n); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            g.AddComponent<Image>().color = Color.clear;
            return g;
        }

        static void SetStretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;
        }

        static GameObject AnchorPanel(Transform p, string n, Color c, Vector2 amin, Vector2 amax)
        {
            var g = new GameObject(n); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = amin; r.anchorMax = amax;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            g.AddComponent<Image>().color = c;
            return g;
        }

        // Rounded HUD stat chip with a label (top) + big auto-fit value
        // (bottom). Returns the VALUE Text so it can be wired to UIManager.
        static Text HudStatChip(Transform p, string n, Vector2 amin, Vector2 amax,
            Color c, string label, string value, Color valueColor)
        {
            // Chip background
            var g = new GameObject(n); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = amin; r.anchorMax = amax;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var img = g.AddComponent<Image>();
            var sp = Resources.Load<Sprite>("UI/Panel");
            if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; img.color = c; }
            else img.color = c;
            var ol = g.AddComponent<Outline>();
            ol.effectColor = new Color(1,1,1,0.35f);
            ol.effectDistance = new Vector2(1.5f,-1.5f);

            // Label (top strip)
            var lg = new GameObject("Lbl"); lg.transform.SetParent(g.transform, false);
            var lr = lg.AddComponent<RectTransform>();
            lr.anchorMin = new Vector2(0.05f, 0.58f);
            lr.anchorMax = new Vector2(0.95f, 0.96f);
            lr.offsetMin = Vector2.zero; lr.offsetMax = Vector2.zero;
            var lt = lg.AddComponent<Text>();
            lt.text = label; lt.color = new Color(1,1,1,0.85f);
            lt.alignment = TextAnchor.MiddleCenter;
            lt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            lt.resizeTextForBestFit = true;
            lt.resizeTextMinSize = 8; lt.resizeTextMaxSize = 22;
            lt.raycastTarget = false;

            // Value (bottom, big, auto-fit — never clips)
            var vg = new GameObject("Val"); vg.transform.SetParent(g.transform, false);
            var vr = vg.AddComponent<RectTransform>();
            vr.anchorMin = new Vector2(0.05f, 0.06f);
            vr.anchorMax = new Vector2(0.95f, 0.58f);
            vr.offsetMin = Vector2.zero; vr.offsetMax = Vector2.zero;
            var vt = vg.AddComponent<Text>();
            vt.text = value; vt.color = valueColor;
            vt.alignment = TextAnchor.MiddleCenter;
            vt.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            vt.fontStyle = FontStyle.Bold;
            vt.resizeTextForBestFit = true;
            vt.resizeTextMinSize = 10; vt.resizeTextMaxSize = 40;
            vt.raycastTarget = false;
            var sh = vg.AddComponent<Shadow>();
            sh.effectColor = new Color(0,0,0,0.5f);
            sh.effectDistance = new Vector2(1.5f,-1.5f);

            return vt;
        }

        static GameObject FullPanel(Transform p, string n, Color c)
        {
            var g = new GameObject(n); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            g.AddComponent<Image>().color = c;
            return g;
        }

        // Task text that reliably shows: fills the lower part of the bar,
        // auto-resizes to fit, and overflows instead of clipping to blank.
        static Text BigTaskText(Transform p, string n, string txt, Color col)
        {
            var g = new GameObject(n); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(0.03f, 0.05f);
            r.anchorMax = new Vector2(0.97f, 0.68f);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var t = g.AddComponent<Text>();
            t.text = txt; t.color = col;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontStyle = FontStyle.Bold;
            // Auto-fit so any objective string is always visible. Truncate
            // vertical overflow so best-fit shrinks multi-line text to fit
            // the box height (prevents lines spilling out / overlapping).
            t.resizeTextForBestFit = true;
            t.resizeTextMinSize = 10;
            t.resizeTextMaxSize = 30;
            t.lineSpacing = 1f;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            // Dark shadow for readability on the purple panel
            var sh = g.AddComponent<Shadow>();
            sh.effectColor = new Color(0,0,0,0.6f);
            sh.effectDistance = new Vector2(1.5f,-1.5f);
            return t;
        }

        static Text AnchorText(Transform p, string n, string txt, int sz, Color col,
            Vector2 anchor, bool bold = false)
        {
            var g = new GameObject(n); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(anchor.x-0.45f, anchor.y-0.1f);
            r.anchorMax = new Vector2(anchor.x+0.45f, anchor.y+0.1f);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var t = g.AddComponent<Text>();
            t.text = txt; t.fontSize = sz; t.color = col;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            return t;
        }

        static GameObject AnchorButton(Transform p, string n, string lbl,
            Vector2 amin, Vector2 amax, Color c, int sz)
        {
            var g = new GameObject(n); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = amin; r.anchorMax = amax;
            r.offsetMin = new Vector2(8,4); r.offsetMax = new Vector2(-8,-4);
            var img = g.AddComponent<Image>();

            // Use rounded button sprite (9-sliced) — matches home page
            Sprite sp = PickBtnSprite(c);
            if (sp != null) { img.sprite = sp; img.type = Image.Type.Sliced; img.color = Color.white; }
            else img.color = c;

            var btn = g.AddComponent<Button>();
            var cb  = btn.colors;
            cb.normalColor = Color.white; cb.highlightedColor = new Color(1.1f,1.1f,1.1f);
            cb.pressedColor = new Color(0.82f,0.82f,0.82f); btn.colors = cb;
            btn.targetGraphic = img;

            // Shadow text
            var shg = new GameObject("Shadow"); shg.transform.SetParent(g.transform,false);
            var shr = shg.AddComponent<RectTransform>();
            shr.anchorMin = Vector2.zero; shr.anchorMax = Vector2.one;
            shr.offsetMin = new Vector2(2,-3); shr.offsetMax = new Vector2(2,-3);
            var sht = shg.AddComponent<Text>();
            sht.text = lbl; sht.fontSize = sz; sht.color = new Color(0,0,0,0.5f);
            sht.alignment = TextAnchor.MiddleCenter;
            sht.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            sht.fontStyle = FontStyle.Bold; sht.raycastTarget = false;

            var tg = new GameObject("Label"); tg.transform.SetParent(g.transform,false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            var t = tg.AddComponent<Text>();
            t.text = lbl; t.fontSize = sz; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontStyle = FontStyle.Bold; t.raycastTarget = false;
            return g;
        }

        // Round (circular) button — uses Circle sprite
        static GameObject RoundButton(Transform p, string n, string lbl,
            Vector2 amin, Vector2 amax, Color c, int sz)
        {
            var g = new GameObject(n); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = amin; r.anchorMax = amax;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var img = g.AddComponent<Image>();
            var circle = Resources.Load<Sprite>("UI/Circle");
            if (circle != null) { img.sprite = circle; img.color = c; img.preserveAspect = true; }
            else img.color = c;

            var btn = g.AddComponent<Button>();
            var cb  = btn.colors;
            cb.normalColor = Color.white; cb.highlightedColor = new Color(1.15f,1.15f,1.15f);
            cb.pressedColor = new Color(0.8f,0.8f,0.8f); btn.colors = cb;
            btn.targetGraphic = img;

            var ol = g.AddComponent<Outline>();
            ol.effectColor = new Color(1,1,1,0.6f);
            ol.effectDistance = new Vector2(1.5f,-1.5f);

            var tg = new GameObject("Label"); tg.transform.SetParent(g.transform,false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            var t = tg.AddComponent<Text>();
            t.text = lbl; t.fontSize = sz; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontStyle = FontStyle.Bold; t.raycastTarget = false;
            return g;
        }

        // Pick rounded button sprite by colour
        static Sprite PickBtnSprite(Color c)
        {
            if (c.g > c.r && c.g > c.b) return Resources.Load<Sprite>("UI/BtnGreen");
            if (c.b > c.r && c.b > c.g) return Resources.Load<Sprite>("UI/BtnBlue");
            if (c.r > 0.8f && c.g > 0.5f) return Resources.Load<Sprite>("UI/BtnGold");
            if (c.r > c.g && c.r > c.b) return Resources.Load<Sprite>("UI/BtnRed");
            if (c.r > 0.4f && c.b > 0.6f) return Resources.Load<Sprite>("UI/BtnPurple");
            return Resources.Load<Sprite>("UI/BtnDark");
        }

        static Image StarImg(Transform p, Vector2 a)
        {
            var g = new GameObject("Star"); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(a.x-0.12f, a.y-0.085f);
            r.anchorMax = new Vector2(a.x+0.12f, a.y+0.085f);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var i = g.AddComponent<Image>();
            i.preserveAspect = true;
            // Use a star sprite so it's a star shape, not a square
            var starSp = Resources.Load<Sprite>("UI/Star");
            if (starSp != null) { i.sprite = starSp; i.color = new Color(0.35f,0.35f,0.45f,0.6f); }
            else i.color = new Color(0.35f,0.35f,0.45f,0.6f);
            // Gold glow outline so filled stars pop
            var ol = g.AddComponent<Outline>();
            ol.effectColor = new Color(1f,0.85f,0.2f,0.5f);
            ol.effectDistance = new Vector2(2,-2);
            return i;
        }

        static GameObject BoostBtn(Transform p, string n, string lbl, int idx, Color c)
        {
            float s = 1f/5f;
            float a0 = idx*s+0.004f, a1 = (idx+1)*s-0.004f;
            var g = new GameObject(n); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            // Leave room for the "BOOSTERS" label at the top of the bar
            r.anchorMin = new Vector2(a0,0.05f); r.anchorMax = new Vector2(a1,0.80f);
            r.offsetMin = new Vector2(6,4); r.offsetMax = new Vector2(-6,-4);

            // Rounded candy slot background (tinted)
            var img = g.AddComponent<Image>();
            var slotSp = Resources.Load<Sprite>("UI/Panel");
            if (slotSp != null) { img.sprite = slotSp; img.type = Image.Type.Sliced; }
            img.color = c;

            var btn = g.AddComponent<Button>();
            var cb  = btn.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1.15f,1.15f,1.15f);
            cb.pressedColor = new Color(0.8f,0.8f,0.8f);
            cb.disabledColor = new Color(0.55f,0.55f,0.6f);
            btn.colors = cb; btn.targetGraphic = img;

            // Glossy rim
            var ol = g.AddComponent<Outline>();
            ol.effectColor = new Color(1,1,1,0.35f);
            ol.effectDistance = new Vector2(1.5f,-1.5f);

            // Icon image (upper portion)
            string[] iconNames = { "Hammer","RowBlast","Shuffle","ExtraMoves","ColorBlast" };
            Sprite iconSprite = idx < iconNames.Length
                ? Resources.Load<Sprite>($"Powers/{iconNames[idx]}") : null;
            if (iconSprite != null)
            {
                var iconGO = new GameObject("Icon");
                iconGO.transform.SetParent(g.transform, false);
                var iRt = iconGO.AddComponent<RectTransform>();
                iRt.anchorMin = new Vector2(0.16f, 0.30f);
                iRt.anchorMax = new Vector2(0.84f, 0.94f);
                iRt.offsetMin = Vector2.zero; iRt.offsetMax = Vector2.zero;
                var iImg = iconGO.AddComponent<Image>();
                iImg.sprite = iconSprite;
                iImg.color = Color.white;
                iImg.preserveAspect = true;
                iImg.raycastTarget = false;
            }

            // Count badge — gold pill at the bottom
            var badge = new GameObject("CountBadge");
            badge.transform.SetParent(g.transform, false);
            var bRt = badge.AddComponent<RectTransform>();
            bRt.anchorMin = new Vector2(0.5f, 0f);
            bRt.anchorMax = new Vector2(0.5f, 0f);
            bRt.pivot     = new Vector2(0.5f, 0f);
            bRt.anchoredPosition = new Vector2(0, 2);
            bRt.sizeDelta = new Vector2(64, 34);
            var bImg = badge.AddComponent<Image>();
            var badgeSp = Resources.Load<Sprite>("UI/BtnGold") ?? Resources.Load<Sprite>("UI/Panel");
            if (badgeSp != null) { bImg.sprite = badgeSp; bImg.type = Image.Type.Sliced; bImg.color = Color.white; }
            else bImg.color = new Color(0.95f,0.65f,0.10f);
            bImg.raycastTarget = false;

            // Count label sits inside the badge — named "Label" so the
            // BoosterHUDController can find and update it ("x0" -> "x3").
            var tg = new GameObject("Label"); tg.transform.SetParent(badge.transform,false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            var t = tg.AddComponent<Text>();
            t.text = iconSprite != null ? "x0" : lbl;
            t.fontSize = 22; t.color = new Color(0.15f,0.08f,0.02f);
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontStyle = FontStyle.Bold;
            t.raycastTarget = false;
            return g;
        }

        static void Wire(GameObject go, MonoBehaviour tgt, string method)
        {
            if (go == null || tgt == null) return;
            var btn = go.GetComponent<Button>(); if (btn == null) return;
            var so  = new SerializedObject(btn);
            var arr = so.FindProperty("m_OnClick.m_PersistentCalls.m_Calls");
            arr.arraySize++;
            var el = arr.GetArrayElementAtIndex(arr.arraySize-1);
            el.FindPropertyRelative("m_Target").objectReferenceValue = tgt;
            el.FindPropertyRelative("m_MethodName").stringValue = method;
            el.FindPropertyRelative("m_Mode").enumValueIndex = 1;
            el.FindPropertyRelative("m_CallState").enumValueIndex = 2;
            so.ApplyModifiedProperties();
        }

        static void SetProp(UnityEngine.Component comp, string prop, UnityEngine.Object val)
        {
            var so = new SerializedObject(comp);
            so.FindProperty(prop).objectReferenceValue = val;
            so.ApplyModifiedProperties();
        }
    }
}
#endif
