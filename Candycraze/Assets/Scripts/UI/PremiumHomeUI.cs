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
        private void Start()
        {
            if (SaveManager.Instance == null) new GameObject("SaveManager").AddComponent<SaveManager>();
            if (GoogleAuthManager.Instance == null) new GameObject("GoogleAuthManager").AddComponent<GoogleAuthManager>();
            if (CloudSaveManager.Instance == null) new GameObject("CloudSaveManager").AddComponent<CloudSaveManager>();
            root = CandyTheme.Page("PremiumHome");
            var purse = CandyTheme.Card(root, "CoinPill", .06f,.927f,.39f,.98f,CandyTheme.Cream);
            coins = CandyTheme.Label(purse.transform,"0 coins",0,0,1,1,32);
            CandyTheme.Label(root,"SWEET ADVENTURES",.42f,.93f,.94f,.977f,26);
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
            daily = CandyTheme.Button(root,"DAILY TREAT",.075f,.304f,.487f,.369f,CandyTheme.Purple,ShowDaily,30);
            settings = CandyTheme.Button(root,"SETTINGS",.513f,.304f,.925f,.369f,CandyTheme.Mint,ShowSettings,30);
            var account = CandyTheme.Card(root,"PlayerCard",.065f,.046f,.935f,.272f,CandyTheme.Cream);
            identity = CandyTheme.Label(account.transform,"GUEST ADVENTURER",.06f,.71f,.94f,.94f,34);
            status = CandyTheme.Label(account.transform,"",.06f,.35f,.94f,.70f,30);
            status.fontStyle = FontStyle.Normal;
            sign = CandyTheme.Button(account.transform,"SIGN IN WITH GOOGLE",.07f,.07f,.93f,.32f,CandyTheme.Purple,AccountAction,30);
            signLabel = sign.GetComponentInChildren<Text>();
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
            var data = SaveManager.Instance.Data; var auth = GoogleAuthManager.Instance;
            bool signed = auth != null && auth.IsAuthenticated(), busy = auth != null && auth.IsBusy;
            coins.text = data.Coins.ToString("N0") + " coins";
            progress.text = "Level " + data.CurrentLevel + " unlocked  •  " + data.TotalStars + " stars";
            identity.text = signed ? "Hello, " + auth.GetUserDisplayName() : "GUEST ADVENTURER";
            status.text = signed ? CloudSaveManager.Instance.StatusMessage : auth.StatusMessage;
            signLabel.text = busy ? "CONNECTING..." : signed ? "SIGN OUT" : "SIGN IN WITH GOOGLE";
            sign.interactable = play.interactable = daily.interactable = settings.interactable = !busy;
        }
        private void CloseDialog() { if (dialog != null) Destroy(dialog); dialog = null; }
        private void AccountAction()
        {
            if (!GoogleAuthManager.Instance.IsAuthenticated()) { GoogleAuthManager.Instance.SignInWithGoogle(); return; }
            var card = CandyTheme.Modal(root,"Sign out?",out dialog);
            CandyTheme.Label(card,"You will return to a fresh guest adventure.\nYour account progress stays saved.\nUnsent progress stays backed up on this phone.",.07f,.39f,.93f,.76f,30);
            CandyTheme.Button(card,"SIGN OUT",.1f,.22f,.9f,.36f,CandyTheme.Pink,() => { GoogleAuthManager.Instance.SignOut(); CloseDialog(); });
            CandyTheme.Button(card,"KEEP PLAYING",.1f,.045f,.9f,.185f,CandyTheme.Purple,CloseDialog);
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
    }
}
