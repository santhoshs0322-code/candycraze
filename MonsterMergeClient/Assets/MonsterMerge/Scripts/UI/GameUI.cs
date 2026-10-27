using System;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
namespace MonsterMerge
{
    public sealed partial class GameUI : MonoBehaviour
    {
        static readonly Color Ink = new Color(.20f,.065f,.27f);
        static readonly Color Mint = new Color(.10f,.79f,.76f);
        static readonly Color Coral = new Color(1,.48f,.57f);
        static readonly Color Gold = new Color(1,.77f,.22f);
        GameApp app;
        GameplayController run;
        RectTransform safe, page, toastRoot;
        Font font, titleFont;
        Texture2D heroTexture;
        Text toast;
        sealed class FloatingNumber { public TextMesh text; public Vector3 origin; public Color color; public float age = 2; }
        FloatingNumber[] floating;
        int numberCursor;
        float toastUntil, toastStarted;
        RunnerInput runnerInput;
        Sprite buttonShape;
        Texture2D buttonTexture;
        RectTransform atmosphere;
        public void Initialize(GameApp game, GameplayController gameplay)
        {
            app = game; run = gameplay;
            if (run) runnerInput = run.Player.GetComponent<RunnerMotor>().Input;
            font = Resources.Load<Font>("Fonts/Lato-Regular") ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            titleFont=Resources.Load<Font>("Fonts/BreeSerif-Regular") ?? font;
            var canvas = new GameObject("Portrait UI",typeof(RectTransform),typeof(Canvas),typeof(CanvasScaler),typeof(GraphicRaycaster));
            canvas.transform.SetParent(transform); canvas.GetComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvas.GetComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(540,960); scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            safe = Rect("Safe area",canvas.transform); safe.gameObject.AddComponent<SafeArea>();
            if (!EventSystem.current) new GameObject("UI input",typeof(EventSystem),typeof(StandaloneInputModule));
            toastRoot = Rect("Toast",safe); Anchor(toastRoot, .06f,run ? .165f : .60f,.94f,run ? .205f : .68f);
            var toastCard=Fill(toastRoot,new Color(1,.96f,.87f,.96f));toastCard.sprite=ButtonShape();toastCard.type=Image.Type.Sliced;
            toast = Label(toastRoot,"",run ? 17 : 26,Color.white); toast.fontStyle = FontStyle.Bold;
            if (run) ShowHud(); else ShowHome();
        }
        RectTransform Rect(string name, Transform parent)
        {
            var obj = new GameObject(name,typeof(RectTransform)); obj.transform.SetParent(parent,false);
            var rect = (RectTransform)obj.transform; Anchor(rect,0,0,1,1); return rect;
        }
        void Anchor(RectTransform rect,float x0,float y0,float x1,float y1)
        { rect.anchorMin = new Vector2(x0,y0); rect.anchorMax = new Vector2(x1,y1); rect.offsetMin = rect.offsetMax = Vector2.zero; }
        Image Fill(RectTransform rect,Color color)
        { var image = rect.gameObject.AddComponent<Image>(); image.color = Surface(color); image.raycastTarget = false; return image; }
        static Color Surface(Color color)
        {
            if(color.a>.8f && Mathf.Max(color.r,color.g,color.b)<.5f)
                return new Color(1,.985f,.94f,color.a);
            return color;
        }
        static Color Lettering(Color color)
        {
            if(color==Gold)return new Color(.53f,.29f,.05f,color.a);
            if(color==Mint)return new Color(.06f,.37f,.31f,color.a);
            if(color==Coral)return new Color(.68f,.16f,.29f,color.a);
            if(color.grayscale>.45f)return new Color(.24f,.16f,.32f,color.a);
            return color;
        }
        RectTransform Panel(string name,float x,float y,float w,float h,Color color,Transform parent=null)
        {
            var rect=Rect(name,parent ? parent : page); Anchor(rect,x,y,x+w,y+h);
            var image=Fill(rect,color); image.sprite=ButtonShape(); image.type=Image.Type.Sliced;
            var edge=rect.gameObject.AddComponent<Outline>(); edge.effectColor=new Color(.23f,.09f,.30f,.55f); edge.effectDistance=new Vector2(2,-2);
            return rect;
        }
        void Emblem(Transform parent,int kind,Color color,float x,float y,float w,float h)
        { var rect=Rect("Emblem",parent); Anchor(rect,x,y,x+w,y+h); var icon=rect.gameObject.AddComponent<MenuEmblem>(); icon.kind=kind; icon.color=color; }
        void Portrait(Transform parent,string id,float x,float y,float w,float h,Color tint,int previewStage=0)
        {
            var monster=app.catalog.Monster(id);
            if(!monster)return;
            var rect=Rect("Cartoon dragon portrait",parent); Anchor(rect,x,y,x+w,y+h);
            var portrait=rect.gameObject.AddComponent<CartoonDragonPortrait>();
            portrait.color=monster.color;portrait.species=id;portrait.stage=previewStage>0?previewStage:app.Save.Stage(id);
        }
        Text Label(RectTransform parent,string value,int size,Color color,TextAnchor alignment = TextAnchor.MiddleCenter)
        {
            var rect = Rect(value,parent); var label = rect.gameObject.AddComponent<Text>();
            label.font = font; label.text = value; label.fontSize = size; label.color = Lettering(color); label.alignment = alignment;
            label.resizeTextForBestFit = true; label.resizeTextMinSize = Mathf.Min(11,size); label.resizeTextMaxSize = size;
            label.raycastTarget = false;
            if(size>=24)StyleTitle(label);
            return label;
        }
        void StyleTitle(Text label)
        {
            label.font=titleFont;label.fontStyle=FontStyle.Normal;label.lineSpacing=.90f;label.color=Color.white;
            label.gameObject.AddComponent<TitleGradient>();
            var edge=label.gameObject.AddComponent<Outline>();edge.effectColor=Ink;edge.effectDistance=new Vector2(2,-2);
            var shadow=label.gameObject.AddComponent<Shadow>();shadow.effectColor=new Color(.31f,.07f,.25f,.65f);shadow.effectDistance=new Vector2(0,-2);
        }
        bool HeroArt(Transform parent,float x,float y,float width,float height)
        {
            if(!heroTexture)heroTexture=Resources.Load<Texture2D>("MenuArt/CinderHero");
            if(!heroTexture)return false;
            var frame=Rect("Illustrated dragon",parent);Anchor(frame,x,y,x+width,y+height);
            var drawing=Rect("Dragon artwork",frame);var image=drawing.gameObject.AddComponent<RawImage>();
            image.texture=heroTexture;image.color=Color.white;image.raycastTarget=false;
            var fit=drawing.gameObject.AddComponent<AspectRatioFitter>();fit.aspectMode=AspectRatioFitter.AspectMode.FitInParent;fit.aspectRatio=(float)heroTexture.width/heroTexture.height;
            return true;
        }
        Text TextAt(string value,float x,float y,float w,float h,int size,Color color,Transform parent = null)
        {
            var rect = Rect(value,parent ? parent : page); Anchor(rect,x,y,x+w,y+h); return Label(rect,value,size,color);
        }
        Button ButtonAt(string title,float x,float y,float w,float h,Action action,Color color,Transform parent = null,bool enabled = true)
        {
            var rect = Rect(title,parent ? parent : page); Anchor(rect,x,y,x+w,y+h);
            bool secondary = color == Color.white;
            var image = Fill(rect,secondary ? new Color(.94f,.995f,1,.98f) : color); image.raycastTarget = true;
            image.sprite = ButtonShape(); image.type = Image.Type.Sliced;
            var shadow = rect.gameObject.AddComponent<Shadow>(); shadow.effectColor = new Color(.24f,.09f,.31f,.65f); shadow.effectDistance = new Vector2(0,-4);
            var rim=Rect("Top light",rect); Anchor(rim,.045f,.94f,.955f,.965f); Fill(rim,new Color(1,1,1,secondary?.17f:.35f));
            var button = rect.gameObject.AddComponent<Button>(); button.targetGraphic = image; button.interactable = enabled;
            var colors = button.colors; colors.disabledColor = new Color(.65f,.65f,.70f,.75f); colors.pressedColor = new Color(.87f,.85f,.96f); button.colors = colors;
            var buttonEdge=rect.gameObject.AddComponent<Outline>();buttonEdge.effectColor=Ink;buttonEdge.effectDistance=new Vector2(1.5f,-1.5f);
            var text = Label(rect,title,18,Ink); text.font=titleFont;text.fontStyle = FontStyle.Normal;
            var textRect = (RectTransform)text.transform; textRect.offsetMin = new Vector2(10,5); textRect.offsetMax = new Vector2(-10,-5);
            button.onClick.AddListener(() => { app.Audio.Play(app.catalog.click); action(); });
            rect.gameObject.AddComponent<ButtonFeedback>(); return button;
        }
        Sprite ButtonShape()
        {
            if (buttonShape) return buttonShape;
            const int size = 48; const float radius = 15;
            buttonTexture = new Texture2D(size,size,TextureFormat.RGBA32,false) { name = "Rounded UI corners", filterMode = FilterMode.Bilinear, wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size*size];
            for (int y = 0; y < size; y++) for (int x = 0; x < size; x++)
            {
                float dx = Mathf.Max(radius - x - .5f, x + .5f - (size-radius), 0);
                float dy = Mathf.Max(radius - y - .5f, y + .5f - (size-radius), 0);
                pixels[y*size+x] = new Color(1,1,1,Mathf.Clamp01(radius - Mathf.Sqrt(dx*dx+dy*dy)));
            }
            buttonTexture.SetPixels(pixels); buttonTexture.Apply();
            buttonShape = Sprite.Create(buttonTexture,new Rect(0,0,size,size),new Vector2(.5f,.5f),100,0,SpriteMeshType.FullRect,new Vector4(16,16,16,16));
            return buttonShape;
        }
        void OnDestroy() { if (buttonShape) Destroy(buttonShape); if (buttonTexture) Destroy(buttonTexture); }
        void Page(bool opaque)
        {
            toastUntil = 0;
            if (page) { page.gameObject.SetActive(false); Destroy(page.gameObject); }
            page = Rect("Screen",safe);
            atmosphere=null;
            viewingCoinShop=false;viewingGoogleLogin=false;googleStatus=null;googleSignIn=null;googleCheck=null;homeAccountLabel=null;
            coinStatus=null; currencyLabel=null; packButtons.Clear(); paymentRefresh=null;
            if (opaque || !run)
            {
                atmosphere=Rect("Living menu background",page);
                atmosphere.gameObject.AddComponent<MenuAtmosphere>().showcase=false;
                page.gameObject.AddComponent<MenuReveal>();
            }
            toastRoot.SetAsLastSibling();
        }
        void Header(string title, Action back)
        {
            TextAt("DRAGON SANCTUARY",.18f,.95f,.64f,.023f,11,Mint);
            TextAt(title,.16f,.885f,.70f,.057f,30,Color.white).fontStyle=FontStyle.Bold;
            ButtonAt("<",.045f,.895f,.095f,.055f,back,Color.white);
            var wallet=Panel("Wallet",.32f,.834f,.36f,.035f,new Color(.13f,.17f,.23f));
            Emblem(wallet,0,Gold,.03f,.05f,.16f,.90f);
            currencyLabel=TextAt(app.Save.Data.coins + " COINS",.2f,.05f,.76f,.90f,13,Gold,wallet);
        }
        public void ShowHome()
        {
            Page(false);
            var coin=Panel("Coin wallet",.055f,.946f,.32f,.038f,new Color(.06f,.09f,.15f,.95f));
            Emblem(coin,0,Gold,.025f,.06f,.18f,.88f); currencyLabel=TextAt(app.Save.Data.coins+" COINS",.22f,0,.74f,1,15,Gold,coin);
            homeAccountLabel=ButtonAt(AccountButtonText(),.395f,.946f,.245f,.038f,()=>ShowGoogleLogin(),Color.white).GetComponentInChildren<Text>();
            var gem=Panel("Gem wallet",.66f,.946f,.285f,.038f,new Color(.06f,.09f,.15f,.95f));
            Emblem(gem,1,Mint,.025f,.06f,.20f,.88f); TextAt(app.Save.Data.gems.ToString(),.24f,0,.7f,1,17,Mint,gem);
            TextAt("MONSTER\nMERGE",.08f,.825f,.84f,.112f,43,Gold);
            var subtitle=TextAt("AI EVOLUTION",.16f,.789f,.68f,.035f,20,Ink);subtitle.font=titleFont;
            TextAt("Explore ? Evolve ? Take Flight",.10f,.757f,.80f,.025f,13,Ink);
            if(app.Save.Data.selected=="dragon")HeroArt(page,.10f,.413f,.80f,.335f);
            else Portrait(page,app.Save.Data.selected,.14f,.413f,.72f,.335f,Color.white);
            var nameplate=Panel("Dragon nameplate",.22f,.358f,.56f,.052f,new Color(.025f,.045f,.085f,.88f));
            TextAt(app.catalog.Monster(app.Save.Data.selected).displayName.ToUpperInvariant(),.06f,.36f,.88f,.56f,21,Color.white,nameplate).fontStyle=FontStyle.Bold;
            TextAt("READY FOR YOUR NEXT ADVENTURE",.06f,.04f,.88f,.30f,9,Mint,nameplate);
            ButtonAt("PLAY  /  LEVEL " + app.Save.Data.level,.08f,.269f,.84f,.073f,() => PlayLevel(app.Save.Data.level),Mint);
            ButtonAt("REPLAY LEVELS / EARN COINS",.12f,.202f,.76f,.055f,ShowReplayLevels,Color.white);
            ButtonAt("DRAGONS",.06f,.167f,.42f,.058f,ShowCollection,Color.white);
            ButtonAt("POWERS",.52f,.167f,.42f,.058f,ShowLab,Color.white);
            ButtonAt("UPGRADES",.06f,.096f,.42f,.058f,ShowUpgrades,Gold);
            ButtonAt("DAILY TREASURE",.52f,.096f,.42f,.058f,ShowDaily,Gold);
            ButtonAt("SHOP",.06f,.025f,.42f,.058f,ShowShop,Color.white);
            ButtonAt("SETTINGS",.52f,.025f,.42f,.058f,ShowSettings,Color.white);
        }
        RectTransform ScrollContent()
        {
            var viewport = Rect("List",page); Anchor(viewport,.05f,.065f,.95f,.815f);
            var background = Fill(viewport,new Color(0,0,0,.01f)); background.raycastTarget = true;
            viewport.gameObject.AddComponent<RectMask2D>();
            var scroll = viewport.gameObject.AddComponent<ScrollRect>(); scroll.viewport = viewport; scroll.horizontal = false;
            var content = Rect("Content",viewport); content.anchorMin = new Vector2(0,1); content.anchorMax = Vector2.one;
            content.pivot = new Vector2(.5f,1); content.sizeDelta = Vector2.zero;
            var layout = content.gameObject.AddComponent<VerticalLayoutGroup>(); layout.spacing = 12;
            layout.childControlHeight = false; layout.childControlWidth = true; layout.childForceExpandHeight = false;
            content.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = content; scroll.movementType = ScrollRect.MovementType.Clamped;
            return content;
        }
        RectTransform Row(RectTransform content,float height)
        {
            var row = Rect("Row",content); row.sizeDelta = new Vector2(0,height);
            var layout = row.gameObject.AddComponent<LayoutElement>(); layout.preferredHeight = height;
            var image=Fill(row,new Color(.085f,.13f,.195f,.98f)); image.sprite=ButtonShape(); image.type=Image.Type.Sliced;
            var rim=Rect("Card accent",row); Anchor(rim,.018f,.14f,.023f,.86f); Fill(rim,new Color(.32f,.69f,.76f,.5f));
            return row;
        }
        void RefreshPreview() { var home = GetComponent<HomeController>(); if (home) home.RefreshPreview(); }
        void Notice(string value) { Toast(value,2.2f); }
        public void ShowDaily()
        {
            Page(true); Header("DAILY TREASURE",ShowHome);
            for (int i = 0; i < 7; i++)
            {
                float y = .73f - i * .075f;
                bool current=i==app.Save.Data.dailyDay;
                var reward=Panel("Day reward",.08f,y-.004f,.84f,.064f,current?new Color(.12f,.27f,.30f):new Color(.075f,.12f,.18f));
                Emblem(reward,0,current?Gold:new Color(.47f,.58f,.69f),.025f,.13f,.12f,.74f);
                TextAt("DAY " + (i + 1),.20f,y,.25f,.05f,18,current ? Mint : Color.white);
                TextAt(app.catalog.dailyRewards[i] + " COINS",.5f,y,.35f,.05f,18,Gold);
            }
            ButtonAt(app.Daily.Available ? "CLAIM " + app.Daily.NextReward + " COINS" : "TREASURE CLAIMED",.1f,.11f,.8f,.07f,() => { app.Daily.Claim(); ShowDaily(); },Mint,null,app.Daily.Available);
        }
        public void ShowSettings()
        {
            Page(true); Header("SETTINGS",ShowHome);
            TextAt("Your adventure, your way",.1f,.77f,.8f,.035f,15,Mint).fontStyle=FontStyle.Bold;
            Panel("Sound card",.07f,.42f,.86f,.31f,new Color(.99f,.96f,.89f));
            Volume("Music",.59f,app.Save.Data.music,value => app.Save.Data.music = value);
            Volume("Sound effects",.46f,app.Save.Data.sfx,value => app.Save.Data.sfx = value);
            Panel("Motion card",.07f,.20f,.86f,.18f,new Color(.99f,.96f,.89f));
            TextAt("Smooth gameplay",.1f,.325f,.8f,.035f,20,Ink).fontStyle=FontStyle.Bold;
            TextAt("Choose the frame rate that feels best",.1f,.295f,.8f,.025f,12,Ink);
            ButtonAt("30 FPS",.11f,.225f,.36f,.055f,() => SetFps(30),app.Save.Data.fps == 30 ? Mint : Color.white);
            ButtonAt("60 FPS",.53f,.225f,.36f,.055f,() => SetFps(60),app.Save.Data.fps == 60 ? Mint : Color.white);
            TextAt("Your changes are saved automatically.",.1f,.15f,.8f,.03f,12,Ink);
            if (Application.isEditor || Debug.isDebugBuild) ButtonAt("DEVELOPMENT",.20f,.07f,.60f,.05f,ShowDebug,Color.white);
        }
        void SetFps(int fps) { app.Save.Data.fps = fps; Application.targetFrameRate = fps; app.Save.Save(); ShowSettings(); }
        void Volume(string title,float y,float value,Action<float> setter)
        {
            TextAt(title,.13f,y+.07f,.53f,.04f,18,Color.white);
            var percentage=TextAt(Mathf.RoundToInt(value*100)+"%",.71f,y+.07f,.16f,.04f,16,Mint);
            var rect = Rect(title,page); Anchor(rect,.15f,y,.85f,y+.035f);
            var bg = Fill(rect,new Color(.025f,.05f,.085f)); bg.raycastTarget = true; bg.sprite=ButtonShape(); bg.type=Image.Type.Sliced;
            var slider = rect.gameObject.AddComponent<Slider>(); slider.minValue = 0; slider.maxValue = 1;
            var fill=Rect("Volume fill",rect); var fillImage=Fill(fill,Mint); fillImage.sprite=ButtonShape(); fillImage.type=Image.Type.Sliced; slider.fillRect=fill;
            var handle = Rect("Handle",rect); handle.sizeDelta = new Vector2(30,14); var handleImage=Fill(handle,Color.white); handleImage.sprite=ButtonShape(); handleImage.type=Image.Type.Sliced; handleImage.raycastTarget = true;
            slider.handleRect = handle; slider.targetGraphic = handle.GetComponent<Image>(); slider.value = value;
            slider.onValueChanged.AddListener(v => { percentage.text=Mathf.RoundToInt(v*100)+"%"; setter(v); app.Audio.SetVolumes(app.Save.Data.music,app.Save.Data.sfx); app.Save.Save(); });
        }
        RunHud runHud;
        public void ShowHud()
        {
            Page(false);
            runHud=page.gameObject.AddComponent<RunHud>(); runHud.Build(app,run,font,ButtonShape());
            ButtonAt("II",.865f,.923f,.10f,.060f,run.TogglePause,Color.white);
        }
        void Update()
        {
            RefreshGoogleUi();
            RefreshPaymentUi();
            if (floating != null) foreach (var number in floating)
            {
                if (number.age >= 1) continue;
                number.age += Time.deltaTime;
                number.text.transform.position = number.origin + Vector3.up * number.age * 1.4f;
                number.text.transform.rotation = Camera.main.transform.rotation;
                var color = number.color; color.a = Mathf.Clamp01((1-number.age)*3); number.text.color = color;
                number.text.gameObject.SetActive(number.age < 1);
            }
            if (toast)
            {
                toastRoot.gameObject.SetActive(Time.unscaledTime < toastUntil);
                toastRoot.localScale = Vector3.one * (1 + .12f * Mathf.Exp(-(Time.unscaledTime-toastStarted)*8));
            }
        }
        public void Toast(string text,float seconds)
        { toast.text = text; toastStarted = Time.unscaledTime; toastUntil = Time.unscaledTime + seconds; toastRoot.SetAsLastSibling(); }
        public void CoinPulse() { if (runHud) runHud.PulseCoin(); }
        public void WorldNumber(Vector3 point, string message, Color color)
        {
            if (floating == null)
            {
                floating = new FloatingNumber[8];
                for (int i=0;i<floating.Length;i++)
                {
                    var obj = new GameObject("Floating combat number"); obj.transform.SetParent(transform);
                    var label = obj.AddComponent<TextMesh>(); label.fontSize = 56; label.characterSize = .06f;
                    label.anchor = TextAnchor.MiddleCenter; label.alignment = TextAlignment.Center;
                    floating[i] = new FloatingNumber {text=label}; obj.SetActive(false);
                }
            }
            var number = floating[numberCursor++%floating.Length]; number.origin = point; number.age = 0;
            number.color = color; number.text.text = message; number.text.color = color;
            number.text.transform.position = point; number.text.gameObject.SetActive(true);
        }
        public void ShowResult()
        {
            Page(false); Fill(page,new Color(.025f,.065f,.09f,.70f)); bool win = run.State == GameState.Victory;
            page.gameObject.AddComponent<MenuReveal>();
            Panel("Result panel",.055f,.075f,.89f,.81f,new Color(.035f,.06f,.10f,.97f));
            Emblem(page,win?3:2,win?Gold:Coral,.43f,.86f,.14f,.065f);
            TextAt(win ? "ADVENTURE COMPLETE!" : "READY FOR ANOTHER RUN?",.08f,.78f,.84f,.09f,40,win ? Mint : Coral);
            TextAt(run.Player.EvolutionName,.08f,.67f,.84f,.065f,28,Color.white);
            TextAt(run.Player.DNA + " DNA",.1f,.59f,.8f,.04f,23,Mint);
            TextAt(win ? "+" + run.RewardCoins * (run.Doubled ? 2 : 1) + " COINS" : "+" + run.RewardCoins + " COINS SAVED",.1f,.49f,.8f,.06f,34,Gold);
            if (!string.IsNullOrEmpty(run.UnlockedMonster)) TextAt(run.UnlockedMonster + " UNLOCKED",.1f,.415f,.8f,.05f,23,Color.white);
            ButtonAt(win ? run.Doubled ? "REWARD DOUBLED" : "2X COINS / REWARDED" : run.IsBoss && !run.BossWardBroken ? "COLLECT 90% TO WIN" : run.Revived ? "REVIVE USED" : "REVIVE / REWARDED",.1f,.31f,.8f,.07f,() => { if (win) run.DoubleCoins(); else run.Revive(); },Gold,null,app.Ads.Available && (win ? !run.Doubled : !run.Revived && (!run.IsBoss || run.BossWardBroken)));
            ButtonAt(win ? "NEXT ADVENTURE" : "TRY AGAIN",.1f,.205f,.8f,.075f,() => {if(win)ShowPostLevelUpgrade();else app.StartLevel(run.DifficultyLevel);},Mint);
            ButtonAt("HOME",.1f,.105f,.8f,.065f,() => app.Navigate("Home"),Color.white);
            if(win)ButtonAt("BUY COINS",.1f,.385f,.8f,.055f,ShowCoinShop,Gold);
        }
        public void ShowPause()
        {
            Page(true);
            Emblem(page,3,Gold,.41f,.79f,.18f,.09f);
            TextAt("Your dragon is taking a breather",.1f,.765f,.8f,.025f,12,Mint);
            TextAt("PAUSED",.1f,.66f,.8f,.09f,42,Color.white).fontStyle=FontStyle.Bold;
            ButtonAt("KEEP EXPLORING",.1f,.48f,.8f,.08f,run.TogglePause,Mint);
            ButtonAt("HOME",.1f,.35f,.8f,.075f,() => app.Navigate("Home"),Color.white);
            if (Application.isEditor || Debug.isDebugBuild) ButtonAt("DEVELOPMENT",.1f,.22f,.8f,.06f,ShowDebug,Gold);
        }
        void ShowDebug()
        {
            if (!Application.isEditor && !Debug.isDebugBuild) return;
            Page(true); Header("DEVELOPMENT",() => { if (run) ShowPause(); else ShowSettings(); }); var list = ScrollContent();
            DebugButton(list,"+1000 COINS",() => { app.Save.Data.coins += 1000; app.Save.Save(); ShowDebug(); });
            DebugButton(list,"UNLOCK ALL",() => { foreach(var m in app.catalog.monsters) if (!app.Save.Data.unlocked.Contains(m.id)) app.Save.Data.unlocked.Add(m.id); app.Save.Save(); Notice("All monsters unlocked"); });
            if (run)
            {
                DebugButton(list,"+25 DNA",() => run.CollectDna(25));
                DebugButton(list,"FORCE EVOLUTION",() => run.Player.Mutate(Element.Fire));
                DebugButton(list,"WIN",() => run.Win());
                DebugButton(list,"LOSE",() => { if (run.State == GameState.Paused) run.TogglePause(); run.Hurt(run.Player.MaxHealth); });
            }
            DebugButton(list,"RESET SAVE",() =>
            {
                Page(true); TextAt("RESET ALL PROGRESS?",.1f,.6f,.8f,.1f,30,Coral);
                ButtonAt("RESET",.1f,.42f,.8f,.07f,() => { app.Save.Reset(); app.Navigate("Home"); },Coral);
                ButtonAt("CANCEL",.1f,.3f,.8f,.07f,ShowDebug,Color.white);
            });
        }
        void DebugButton(RectTransform list,string title,Action action)
        { var row = Row(list,64); ButtonAt(title,0,.05f,1,.9f,action,Mint,row); }
    }
}
