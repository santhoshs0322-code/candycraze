using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Scripting;

namespace MonsterMerge
{
    [Serializable] public sealed class GoogleUser { public string id,name,email; }
    [Serializable] sealed class GoogleLoginState { public string token,loginId,pollToken; }
    [Serializable] sealed class NativeLoginConfiguration { public string serverClientId; }
    [Serializable] sealed class NativeLoginRequest { public string authorizationCode; }
    [Serializable] sealed class NativeLoginEvent { public int operation; public string code,error; }
    [Serializable] sealed class GoogleLoginReply { public string status,token; public GoogleUser user; }
    [Serializable] sealed class MonsterProfileReply {public bool success;public int revision;public SaveData save;public string error;}
    [Serializable] sealed class MonsterProfileUpload {public int revision;public SaveData save;}

    public sealed class GoogleLoginService : MonoBehaviour
    {
        public event Action Changed;
        public bool IsSignedIn { get; private set; }
        public bool Busy { get; private set; }
        public bool Available { get; private set; }
        public bool Waiting => nativeWaiting;
        bool nativeWaiting;
        int nativeOperation;
        float nativeDeadline;
        public GoogleUser User { get; private set; }
        public string Token => IsSignedIn?state.token:"";
        public string Status { get; private set; }="Sign in with Google before purchasing.";
        GoogleLoginState state=new GoogleLoginState();
        SaveService save;
        string server,storageKey;
        float nextSave;
        bool saving,saveConflict;
        sealed class Reply { public bool ok; public long code; public string body; }

