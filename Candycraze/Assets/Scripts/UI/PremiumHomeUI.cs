using UnityEngine;
using UnityEngine.UI;
namespace CandyCraze
{
    public class PremiumHomeUI : MonoBehaviour
    {
        private RectTransform root;
        private Text coins, progress, identity, status, signLabel;
        private Button sign, play, daily, settings;
        private GameObject dialog;
        private float refreshAt;
        private bool shopAfterSignIn;
        private Text shopSignInStatus;
        private Button shopSignInButton;
        private void Start()
        {
            if (SaveManager.Instance == null) new GameObject("SaveManager").AddComponent<SaveManager>();
            if (GoogleAuthManager.Instance == null) new GameObject("GoogleAuthManager").AddComponent<GoogleAuthManager>();
            if (CloudSaveManager.Instance == null) new GameObject("CloudSaveManager").AddComponent<CloudSaveManager>();
            if (IAPManager.Instance == null) new GameObject("IAPManager").AddComponent<IAPManager>();
            root = CandyTheme.Page("PremiumHome");
            var wallet = CandyTheme.Button(root,"",.05f,.916f,.46f,.984f,CandyTheme.Ink,ShowShop);
            CandyTheme.Icon(wallet.transform,CandyGlyph.Kind.Crystal,.035f,.15f,.22f,.85f,Color.white);
            CandyTheme.Label(wallet.transform,"CRYSTALS",.25f,.57f,.78f,.89f,20,CandyTheme.Hex("EADDF5"));
            coins = CandyTheme.Label(wallet.transform,"0",.25f,.07f,.78f,.61f,40,Color.white);
            var add = CandyTheme.Card(wallet.transform,"AddCrystals",.82f,.23f,.96f,.77f,CandyTheme.Pink,false);
            CandyTheme.Label(add.transform,"+",0,0,1,1,32,Color.white);
            sign = CandyTheme.Button(root,"",.62f,.916f,.95f,.984f,CandyTheme.Cream,AccountAction);
            CandyTheme.Icon(sign.transform,CandyGlyph.Kind.Profile,.06f,.16f,.29f,.84f,CandyTheme.Purple);
            signLabel = CandyTheme.Label(sign.transform,"SIGN IN",.33f,.30f,.95f,.80f,28,CandyTheme.Ink);
            CandyTheme.Label(sign.transform,"YOUR PROFILE",.33f,.08f,.95f,.35f,20,CandyTheme.Purple);
            var title = CandyTheme.Label(root,"CANDY",.06f,.79f,.94f,.92f,132,CandyTheme.Cream);
            var shadow = title.gameObject.AddComponent<Shadow>(); shadow.effectDistance = new Vector2(0,-8); shadow.effectColor = CandyTheme.Pink;
            title = CandyTheme.Label(root,"CRAZE",.1f,.685f,.9f,.82f,140,CandyTheme.Pink);
            shadow = title.gameObject.AddComponent<Shadow>(); shadow.effectDistance = new Vector2(0,-7); shadow.effectColor = Color.white;
            CandyTheme.Label(root,"A little match. A lot of magic.",.08f,.644f,.92f,.691f,31);
            var gems = Resources.LoadAll<GemDefinition>("Gems");
            System.Array.Sort(gems, (a,b) => a.GemTypeID.CompareTo(b.GemTypeID));
            for(int i=0; i<6 && gems.Length>0; i++)
            {
                float x=.03f+i*.157f, y=.535f+(i%2)*.018f;
                var gem = CandyTheme.SpriteImage(root,gems[i%gems.Length].CandySprite,x,y,x+.155f,y+.105f);
                gem.transform.localEulerAngles = new Vector3(0,0,(i-2)*-12);
            }
            play = CandyTheme.Button(root,"LET'S PLAY",.10f,.345f,.90f,.44f,CandyTheme.Pink,
                () => SceneController.NavigateTo(Constants.SCENE_LEVEL_MAP),54);
            progress = CandyTheme.Label(root,"Your adventure starts here",.08f,.295f,.92f,.335f,28);
            daily = CandyTheme.Button(root,"DAILY TREAT",.045f,.195f,.345f,.27f,CandyTheme.Purple,ShowDaily,25);
            CandyTheme.Button(root,"SHOP",.36f,.195f,.64f,.27f,CandyTheme.Pink,ShowShop,28);
            settings = CandyTheme.Button(root,"SETTINGS",.655f,.195f,.955f,.27f,CandyTheme.Mint,ShowSettings,25);
            identity = CandyTheme.Label(root,"",.08f,.14f,.92f,.18f,26);
            status = CandyTheme.Label(root,"",.10f,.045f,.90f,.135f,25);
            status.fontStyle = FontStyle.Normal;
            GoogleAuthManager.Instance.OnStateChanged += Refresh;
            SaveManager.Instance.OnDataChanged += Refresh;
            AudioManager.Instance?.PlayMenuMusic();
            Refresh();
        }
        private void Update() { if (Time.unscaledTime > refreshAt) { refreshAt = Time.unscaledTime + 1; Refresh(); } }
        private void OnDestroy()
        {
            if (GoogleAuthManager.Instance != null) GoogleAuthManager.Instance.OnStateChanged -= Refresh;
            if (SaveManager.Instance != null) SaveManager.Instance.OnDataChanged -= Refresh;
        }
        private void Refresh()
        {
            if (coins == null || SaveManager.Instance == null) return;
            if (shopAfterSignIn && GoogleAuthManager.Instance != null)
            {
                var login = GoogleAuthManager.Instance;
                if (login.IsAuthenticated() && CloudSaveManager.Instance != null && CloudSaveManager.Instance.IsSignedIn)
                { ShowShop(); }
                else
                {
                    if (shopSignInButton != null) shopSignInButton.interactable = !login.IsBusy;
                    if (shopSignInStatus != null) shopSignInStatus.text = login.StatusMessage;
                }
            }
            var data = SaveManager.Instance.Data; var auth = GoogleAuthManager.Instance;
            bool signed = auth != null && auth.IsAuthenticated(), busy = auth != null && auth.IsBusy;
            coins.text = data.Coins.ToString("N0");
            bool firstAdventure = data.CurrentLevel == 1 && data.LevelEntries.Count == 0;
            progress.text = firstAdventure ? "Start at level 1. Your story begins here." : "Level " + data.CurrentLevel + "  /  " + data.TotalStars + " stars collected";
            play.GetComponentInChildren<Text>().text = firstAdventure ? "START YOUR ADVENTURE" : "PLAY LEVEL " + data.CurrentLevel;
            identity.text = signed ? "Welcome back, " + auth.GetUserDisplayName() : "";
            status.text = signed ? CloudSaveManager.Instance.StatusMessage : auth.StatusMessage;
            signLabel.text = busy ? "WAIT..." : signed ? "ACCOUNT" : "SIGN IN";
            sign.interactable = play.interactable = daily.interactable = settings.interactable = !busy;
        }
        private void CloseDialog()
        {
            shopAfterSignIn = false;
            shopSignInStatus = null;
            shopSignInButton = null;
            if (dialog != null) Destroy(dialog);
            dialog = null;
        }
        private void AccountAction()
        {
            CloseDialog();
            var auth = GoogleAuthManager.Instance;
            bool signed = auth.IsAuthenticated();
            var card = CandyTheme.Modal(root,signed ? "Your account" : "Save your adventure",out dialog);
            CandyTheme.Label(card,signed ? "Signed in as " + auth.GetUserDisplayName() : "Sign in with your Google Play Games profile.",.07f,.60f,.93f,.76f,32);
            CandyTheme.Label(card,"Choose account signs you out of this game. In Play Games settings > Sign in account, select CandyCraze and an account or Ask each time. Then reopen the game.",.07f,.38f,.93f,.60f,26);
            CandyTheme.Button(card,signed ? "SIGN OUT" : "CONTINUE WITH GOOGLE",.08f,.25f,.92f,.37f,CandyTheme.Purple,() => {
                CloseDialog();
                if (signed) auth.SignOut(); else auth.SignInWithGoogle();
            },28);
            CandyTheme.Button(card,"CHOOSE ACCOUNT",.08f,.12f,.92f,.24f,CandyTheme.Pink,() => { auth.OpenAccountSettings(); CloseDialog(); },28);
            CandyTheme.Button(card,"CLOSE",.08f,.015f,.92f,.11f,CandyTheme.Purple,CloseDialog,24);
        }
        private void ShowDaily()
        {
            if (DailyRewardManager.Instance == null) new GameObject("DailyRewardManager").AddComponent<DailyRewardManager>();
            var manager = DailyRewardManager.Instance;
            var card = CandyTheme.Modal(root,"Daily treat",out dialog);
            bool ready = manager.CanClaimToday();
            CandyTheme.SpriteImage(card,Resources.Load<Sprite>("UI/Star"),.35f,.49f,.65f,.73f);
            CandyTheme.Label(card,ready ? "Day " + manager.GetCurrentDay() + " • " + manager.GetTodaysReward().DisplayText : "Today's treat is claimed.\nCome back tomorrow!",.06f,.30f,.94f,.49f,32);
            var claim = CandyTheme.Button(card,ready ? "CLAIM TREAT" : "CLAIMED",.1f,.17f,.9f,.3f,CandyTheme.Pink,() => { manager.ClaimReward(); CloseDialog(); Refresh(); });
            claim.interactable = ready;
            CandyTheme.Button(card,"CLOSE",.1f,.035f,.9f,.14f,CandyTheme.Purple,CloseDialog,30);
        }
        private void ShowSettings()
        {
            var card = CandyTheme.Modal(root,"Your preferences",out dialog);
            var data = SaveManager.Instance.Data;
            Button sound = null, music = null;
            sound = CandyTheme.Button(card,"SOUND: " + (data.SoundOn?"ON":"OFF"),.1f,.56f,.9f,.70f,CandyTheme.Purple,() => {
                data.SoundOn=!data.SoundOn; AudioManager.Instance?.SetSoundOn(data.SoundOn); SaveManager.Instance.Save();
                sound.GetComponentInChildren<Text>().text="SOUND: "+(data.SoundOn?"ON":"OFF");
            });
            music = CandyTheme.Button(card,"MUSIC: " + (data.MusicOn?"ON":"OFF"),.1f,.38f,.9f,.52f,CandyTheme.Mint,() => {
                data.MusicOn=!data.MusicOn; AudioManager.Instance?.SetMusicOn(data.MusicOn); SaveManager.Instance.Save();
                music.GetComponentInChildren<Text>().text="MUSIC: "+(data.MusicOn?"ON":"OFF");
            });
            CandyTheme.Button(card,"CANDY POWER GUIDE",.1f,.20f,.9f,.34f,CandyTheme.Purple,() => CandyPowerGuide.Show(root),30);
            CandyTheme.Button(card,"ALL SET",.1f,.03f,.9f,.17f,CandyTheme.Pink,CloseDialog);
        }
        private void ShowShop()
        {
            CloseDialog();
            if (GoogleAuthManager.Instance == null || !GoogleAuthManager.Instance.IsAuthenticated() ||
                CloudSaveManager.Instance == null || !CloudSaveManager.Instance.IsSignedIn)
            {
                var prompt = CandyTheme.Modal(root,"Sign in to shop",out dialog);
                CandyTheme.Label(prompt,"Sign in to continue purchasing.\nYour crystals will be saved to your account.",.08f,.49f,.92f,.75f,34);
                shopSignInStatus = CandyTheme.Label(prompt,"",.08f,.32f,.92f,.48f,25);
                shopSignInButton = CandyTheme.Button(prompt,"SIGN IN WITH GOOGLE",.08f,.17f,.92f,.30f,CandyTheme.Purple,() => GoogleAuthManager.Instance?.SignInWithGoogle(),30);
                CandyTheme.Button(prompt,"NOT NOW",.08f,.025f,.92f,.15f,CandyTheme.Pink,CloseDialog,28);
                shopAfterSignIn = true;
                return;
            }
            var card = CandyTheme.Modal(root,"Crystal shop",out dialog);
            CrystalShopView.Build(card,dialog,CloseDialog);
        }
    }
}
