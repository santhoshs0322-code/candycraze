using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Scripting;

namespace MonsterMerge
{
    [Serializable] public sealed class PaymentConfiguration
    {
        public bool enabled, allowLocalhostForEditor;
        public string serverUrl="", distribution="google-play", mode="live", provider="google-play";
    }
    [Serializable] public sealed class CoinPack
    {
        public string id, title, localizedPrice;
        public int coins;
        public string Price => string.IsNullOrEmpty(localizedPrice)?"Unavailable":localizedPrice;
    }
    [Serializable] sealed class EmptyPaymentRequest { }
    [Serializable] sealed class PlayRequest { public string productId, purchaseToken; }
    [Serializable] sealed class PlayReply { public bool success, paymentsEnabled; public string accountId, error; public long creditedCoinsTotal; }
    [Serializable] sealed class BillingEvent { public string type, productId, value, accountId; }
    [Serializable] sealed class PendingPurchases { public List<BillingEvent> items=new List<BillingEvent>(); }

    public sealed class GooglePlayPayments : MonoBehaviour
    {
        public event Action Changed;
        public bool Busy { get; private set; }
        public bool Ready { get; private set; }
        public bool Configured { get; private set; }
        public bool WalletReady => walletReady;
        public int CatalogVersion { get; private set; }
        public string Status { get; private set; }="Sign in to buy coins through Google Play.";
        public bool SignedIn => login && login.IsSignedIn;
        public bool HasPending => SignedIn && pending.items.Exists(p=>string.IsNullOrEmpty(p.accountId)||p.accountId==AccountHash(login.User.id));
        public static string SaveProfile => "";
        public CoinPack[] Packs { get; private set; }=new[] {
            new CoinPack{id="starter",title="Starter",coins=500},
            new CoinPack{id="adventurer",title="Adventurer",coins=1500},
            new CoinPack{id="champion",title="Champion",coins=4000},
            new CoinPack{id="legend",title="Legend",coins=10000}
        };
        const string PendingKey="MonsterMerge.GooglePlay.Pending.v1";
        PendingPurchases pending=new PendingPurchases();
        PaymentConfiguration config;
        SaveService save;
        GoogleLoginService login;
        bool walletReady, storeReady, checkout, serverEnabled;
        string player, checkoutPlayer, checkoutProduct;
        float retryAt, checkoutDeadline;
        public void Initialize(SaveService savedGame,GoogleLoginService googleLogin)
        {
            save=savedGame;login=googleLogin;
            var asset=Resources.Load<TextAsset>("PaymentConfiguration");
            try { config=asset?JsonUtility.FromJson<PaymentConfiguration>(asset.text):null; } catch { }
            Configured=config!=null && config.enabled && config.provider=="google-play" && config.distribution=="google-play" && config.mode=="live"
                && Uri.TryCreate(config.serverUrl,UriKind.Absolute,out var uri) && uri.Scheme=="https" && uri.AbsolutePath=="/" && string.IsNullOrEmpty(uri.UserInfo) && string.IsNullOrEmpty(uri.Query) && string.IsNullOrEmpty(uri.Fragment);
            try { pending=JsonUtility.FromJson<PendingPurchases>(PlayerPrefs.GetString(PendingKey,""))??new PendingPurchases(); } catch { }
            if(pending.items==null)pending.items=new List<BillingEvent>();
            login.Changed+=LoginChanged;LoginChanged();
        }
        static string AccountHash(string id)
        {
            using(var sha=System.Security.Cryptography.SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes("monstermerge:"+id))).Replace("-","").ToLowerInvariant();
        }
        void LoginChanged()
        {
            var current=SignedIn?login.User.id:null;
            if(player!=current) { StopAllCoroutines();Busy=false;checkout=false;walletReady=false;serverEnabled=false;player=current;retryAt=0; }
            Recompute();
            if(SignedIn)Refresh();else Notify("Sign in with Google before purchasing.");
        }
        void OnDestroy() { if(login)login.Changed-=LoginChanged; }
        void Recompute() { Ready=Configured && SignedIn && walletReady && serverEnabled && storeReady && !HasPending && !Busy; }
        void Notify(string value) { Status=value;Recompute();Changed?.Invoke(); }
        void Persist() { PlayerPrefs.SetString(PendingKey,JsonUtility.ToJson(pending));PlayerPrefs.Save(); }
        IEnumerator Request(string path,object body,Action<PlayReply> complete)
        {
            string owner=player;
            using(var request=new UnityWebRequest(config.serverUrl.TrimEnd('/')+"/api/billing/"+path,body==null?"GET":"POST"))
            {
                request.downloadHandler=new DownloadHandlerBuffer();request.timeout=20;
                request.SetRequestHeader("Authorization","Bearer "+login.Token);
                if(body!=null) {request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));request.SetRequestHeader("Content-Type","application/json");}
                yield return request.SendWebRequest();
                if(owner!=player || !SignedIn)yield break;
                PlayReply result=null;
                try {result=JsonUtility.FromJson<PlayReply>(request.downloadHandler.text);}catch{}
                if(request.result!=UnityWebRequest.Result.Success)result=new PlayReply{error=result?.error??"Cannot verify purchases. Check your connection and restore purchases."};
                complete(result??new PlayReply{error="Invalid billing response. Please retry."});
            }
        }
        public void Refresh()
        {
            if(!Configured || Busy || !SignedIn)return;
            retryAt=0;
#if UNITY_ANDROID && !UNITY_EDITOR
            try {using(var bridge=new AndroidJavaClass("com.yourstudio.monstermerge.billing.MonsterBilling"))bridge.CallStatic("restore",gameObject.name);}
            catch(Exception) {storeReady=false;Notify("Google Play is unavailable. Please retry.");}
#else
            storeReady=false;Notify("Purchases require the Android app installed from Google Play.");
#endif
            StartCoroutine(Wallet());
        }
        bool Apply(PlayReply reply)
        {
            if(!reply.success || !save.ApplyPaidCredits(reply.accountId,reply.creditedCoinsTotal))return false;
            walletReady=true;return true;
        }
        IEnumerator Wallet()
        {
            Busy=true;Recompute();
            yield return Request("wallet",null,r=> {
                serverEnabled=r.success && r.paymentsEnabled;
                if(!Apply(r)) {walletReady=false;Status=r.error??"Coin balance needs support.";}
                else if(!serverEnabled)Status="Coin purchases are temporarily unavailable. You can still earn coins by playing.";
                else if(storeReady)Status="Choose a coin pack. Prices are provided by Google Play.";
            });
            Busy=false;Recompute();Changed?.Invoke();
        }
        public void Buy(CoinPack confirmed)
        {
            if(!SignedIn) {Ready=false;Notify("Sign in with Google before purchasing.");return;}
            if(!Ready || Busy || confirmed==null)return;
            var current=Array.Find(Packs,p=>p.id==confirmed.id);
            if(current==null || current.coins!=confirmed.coins || current.Price!=confirmed.Price || string.IsNullOrEmpty(current.localizedPrice))
            {Notify("Pack changed. Refresh and confirm again.");return;}
            Busy=true;checkout=true;checkoutPlayer=player;checkoutProduct=current.id;checkoutDeadline=Time.realtimeSinceStartup+180;
            Notify("Opening secure Google Play checkout...");StartCoroutine(Open(current.id));
        }
        IEnumerator Open(string id)
        {
            yield return Request("account",new EmptyPaymentRequest(),r=>{
                if(!r.success || r.accountId!=AccountHash(player)) {Failed(r.error??"Checkout account could not be verified.");return;}
#if UNITY_ANDROID && !UNITY_EDITOR
                try {using(var bridge=new AndroidJavaClass("com.yourstudio.monstermerge.billing.MonsterBilling"))bridge.CallStatic("purchase",id,r.accountId);}
                catch(Exception) {Failed("Google Play checkout unavailable. Restore purchases and retry.");}
#else
                Failed("Purchases require the Android app installed from Google Play.");
#endif
            });
        }
        [Preserve] public void OnBillingEvent(string json)
        {
            BillingEvent item;try {item=JsonUtility.FromJson<BillingEvent>(json);}catch{return;}
            if(item==null)return;
            switch(item.type)
            {
                case "catalog": storeReady=false;for(int i=0;i<Packs.Length;i++)Packs[i]=new CoinPack{id=Packs[i].id,title=Packs[i].title,coins=Packs[i].coins};CatalogVersion++;break;
                case "price":
                    var pack=Array.Find(Packs,p=>p.id==item.productId);
                    if(pack!=null) {int index=Array.IndexOf(Packs,pack);Packs[index]=new CoinPack{id=pack.id,title=pack.title,coins=pack.coins,localizedPrice=item.value};CatalogVersion++;}break;
                case "ready": storeReady=Array.Exists(Packs,p=>!string.IsNullOrEmpty(p.localizedPrice));if(!Busy && serverEnabled)Status=storeReady?"Choose a coin pack. Prices are provided by Google Play.":"No coin packs are available from Google Play for this account yet.";break;
                case "disconnected":storeReady=false;goto case "error";
                case "error": case "cancelled": case "pending":
                    if(checkout) {checkout=false;Busy=false;}Status=item.value;break;
                case "purchase":
                    if(!Array.Exists(Packs,p=>p.id==item.productId)||string.IsNullOrEmpty(item.value))return;
                    if(string.IsNullOrEmpty(item.accountId) && checkout && checkoutPlayer==player && checkoutProduct==item.productId)item.accountId=AccountHash(checkoutPlayer);
                    if(!pending.items.Exists(p=>p.value==item.value)) {pending.items.Add(item);Persist();}
                    if(checkout) {checkout=false;Busy=false;}retryAt=0;Status="Verifying your purchase...";break;
            }
            Recompute();Changed?.Invoke();
        }
        void Update()
        {
            if(checkout && Time.realtimeSinceStartup>checkoutDeadline)Failed("Checkout timed out. Restore purchases before retrying.");
            if(!Configured || !SignedIn || Busy || Time.realtimeSinceStartup<retryAt)return;
            var item=pending.items.Find(p=>string.IsNullOrEmpty(p.accountId)||p.accountId==AccountHash(player));
            if(item!=null)StartCoroutine(Verify(item));
        }
        IEnumerator Verify(BillingEvent item)
        {
            Busy=true;Notify("Verifying payment and saving coins...");
            yield return Request("verify",new PlayRequest{productId=item.productId,purchaseToken=item.value},r=>{
                if(Apply(r)) {pending.items.Remove(item);Persist();Status="Purchase verified. Your coins are ready.";}
                else Status=r.error??"Coin balance could not be restored. Please retry.";
            });
            Busy=false;retryAt=Time.realtimeSinceStartup+20;Recompute();Changed?.Invoke();
        }
        void Failed(string message) {Busy=false;checkout=false;Notify(message);}
        void OnApplicationFocus(bool focus) {if(focus && !Busy)Refresh();}
        void OnApplicationPause(bool paused) {if(!paused && !Busy)Refresh();}
    }
}