        public void Initialize(SaveService savedGame)
        {
            save=savedGame;
            var asset=Resources.Load<TextAsset>("PaymentConfiguration");
            var config=asset?JsonUtility.FromJson<PaymentConfiguration>(asset.text):null;
            server=(config?.serverUrl??"").TrimEnd('/');
            Available=Uri.TryCreate(server,UriKind.Absolute,out var uri)&&uri.Scheme=="https"&&string.IsNullOrEmpty(uri.UserInfo)&&uri.AbsolutePath=="/"&&string.IsNullOrEmpty(uri.Query)&&string.IsNullOrEmpty(uri.Fragment);
            if(!Available) { Status="Google sign-in is not configured yet."; return; }
            using(var hash=System.Security.Cryptography.SHA256.Create())
                storageKey="MonsterMerge.Google."+BitConverter.ToString(hash.ComputeHash(Encoding.UTF8.GetBytes(server+"/"+config.mode))).Replace("-","");
            try { state=JsonUtility.FromJson<GoogleLoginState>(PlayerPrefs.GetString(storageKey,""))??new GoogleLoginState(); }
            catch { state=new GoogleLoginState(); }
            state.loginId=state.pollToken="";
            Check();
        }
        void Persist() { if(string.IsNullOrEmpty(storageKey))return; PlayerPrefs.SetString(storageKey,JsonUtility.ToJson(state)); PlayerPrefs.Save(); }
        void Notify(string value) { Status=value; Changed?.Invoke(); }
        IEnumerator Request(string path,object body,Reply reply,string token="")
        {
            using(var request=new UnityWebRequest(server+path,body==null?"GET":"POST"))
            {
                request.timeout=20; request.downloadHandler=new DownloadHandlerBuffer();
                if(body!=null) { request.uploadHandler=new UploadHandlerRaw(Encoding.UTF8.GetBytes(JsonUtility.ToJson(body)));request.SetRequestHeader("Content-Type","application/json"); }
                if(!string.IsNullOrEmpty(token))request.SetRequestHeader("Authorization","Bearer "+token);
                yield return request.SendWebRequest();
                reply.ok=request.result==UnityWebRequest.Result.Success;reply.code=request.responseCode;
                reply.body=reply.ok?request.downloadHandler.text:"";
            }
        }
        T Read<T>(Reply reply) where T:class { try {return reply.ok?JsonUtility.FromJson<T>(reply.body):null;}catch{return null;} }
        public void SignIn()
        {
            if(!Available||Busy||IsSignedIn)return;
            StartCoroutine(StartLogin());
        }
        IEnumerator StartLogin()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Busy=true;Notify("Connecting to Google Play Games...");
            int operation=++nativeOperation;
            var reply=new Reply();yield return Request("/auth/monster/config",null,reply);
            var config=Read<NativeLoginConfiguration>(reply);
            if(config==null||string.IsNullOrEmpty(config.serverClientId)||!config.serverClientId.EndsWith(".apps.googleusercontent.com"))
            {Busy=false;Notify("Google Play Games is not configured on the server yet.");yield break;}
            nativeWaiting=true;nativeDeadline=Time.realtimeSinceStartup+100;
            try {using(var bridge=new AndroidJavaClass("com.gamixtv.monstermerge.auth.MonsterSignIn"))
                bridge.CallStatic("signIn",gameObject.name,config.serverClientId,operation);}
            catch(Exception) {nativeWaiting=false;Busy=false;Notify("Google Play Games could not start. Please retry.");}
#else
            Notify("Google Play Games sign-in requires the installed Android game.");
            yield break;
#endif
        }
        [Preserve] public void OnNativeSignIn(string json)
        {
            NativeLoginEvent result;
            try {result=JsonUtility.FromJson<NativeLoginEvent>(json);}catch{return;}
            if(result==null||!nativeWaiting||result.operation!=nativeOperation)return;
            nativeWaiting=false;
            if(string.IsNullOrEmpty(result.code)){Busy=false;Notify(string.IsNullOrEmpty(result.error)?"Sign-in cancelled. Guest play is available.":result.error);return;}
            StartCoroutine(ExchangeNativeCode(result.code));
        }
        IEnumerator ExchangeNativeCode(string code)
        {
            Notify("Restoring your Play Games adventure...");
            var reply=new Reply();yield return Request("/auth/monster/play-games",new NativeLoginRequest{authorizationCode=code},reply);
            var result=Read<GoogleLoginReply>(reply);Busy=false;
            if(result?.status=="signed_in"&&!string.IsNullOrEmpty(result.token))Accept(result.user,result.token);
            else Notify("Play Games sign-in could not be verified. Please retry.");
        }
        public void Check()
        {
            if(!Available||Busy)return;
            if(!string.IsNullOrEmpty(state.token))StartCoroutine(Validate());
        }
        IEnumerator Validate()
        {
            Busy=true;IsSignedIn=false;Notify("Checking your Google session...");
            var reply=new Reply();yield return Request("/auth/monster/me",null,reply,state.token);
            var result=Read<GoogleLoginReply>(reply);Busy=false;
            if(result?.status=="signed_in")Accept(result.user,state.token);
            else if(reply.code==401)Expire();
            else Notify("Connect to the internet to verify Google sign-in before purchasing.");
        }
        void Accept(GoogleUser user,string token)
        {
            if(user==null||string.IsNullOrEmpty(user.id)) { Expire();return; }
            StartCoroutine(RestoreAccount(user,token));
        }
        IEnumerator RestoreAccount(GoogleUser user,string token)
        {
            Busy=true;IsSignedIn=false;Notify("Restoring your dragon collection...");
            var reply=new Reply();yield return Request("/auth/monster/profile",null,reply,token);
            var profile=Read<MonsterProfileReply>(reply);Busy=false;
            if(profile?.success!=true){Notify("Your cloud profile could not load. Retry Google sign-in when online.");yield break;}
            save.RestoreAccount(user.id,profile.save,profile.revision);state.token=token;Persist();User=user;IsSignedIn=true;saveConflict=false;nextSave=Time.unscaledTime+8;
            Notify("Signed in as "+(string.IsNullOrEmpty(user.name)?"Dragon Explorer":user.name));
        }
        IEnumerator UploadProfile()
        {
            saving=true;int generation=save.Generation;
            var snapshot=JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(save.Data));
            var reply=new Reply();yield return Request("/auth/monster/profile",new MonsterProfileUpload{revision=save.CloudRevision,save=snapshot},reply,state.token);
            var response=Read<MonsterProfileReply>(reply);saving=false;nextSave=Time.unscaledTime+15;
            if(response?.success==true)save.AcknowledgeCloud(response.revision,generation);
            else if(reply.code==401)Expire();
            else if(reply.code==409){saveConflict=true;Status="Cloud progress changed. Sign in again to restore; device progress is backed up.";}
        }
        public void Expire() { IsSignedIn=false;User=null;state.token="";Persist();Notify("Your Google session expired. Sign in again to purchase."); }
        public void Cancel() { nativeOperation++;nativeWaiting=false;StopAllCoroutines();Busy=false;saving=false;state.loginId=state.pollToken="";Persist();Notify("Sign-in cancelled. You can still play as a guest."); }
        public void SignOut()
        {
            nativeOperation++;nativeWaiting=false;
            string token=state.token;StopAllCoroutines();state=new GoogleLoginState();Persist();IsSignedIn=false;Busy=false;User=null;
            saving=false;save.UseGuest();Notify("Signed out. Sign in with Google before purchasing.");
            if(!string.IsNullOrEmpty(token))StartCoroutine(Revoke(token));
        }
        IEnumerator Revoke(string token) { var reply=new Reply();yield return Request("/auth/logout",new EmptyPaymentRequest(),reply,token); }
        void Update() { if(nativeWaiting&&Time.realtimeSinceStartup>=nativeDeadline){nativeOperation++;nativeWaiting=false;Busy=false;Notify("Play Games sign-in timed out. Please retry.");}var payments=GetComponent<GooglePlayPayments>();if(IsSignedIn&&!Busy&&!saving&&!saveConflict&&save.CloudDirty&&Time.unscaledTime>=nextSave&&(!payments||(!payments.Busy&&payments.WalletReady)))StartCoroutine(UploadProfile()); }
        void OnApplicationFocus(bool value) { if(value)Check(); }
        void OnApplicationPause(bool paused) { if(!paused)Check(); }
    }
}
