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
            var purse = CandyTheme.Card(root, "CoinPill", .06f,.927f,.39f,.98f,CandyTheme.Cream);
            coins = CandyTheme.Label(purse.transform,"0 coins",0,0,1,1,32);
            sign = CandyTheme.Button(root,"SIGN IN",.65f,.927f,.94f,.98f,CandyTheme.Purple,AccountAction,28);
            signLabel = sign.GetComponentInChildren<Text>();
            signLabel.rectTransform.anchorMin = new Vector2(.27f,.1f);
            // A simple profile silhouette stays legible without an external icon font.
            CandyTheme.SpriteImage(sign.transform,CandyTheme.Circle,.09f,.52f,.24f,.83f).color = Color.white;
            CandyTheme.SpriteImage(sign.transform,CandyTheme.Circle,.055f,.17f,.275f,.54f).color = Color.white;
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
            play = CandyTheme.Button(root,"LET'S PLAY",.14f,.428f,.86f,.515f,CandyTheme.Pink,
                () => SceneController.NavigateTo(Constants.SCENE_LEVEL_MAP),54);
            progress = CandyTheme.Label(root,"Your adventure starts here",.08f,.38f,.92f,.425f,28);
            daily = CandyTheme.Button(root,"DAILY TREAT",.045f,.304f,.345f,.369f,CandyTheme.Purple,ShowDaily,25);
            CandyTheme.Button(root,"SHOP",.36f,.304f,.64f,.369f,CandyTheme.Pink,ShowShop,28);
            settings = CandyTheme.Button(root,"SETTINGS",.655f,.304f,.955f,.369f,CandyTheme.Mint,ShowSettings,25);
            var account = CandyTheme.Card(root,"PlayerCard",.065f,.046f,.935f,.272f,CandyTheme.Cream);
            identity = CandyTheme.Label(account.transform,"GUEST ADVENTURER",.06f,.71f,.94f,.94f,34);
            status = CandyTheme.Label(account.transform,"",.06f,.16f,.94f,.65f,30);
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
            coins.text = data.Coins.ToString("N0") + " crystals";
            progress.text = "Level " + data.CurrentLevel + " unlocked  •  " + data.TotalStars + " stars";
            identity.text = signed ? "Hello, " + auth.GetUserDisplayName() : "GUEST ADVENTURER";
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
