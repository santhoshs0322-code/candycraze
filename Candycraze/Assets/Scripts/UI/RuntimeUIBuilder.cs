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

        // ════════════════════════════════════════════════════
        // MAIN MENU
        // ────────────────────────────────────────────────────
        // The candy-land home screen is built by CandyHomeMenu,
        // which recreates the reference design as fully interactive
        // Unity UI elements with original, code-generated art.
        // ════════════════════════════════════════════════════
        void BuildMainMenu(Canvas cv)
        {
            var home = gameObject.GetComponent<CandyHomeMenu>() ?? gameObject.AddComponent<CandyHomeMenu>();
            home.Build(cv);
        }

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
            var scrollGO = Pnl("ScrollView", root, V2(0,0),V2(1,1),
                V2(0,botPad), V2(0,-topPad));
            Clr(scrollGO, C_TRANS);
            var sr = scrollGO.AddComponent<ScrollRect>();
            sr.horizontal=false; sr.vertical=true;
            sr.scrollSensitivity=30f; sr.inertia=true;
            sr.decelerationRate=0.135f;
            sr.movementType=ScrollRect.MovementType.Elastic;
            sr.elasticity=0.1f;
            var mskImg=scrollGO.AddComponent<Image>(); mskImg.color=C_TRANS;
            scrollGO.AddComponent<Mask>().showMaskGraphic=false;

            // Viewport
            var vp = Pnl("Viewport", scrollGO.RT(), V2(0,0),V2(1,1),V2(0,0),V2(0,0));
            Clr(vp, C_TRANS);
            sr.viewport = vp.RT();

            // Content
            var ct = new GameObject("Content"); ct.transform.SetParent(vp.transform,false);
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

            // Wire LevelMapController
            var mc = FindObjectOfType<LevelMapController>();
            if (mc != null) mc.InjectRefs(BackButton, LevelContainer, TotalStarsText);
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
            img.color = unlocked ? Hex("#2D1B5E") : Hex("#3A3355");

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
            Canvas c=GetComponent<Canvas>()??gameObject.AddComponent<Canvas>();
            c.renderMode=RenderMode.ScreenSpaceOverlay;
            var cs=GetComponent<CanvasScaler>()??gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode=CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution=new Vector2(1080,2400);
            cs.screenMatchMode=CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            cs.matchWidthOrHeight=0.5f;
            if(!GetComponent<GraphicRaycaster>()) gameObject.AddComponent<GraphicRaycaster>();
            if(!GetComponent<EventSystems.EventSystem>())
            {
                var esGO=new GameObject("EventSystem");
                esGO.AddComponent<EventSystems.EventSystem>();
                esGO.AddComponent<EventSystems.StandaloneInputModule>();
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
        static Button Btn(string n,RectTransform p,Vector2 amin,Vector2 amax,Vector2 apos,Vector2 sd,
            string lbl,float fsz,Color nc,Color pc,Color tc)
        {
            var g=new GameObject(n); g.transform.SetParent(p,false);
            var r=g.AddComponent<RectTransform>();
            r.anchorMin=amin; r.anchorMax=amax; r.anchoredPosition=apos; r.sizeDelta=sd;
            var img=g.AddComponent<Image>(); img.color=nc;
            var btn=g.AddComponent<Button>();
            var cb=btn.colors; cb.normalColor=nc; cb.highlightedColor=nc*1.15f;
            cb.pressedColor=pc; cb.disabledColor=nc.WithAlpha(0.4f);
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
