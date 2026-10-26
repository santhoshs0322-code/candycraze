using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace MonsterMerge
{
    public sealed partial class GameUI
    {
        Text coinStatus,currencyLabel;
        Button paymentRefresh;
        readonly List<Button> packButtons=new List<Button>();
        string purchaseNotice="";
        bool viewingCoinShop;
        int coinCatalogVersion;
        void RefreshPaymentUi()
        {
            if(currencyLabel) currencyLabel.text=app.Save.Data.coins.ToString("N0")+" COINS";
            if(!app.Payments) return;
            if(viewingCoinShop && !app.Payments.Busy && coinCatalogVersion!=app.Payments.CatalogVersion) { ShowCoinShop(); return; }
            if(coinStatus) coinStatus.text=app.Payments.Status;
            for(int i=0;i<packButtons.Count;i++) if(packButtons[i]) packButtons[i].interactable=app.Payments.Ready && !app.Payments.Busy && i<app.Payments.Packs.Length && !string.IsNullOrEmpty(app.Payments.Packs[i].localizedPrice);
            if(paymentRefresh) paymentRefresh.interactable=app.Payments.Configured && !app.Payments.Busy;
        }
        public void ShowCollection()
        {
            Page(true); Header("DRAGONS",ShowHome);
            TextAt("Meet 8 dragons. Discover 4 powerful stages.",.08f,.795f,.84f,.024f,11,Mint);
            var list=ScrollContent(); list.parent.GetComponent<RectTransform>().anchorMax=new Vector2(.95f,.775f);
            int rank=0;
            foreach(var definition in DragonProgression.Ordered(app.catalog))
            {
                var dragon=definition; rank++;
                bool owned=app.Save.Data.unlocked.Contains(dragon.id);
                int stage=app.Save.Stage(dragon.id);
                var row=Row(list,205);
                Portrait(row,dragon.id,.025f,.24f,.27f,.69f,Color.white);
                TextAt(rank.ToString("00"),.035f,.035f,.10f,.15f,12,Gold,row);
                TextAt(dragon.displayName.ToUpperInvariant(),.31f,.75f,.64f,.17f,20,Color.white,row).fontStyle=FontStyle.Bold;
                TextAt(DragonProgression.Species(dragon.id),.31f,.64f,.64f,.10f,10,new Color(.38f,.30f,.43f),row);
                TextAt(DragonProgression.PowerName(dragon),.31f,.49f,.64f,.14f,14,Mint,row);
                TextAt(owned?"OWNED  /  STAGE "+stage+" OF 4":dragon.unlockCost.ToString("N0")+" COINS",.31f,.35f,.64f,.15f,15,Gold,row);
                ButtonAt(owned?"MEET & EVOLVE":"MEET DRAGON",.31f,.075f,.64f,.21f,()=>{purchaseNotice=""; ShowDragonDetails(dragon);},owned?Mint:Gold,row);
            }
        }
        public void ShowUpgrades() { purchaseNotice=""; ShowDragonDetails(app.catalog.Monster(app.Save.Data.selected)); }
        public void ShowLab() { purchaseNotice=""; ShowDragonDetails(app.catalog.Monster(app.Save.Data.selected)); }
        public void ShowDragonDetails(MonsterDefinition dragon,int previewStage=0)
        {
            Page(true); Header(dragon.displayName.ToUpperInvariant(),ShowCollection);
            bool owned=app.Save.Data.unlocked.Contains(dragon.id);
            int stage=app.Save.Stage(dragon.id);
            int shown=previewStage>0?previewStage:stage;
            var hero=Panel("Dragon power card",.065f,.54f,.87f,.255f,new Color(.075f,.12f,.19f));
            Portrait(hero,dragon.id,.005f,.06f,.40f,.88f,Color.white,shown);
            TextAt(DragonProgression.Species(dragon.id),.40f,.81f,.56f,.105f,10,Gold,hero);
            TextAt(DragonProgression.PowerName(dragon),.40f,.62f,.56f,.16f,20,Mint,hero).fontStyle=FontStyle.Bold;
            TextAt(DragonProgression.PowerDescription(dragon,shown),.40f,.20f,.56f,.38f,14,Color.white,hero);
            TextAt("PREVIEW "+shown+"  /  ATK "+Mathf.RoundToInt(dragon.baseAttack*DragonProgression.AttackMultiplier(shown))+"  HP "+Mathf.RoundToInt(dragon.baseHealth*DragonProgression.HealthMultiplier(shown)),.40f,.055f,.56f,.10f,11,Gold,hero);
            TextAt("Choose a stage to see your dragon grow",.07f,.49f,.86f,.027f,11,Gold);
            for(int i=1;i<=4;i++)
            {
                bool reached=owned && i<=stage;
                var tile=Panel("Stage "+i,.065f+(i-1)*.22f,.389f,.205f,.085f,reached?new Color(.10f,.29f,.30f):new Color(.08f,.12f,.18f));
                int targetStage=i;var hit=tile.gameObject.AddComponent<Button>();hit.targetGraphic=tile.GetComponent<Image>();hit.targetGraphic.raycastTarget=true;hit.onClick.AddListener(()=>ShowDragonDetails(dragon,targetStage));
                if(i==shown)tile.GetComponent<Image>().color=new Color(1,.86f,.49f);
                TextAt(i.ToString(),.1f,.61f,.8f,.28f,22,reached?Mint:Color.white,tile).fontStyle=FontStyle.Bold;
                TextAt(DragonProgression.StageName(i),.025f,.32f,.95f,.23f,10,reached?Mint:new Color(.6f,.69f,.79f),tile);
                TextAt(i==1?"START":DragonProgression.StageCost(dragon,i-1).ToString("N0")+" C",.025f,.07f,.95f,.19f,9,Gold,tile);
            }
            int nextCost=app.Save.UpgradeCost(dragon.id);
            string details=!owned?"Begin your journey together as a Hatchling.":stage>=4?"Ancient form reached. Your dragon is at full strength.":"NEXT: "+DragonProgression.PowerDescription(dragon,stage+1);
            TextAt(details,.085f,.303f,.83f,.07f,14,Color.white);
            if(!owned)
                ButtonAt("BUY / "+dragon.unlockCost.ToString("N0")+" COINS",.09f,.22f,.82f,.065f,()=>ConfirmDragonPurchase(dragon,false),Gold);
            else
                ButtonAt(stage>=4?"MAX STAGE":"UPGRADE / "+nextCost.ToString("N0")+" COINS",.09f,.22f,.82f,.065f,()=>ConfirmDragonPurchase(dragon,true),Gold,null,stage<4);
            ButtonAt(owned?(app.Save.Data.selected==dragon.id?"YOUR COMPANION":"CHOOSE COMPANION"):"GET COINS",.09f,.135f,.82f,.060f,()=>
            {
                if(!owned) { ShowCoinShop(); return; }
                app.Save.Data.selected=dragon.id; app.Save.Save(); RefreshPreview(); ShowDragonDetails(dragon);
            },Mint,null,!owned || app.Save.Data.selected!=dragon.id);
            TextAt(purchaseNotice,.07f,.062f,.86f,.048f,13,Mint);
            TextAt("PREVIEW: "+DragonProgression.VisualUpgrade(dragon.id,shown)+"  /  "+(owned?"OWNED STAGE "+stage:"NOT OWNED"),.055f,.018f,.89f,.035f,10,new Color(.63f,.75f,.85f));
        }
        void ConfirmDragonPurchase(MonsterDefinition dragon,bool upgrade)
        {
            if(!RequireGoogleLogin(()=>ConfirmDragonPurchase(dragon,upgrade)))return;
            int cost=upgrade?app.Save.UpgradeCost(dragon.id):dragon.unlockCost;
            if(cost>app.Save.Data.coins)
            {
                purchaseNotice="You need "+(cost-app.Save.Data.coins).ToString("N0")+" more coins.";
                ShowDragonDetails(dragon); return;
            }
            Page(true); Header("CONFIRM PURCHASE",()=>ShowDragonDetails(dragon));
            Portrait(page,dragon.id,.30f,.60f,.40f,.19f,Color.white);
            TextAt(upgrade?"UPGRADE "+dragon.displayName: "UNLOCK "+dragon.displayName,.07f,.515f,.86f,.065f,26,Color.white).fontStyle=FontStyle.Bold;
            TextAt(upgrade?"Stage "+app.Save.Stage(dragon.id)+"  >  Stage "+(app.Save.Stage(dragon.id)+1):"Stage 1 - "+DragonProgression.StageName(1),.1f,.457f,.8f,.035f,17,Mint);
            TextAt(cost.ToString("N0")+" COINS",.1f,.37f,.8f,.065f,32,Gold);
            TextAt("Balance after purchase: "+(app.Save.Data.coins-cost).ToString("N0")+" coins",.08f,.315f,.84f,.035f,15,Color.white);
            ButtonAt("CONFIRM / "+cost.ToString("N0")+" COINS",.1f,.215f,.8f,.07f,()=>
            {
                if(!RequireGoogleLogin(()=>ConfirmDragonPurchase(dragon,upgrade)))return;
                bool success=upgrade?app.Save.TryUpgrade(dragon):app.Save.TryUnlock(dragon);
                purchaseNotice=success?(upgrade?"A new form, a stronger companion!":"Your new dragon is ready. Choose it to begin!"):"Not enough coins for this upgrade. Your balance is unchanged.";
                if(success) { app.Audio.Play(app.catalog.evolution); RefreshPreview(); }
                ShowDragonDetails(dragon);
            },Gold);
            ButtonAt("CANCEL",.1f,.115f,.8f,.06f,()=>ShowDragonDetails(dragon),Color.white);
        }
        public void ShowShop()
        {
            Page(true); Header("SHOP",ShowHome);
            var coins=Panel("Get coins",.07f,.50f,.86f,.275f,new Color(.10f,.15f,.22f));
            Emblem(coins,0,Gold,.035f,.34f,.24f,.55f);
            TextAt("FUEL YOUR ADVENTURE",.30f,.69f,.64f,.16f,24,Gold,coins).fontStyle=FontStyle.Bold;
            TextAt("Earn coins as you explore, or choose a coin pack.\nCheckout is handled by Google Play.",.30f,.35f,.64f,.29f,14,Color.white,coins);
            ButtonAt("COIN PACKS",.06f,.08f,.88f,.21f,ShowCoinShop,Gold,coins);
            var dragons=Panel("Choose a dragon",.07f,.18f,.86f,.275f,new Color(.075f,.15f,.20f));
            Emblem(dragons,3,Mint,.035f,.34f,.24f,.55f);
            TextAt("FIND YOUR COMPANION",.30f,.69f,.64f,.16f,20,Mint,dragons).fontStyle=FontStyle.Bold;
            TextAt("Eight dragons. Eight unique powers.\nFind your favorite and help it grow.",.30f,.35f,.64f,.29f,14,Color.white,dragons);
            ButtonAt("BROWSE DRAGONS",.06f,.08f,.88f,.21f,ShowCollection,Mint,dragons);
            TextAt("Choose your next adventure. Confirm before spending coins.",.07f,.095f,.86f,.045f,13,new Color(.63f,.74f,.84f));
        }
        public void ShowCoinShop()
        {
            if(!RequireGoogleLogin(ShowCoinShop))return;
            Page(true); Header("GET COINS",()=>{if(run && run.State==GameState.Victory)ShowPostLevelUpgrade();else ShowShop();});
            var payments=app.Payments;
            viewingCoinShop=true; coinCatalogVersion=payments.CatalogVersion;
            coinStatus=TextAt(payments.Status,.075f,.728f,.85f,.072f,13,Mint);
            var list=ScrollContent(); list.parent.GetComponent<RectTransform>().anchorMin=new Vector2(.05f,.27f); list.parent.GetComponent<RectTransform>().anchorMax=new Vector2(.95f,.715f);
            foreach(var pack in payments.Packs)
            {
                var selected=pack;
                var row=Row(list,115);
                Emblem(row,0,Gold,.04f,.26f,.15f,.53f);
                TextAt(pack.coins.ToString("N0")+" COINS",.22f,.59f,.72f,.24f,23,Gold,row).fontStyle=FontStyle.Bold;
                TextAt(pack.title,.22f,.39f,.72f,.16f,12,new Color(.60f,.72f,.83f),row);
                packButtons.Add(ButtonAt("BUY / "+pack.Price,.23f,.08f,.71f,.26f,()=>ConfirmCoinPack(selected),Gold,row,payments.Ready && !payments.Busy && !string.IsNullOrEmpty(pack.localizedPrice)));
            }
            paymentRefresh=ButtonAt("RESTORE PURCHASES",.08f,.185f,.84f,.052f,payments.Refresh,Mint,null,payments.Configured && !payments.Busy);
            TextAt("Your coins arrive after purchase verification.\nSign in with Google to keep purchases linked to you.",.07f,.033f,.86f,.06f,11,new Color(.59f,.70f,.80f));
        }
        void ConfirmCoinPack(CoinPack pack)
        {
            if(!RequireGoogleLogin(()=>ConfirmCoinPack(pack)))return;
            Page(true); Header("CONFIRM COIN PACK",ShowCoinShop);
            Emblem(page,0,Gold,.36f,.62f,.28f,.16f);
            TextAt(pack.coins.ToString("N0")+" COINS",.1f,.51f,.8f,.07f,35,Gold).fontStyle=FontStyle.Bold;
            TextAt(pack.Price,.1f,.425f,.8f,.06f,28,Color.white);
            TextAt("One-time purchase through Google Play",.08f,.36f,.84f,.04f,13,Mint);
            TextAt("For "+app.GoogleLogin.User.name+"\n"+pack.coins.ToString("N0")+" coins added after verified payment",.08f,.29f,.84f,.055f,12,Color.white);
            ButtonAt("CONTINUE TO GOOGLE PLAY / "+pack.Price,.1f,.205f,.8f,.075f,()=>{app.Payments.Buy(pack); ShowCoinShop();},Gold,null,app.Payments.Ready && !app.Payments.Busy);
            ButtonAt("CANCEL",.1f,.13f,.8f,.06f,ShowCoinShop,Color.white);
            TextAt("Purchase still pending? Choose Restore purchases.\nPlease wait for verification before buying again.",.075f,.032f,.85f,.07f,12,new Color(.59f,.70f,.80f));
        }
    }
}
