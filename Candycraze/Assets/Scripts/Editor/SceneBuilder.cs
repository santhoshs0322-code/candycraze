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

            // RuntimeUIBuilder builds scroll view; LevelMapController fills it
            var uiGO = MakeGO("UIBuilder");
            var rb   = AddComp<RuntimeUIBuilder>(uiGO);
            rb.SceneTarget = UISceneTarget.LevelMap;
            AddComp<LevelMapController>(uiGO);

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

            var mgr  = MakeGO("Managers");
            var gmGO = Child("GameManager",     mgr);
            var lmGO = Child("LevelManager",    mgr);
            var bmGO = Child("BoardManager",    mgr);
            var tmGO = Child("TileManager",     mgr);
            var mdGO = Child("MatchDetector",   mgr);
            var gcGO = Child("GravityCtrl",     mgr);
            var smGO = Child("ScoreManager",    mgr);
            var omGO = Child("ObjectiveMgr",    mgr);
            var swGO = Child("SwapController",  mgr);
            var uiGO = Child("UIManager",       mgr);
            var spGO = Child("SpecialHandler",  mgr);

            Child("SaveMgr",    mgr).AddComponent<SaveManager>();
            Child("AudMgr",     mgr).AddComponent<AudioManager>();
            Child("ScnCtrl",    mgr).AddComponent<SceneController>();
            Child("ObjPool",    mgr).AddComponent<ObjectPool>();
            Child("BstMgr",     mgr).AddComponent<BoosterManager>();
            Child("PrtMgr",     mgr).AddComponent<ParticleManager>();
            Child("BlstAn",     mgr).AddComponent<BlastAnimator>();
            Child("PremUI",     mgr).AddComponent<PremiumUIAnimator>();

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

            // ── HUD Canvas — anchor-based, adaptive ──────────
            var cv = MakeCanvas("GameCanvas");

            // Top bar
            var topBar = AnchorPanel(cv.transform, "TopBar",
                new Color(0,0,0,0.88f), new Vector2(0,0.88f), Vector2.one);
            var lvT = AnchorText(topBar.transform, "LvT", "♥♥♥♥♥",
                24, Color.red, new Vector2(0.17f,0.42f), bold:true);
            AnchorText(topBar.transform,"LvL","LIVES",14,
                new Color(0.2f,0.9f,1f), new Vector2(0.17f,0.82f));
            var scT = AnchorText(topBar.transform,"ScT","0",
                30,new Color(1f,0.85f,0.2f),new Vector2(0.50f,0.42f),bold:true);
            AnchorText(topBar.transform,"ScL","SCORE",14,
                new Color(0.2f,0.9f,1f),new Vector2(0.50f,0.82f));
            var mvT = AnchorText(topBar.transform,"MvT","25",
                34,Color.white,new Vector2(0.83f,0.42f),bold:true);
            AnchorText(topBar.transform,"MvL","MOVES",14,
                new Color(0.2f,0.9f,1f),new Vector2(0.83f,0.82f));

            // Pause button
            var pbGO = AnchorButton(cv.transform,"PauseBtn","||",
                new Vector2(0.88f,0.92f),Vector2.one,
                new Color(0,0,0,0.6f),28);

            // Objective bar
            var objBar = AnchorPanel(cv.transform,"ObjBar",
                new Color(0,0,0,0.7f),new Vector2(0,0.84f),new Vector2(1,0.88f));
            var obT = AnchorText(objBar.transform,"OT","Score: 0/1000",
                20,Color.white,new Vector2(0.5f,0.5f));

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

            // Booster bar
            var bstBar = AnchorPanel(cv.transform,"BoosterBar",
                new Color(0,0,0,0.90f),Vector2.zero,new Vector2(1,0.10f));
            AnchorText(bstBar.transform,"BstLbl","BOOSTERS",
                14,new Color(0.2f,0.9f,1f),new Vector2(0.5f,0.88f));
            var bstHUD = bstBar.AddComponent<BoosterHUDController>();
            var hB = BoostBtn(bstBar.transform,"H","Hammer\nx0",0, new Color(0.1f,0.3f,0.8f));
            var rB = BoostBtn(bstBar.transform,"R","Row\nx0",  1, new Color(0.9f,0.4f,0.1f));
            var sB = BoostBtn(bstBar.transform,"S","Shuffle\nx0",2,new Color(0.1f,0.7f,0.3f));
            var eB = BoostBtn(bstBar.transform,"E","+Moves\nx0",3, new Color(0.4f,0.1f,0.8f));
            var cB = BoostBtn(bstBar.transform,"C","Color\nx0", 4, new Color(0.8f,0.1f,0.5f));
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
            var s1 = StarImg(winP.transform, new Vector2(0.22f,0.73f));
            var s2 = StarImg(winP.transform, new Vector2(0.50f,0.75f));
            var s3 = StarImg(winP.transform, new Vector2(0.78f,0.73f));
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
            var pAudio = AddComp<MainMenuController>(Child("AudHlp",mgr));
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
            cs.referenceResolution = new Vector2(1080,1920);
            cs.matchWidthOrHeight  = 0.5f;
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

        static GameObject FullPanel(Transform p, string n, Color c)
        {
            var g = new GameObject(n); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = Vector2.zero; r.anchorMax = Vector2.one;
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            g.AddComponent<Image>().color = c;
            return g;
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
            var img = g.AddComponent<Image>(); img.color = c;
            var btn = g.AddComponent<Button>();
            var cb  = btn.colors;
            cb.normalColor = c; cb.highlightedColor = c*1.3f;
            cb.pressedColor = c*0.7f; btn.colors = cb;
            btn.targetGraphic = img;
            var tg = new GameObject("Label"); tg.transform.SetParent(g.transform,false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            var t = tg.AddComponent<Text>();
            t.text = lbl; t.fontSize = sz; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.fontStyle = FontStyle.Bold;
            return g;
        }

        static Image StarImg(Transform p, Vector2 a)
        {
            var g = new GameObject("Star"); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(a.x-0.09f, a.y-0.06f);
            r.anchorMax = new Vector2(a.x+0.09f, a.y+0.06f);
            r.offsetMin = Vector2.zero; r.offsetMax = Vector2.zero;
            var i = g.AddComponent<Image>(); i.color = new Color(0.4f,0.4f,0.4f);
            return i;
        }

        static GameObject BoostBtn(Transform p, string n, string lbl, int idx, Color c)
        {
            float s = 1f/5f;
            float a0 = idx*s+0.004f, a1 = (idx+1)*s-0.004f;
            var g = new GameObject(n); g.transform.SetParent(p, false);
            var r = g.AddComponent<RectTransform>();
            r.anchorMin = new Vector2(a0,0.05f); r.anchorMax = new Vector2(a1,0.92f);
            r.offsetMin = new Vector2(3,3); r.offsetMax = new Vector2(-3,-3);
            var bg2 = new Color(c.r*0.6f,c.g*0.6f,c.b*0.6f);
            var img = g.AddComponent<Image>(); img.color = bg2;
            var btn = g.AddComponent<Button>();
            var cb  = btn.colors; cb.normalColor = bg2; cb.highlightedColor = c;
            cb.pressedColor = bg2*0.6f; btn.colors = cb; btn.targetGraphic = img;
            var tg = new GameObject("Label"); tg.transform.SetParent(g.transform,false);
            var tr = tg.AddComponent<RectTransform>();
            tr.anchorMin = Vector2.zero; tr.anchorMax = Vector2.one;
            tr.offsetMin = Vector2.zero; tr.offsetMax = Vector2.zero;
            var t = tg.AddComponent<Text>();
            t.text = lbl; t.fontSize = 18; t.color = Color.white;
            t.alignment = TextAnchor.MiddleCenter;
            t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            t.lineSpacing = 0.85f;
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
