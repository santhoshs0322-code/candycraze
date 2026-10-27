using UnityEngine;
using UnityEngine.UI;
namespace MonsterMerge
{
    public sealed partial class GameUI
    {
        Text countdownLabel;
        bool advancingToNextLevel;
        public void ShowCountdown(int seconds)
        {
            Page(false);
            Panel("Ready panel",.15f,.33f,.70f,.34f,new Color(1,.98f,.91f,.97f));
            TextAt("LEVEL "+run.DifficultyLevel,.2f,.61f,.60f,.035f,20,Ink);
            TextAt("Spread your wings",.2f,.555f,.60f,.045f,22,Ink);
            countdownLabel=TextAt(seconds.ToString(),.30f,.38f,.40f,.16f,100,Gold);
            TextAt("Hold to move. Drag to steer. Release to stop.",.18f,.345f,.64f,.03f,12,Ink);
        }
        public void UpdateCountdown(float seconds)
        {
            if(!countdownLabel)return;
            countdownLabel.text=Mathf.CeilToInt(seconds).ToString();
            countdownLabel.transform.localScale=Vector3.one*(1+.13f*Mathf.Repeat(seconds,1));
        }
        public void ShowPostLevelUpgrade()
        {
            if(advancingToNextLevel)return;
            var dragon=app.catalog.Monster(app.Save.Data.selected);
            int stage=app.Save.Stage(dragon.id), next=Mathf.Min(4,stage+1),cost=app.Save.UpgradeCost(dragon.id);
            Page(true);Header("YOUR NEXT ADVENTURE",()=>{if(run)ShowResult();else ShowHome();});
            bool blocked=!RunRules.CanPlay(app.Save.Data.level,stage);
            TextAt(blocked?"LEVEL "+app.Save.Data.level+" NEEDS STAGE "+RunRules.RequiredStage(app.Save.Data.level):"LEVEL "+app.Save.Data.level+" AWAITS",.1f,.77f,.8f,.04f,22,Ink);
            var card=Panel("Evolution preview",.07f,.39f,.86f,.35f,new Color(1,.985f,.94f));
            Portrait(card,dragon.id,.02f,.16f,.43f,.72f,Color.white,stage);
            Portrait(card,dragon.id,.55f,.16f,.43f,.72f,Color.white,next);
            TextAt("NOW",.08f,.07f,.30f,.08f,13,Ink,card);
            TextAt(stage<4?"NEXT FORM":"ANCIENT FORM",.57f,.07f,.36f,.08f,13,Ink,card);
            TextAt(">",.46f,.39f,.08f,.18f,28,Gold,card);
            TextAt(dragon.displayName+" • "+DragonProgression.StageName(next),.09f,.33f,.82f,.05f,21,Ink);
            TextAt(DragonProgression.VisualUpgrade(dragon.id,next),.09f,.285f,.82f,.04f,14,Ink);
            TextAt(stage<4?DragonProgression.PowerDescription(dragon,next):"Your dragon has reached its final form. Keep exploring!",.08f,.22f,.84f,.055f,13,Ink);
            if(stage<4)
            {
                bool affordable=app.Save.Data.coins>=cost;
                string owner=app.Save.Data.googleUserId;
                ButtonAt(affordable?"UPGRADE / "+cost.ToString("N0")+" COINS":"GET COINS TO UPGRADE",.10f,.16f,.80f,.055f,()=>
                {
                    if(advancingToNextLevel)return;
                    if(app.Save.Data.coins<cost){ShowInsufficientCoins(cost,ShowPostLevelUpgrade);return;}
                    if(!RequireGoogleLogin(ShowPostLevelUpgrade))return;
                    // A changed account, stage or selection requires reviewing the preview again.
                    if(owner!=app.Save.Data.googleUserId || app.Save.Data.selected!=dragon.id || app.Save.Stage(dragon.id)!=stage || app.Save.UpgradeCost(dragon.id)!=cost)
                    {ShowPostLevelUpgrade();return;}
                    if(!app.Save.TryUpgrade(dragon)){ShowPostLevelUpgrade();Notice("Your coins were not spent. Replay earlier levels to earn more.");return;}
                    app.Audio.Play(app.catalog.evolution);StartNextAdventure();
                },Gold);
            }
            ButtonAt("BUY COINS",.10f,.095f,.38f,.055f,ShowCoinShop,Gold);
            ButtonAt("REPLAY LEVELS",.52f,.095f,.38f,.055f,ShowReplayLevels,Color.white);
            ButtonAt(blocked?"REPLAY & EARN UPGRADE COINS":stage<4?"SKIP & PLAY NEXT LEVEL":"PLAY NEXT LEVEL",.10f,.025f,.80f,.055f,()=>{if(blocked)ShowReplayLevels();else StartNextAdventure();},Mint);
        }
        void PlayLevel(int level)
        {
            if(!app.StartLevel(level))ShowPostLevelUpgrade();
        }
        void ShowReplayLevels() { ShowReplayPage(0); }
        void ShowReplayPage(int pageIndex)
        {
            Page(true);Header("REPLAY & EARN COINS",()=>{if(run)ShowResult();else ShowHome();});
            int last=Mathf.Min(Mathf.Max(1,app.Save.Data.level-1),RunRules.MaxLevel(app.Save.Stage(app.Save.Data.selected)));
            int pages=(last-1)/10+1;pageIndex=Mathf.Clamp(pageIndex,0,pages-1);
            var list=ScrollContent();list.parent.GetComponent<RectTransform>().anchorMin=new Vector2(.05f,.18f);
            for(int level=pageIndex*10+1;level<=Mathf.Min(last,pageIndex*10+10);level++)
            {
                int selectedLevel=level;var row=Row(list,76);
                ButtonAt("PLAY LEVEL "+level+" / "+LevelTheme.For(level).name,.04f,.12f,.92f,.76f,()=>PlayLevel(selectedLevel),Mint,row);
            }
            int current=pageIndex;
            ButtonAt("PREVIOUS",.07f,.09f,.40f,.06f,()=>ShowReplayPage(current-1),Color.white,null,pageIndex>0);
            ButtonAt("NEXT",.53f,.09f,.40f,.06f,()=>ShowReplayPage(current+1),Color.white,null,pageIndex+1<pages);
            TextAt("Earn coins on every completed replay. Your progress is kept.",.07f,.025f,.86f,.045f,12,Ink);
        }
        void StartNextAdventure()
        {
            if(advancingToNextLevel)return;
            if(app.StartLevel(app.Save.Data.level))advancingToNextLevel=true;else ShowPostLevelUpgrade();
        }
    }
}
