using System;
using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;
using UnityEngine.Scripting;

namespace CandyCraze
{
    public static class IAPProductIDs
    {
        public const string Coins_Small = "coins_small", Coins_Medium = "coins_medium", Coins_Large = "coins_large";
        public static readonly string[] All = { Coins_Small, Coins_Medium, Coins_Large };
    }
    public class IAPManager : MonoBehaviour
    {
        public static IAPManager Instance { get; private set; }
        public event Action OnChanged;
        public bool VerificationInProgress { get; private set; }
        public bool IsBusy { get; private set; }
        public string StatusMessage { get; private set; } = "Sign in to buy crystals.";
        private readonly Dictionary<string, string> prices = new Dictionary<string, string>();
        private const string PendingKey = "play_pending_purchases_v1";
        private PendingList pending;
        private Action<string> onSuccess, onFailure;
        private float retryAt, checkoutDeadline;
        private string lastPlayer;
        [Serializable] private class PendingList { public List<BillingEvent> items = new List<BillingEvent>(); }
        [Serializable] private class BillingEvent { public string type, productId, value, accountId; }
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this; gameObject.name = "IAPManager";
            transform.SetParent(null); DontDestroyOnLoad(gameObject);
            try { pending = JsonUtility.FromJson<PendingList>(PlayerPrefs.GetString(PendingKey, "")); } catch (Exception) { }
            if (pending == null || pending.items == null) pending = new PendingList();
            RestorePurchases();
        }
        public string GetPrice(string id) => prices.TryGetValue(id, out string price) ? price : "Unavailable";
        public bool CanBuy(string id) => prices.ContainsKey(id) && !IsBusy && !VerificationInProgress &&
            CloudSaveManager.Instance != null && CloudSaveManager.Instance.IsSignedIn &&
            !pending.items.Exists(p => p.accountId == AccountHash(CloudSaveManager.Instance.PlayerId));
        public void BuyProduct(string productId, Action<string> onSuccess, Action<string> onFailure = null)
        {
            if (!CanBuy(productId)) { SetStatus("Sign in and restore pending purchases, then retry."); onFailure?.Invoke(StatusMessage); return; }
            this.onSuccess = onSuccess; this.onFailure = onFailure;
            IsBusy = true; checkoutDeadline = Time.realtimeSinceStartup + 180;
            SetStatus("Opening secure Google Play checkout...");
            StartCoroutine(OpenCheckout(productId));
        }
        private IEnumerator OpenCheckout(string id)
        {
            yield return CloudSaveManager.Instance.BillingRequest(id, null, (ok, value) => {
                if (!ok) { Failed(value); return; }
#if UNITY_ANDROID && !UNITY_EDITOR
                try { using (var bridge = new AndroidJavaClass("com.gamixtv.candycraze.billing.CandyBilling")) bridge.CallStatic("purchase", id, value); }
                catch (Exception) { Failed("Google Play checkout is unavailable. Please retry."); }
#else
                Failed("Real purchases require the Android app installed from Google Play.");
#endif
            });
        }
        public void RestorePurchases()
        {
            retryAt = 0;
#if UNITY_ANDROID && !UNITY_EDITOR
            try { using (var bridge = new AndroidJavaClass("com.gamixtv.candycraze.billing.CandyBilling")) bridge.CallStatic("restore"); }
            catch (Exception) { SetStatus("Google Play store unavailable. Please retry."); }
#else
            SetStatus("Purchases require the Android app installed from Google Play.");
#endif
        }
        private static string AccountHash(string id)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes("candycraze:" + id))).Replace("-", "").ToLowerInvariant();
        }
        [Preserve] public void OnBillingEvent(string json)
        {
            BillingEvent item;
            try { item = JsonUtility.FromJson<BillingEvent>(json); } catch (Exception) { return; }
            if (item == null) return;
            switch (item.type)
            {
                case "catalog": prices.Clear(); break;
                case "price": prices[item.productId] = item.value; break;
                case "ready":
                    if (!IsBusy && !VerificationInProgress) SetStatus("Choose a crystal pack. Prices are provided by Google Play.");
                    StartCoroutine(ReportCatalog());
                    break;
                case "disconnected": prices.Clear(); Failed(item.value); break;
                case "error": case "cancelled": case "pending": Failed(item.value); break;
                case "purchase":
                    if (Array.IndexOf(IAPProductIDs.All, item.productId) < 0 || string.IsNullOrEmpty(item.value)) return;
                    if (!pending.items.Exists(p => p.value == item.value)) { pending.items.Add(item); PersistPending(); }
                    IsBusy = false; retryAt = 0; SetStatus("Confirming your purchase...");
                    break;
            }
            OnChanged?.Invoke();
        }
        private void Update()
        {
            var cloud = CloudSaveManager.Instance;
            string player = cloud != null && cloud.IsSignedIn ? cloud.PlayerId : null;
            if (player != lastPlayer) { lastPlayer = player; onSuccess = null; onFailure = null; IsBusy = false; RestorePurchases(); }
            if (IsBusy && Time.realtimeSinceStartup > checkoutDeadline) Failed("Checkout timed out. Restore purchases before trying again.");
            if (player == null || VerificationInProgress || IsBusy || Time.realtimeSinceStartup < retryAt) return;
            string account = AccountHash(player);
            var item = pending.items.Find(p => p.accountId == account);
            if (item != null) StartCoroutine(Verify(item));
        }
        private IEnumerator Verify(BillingEvent item)
        {
            VerificationInProgress = true; SetStatus("Verifying payment and saving crystals...");
            try
            {
                yield return CloudSaveManager.Instance.BillingRequest(item.productId, item.value, (ok, message) => {
                    SetStatus(message);
                    if (!ok) return;
                    pending.items.Remove(item); PersistPending();
                    var callback = onSuccess; onSuccess = null; onFailure = null;
                    callback?.Invoke(item.productId);
                });
            }
            finally { VerificationInProgress = false; retryAt = Time.realtimeSinceStartup + 20; }
            CloudSaveManager.Instance?.UploadCurrentSave(); OnChanged?.Invoke();
        }
        private void PersistPending() { PlayerPrefs.SetString(PendingKey, JsonUtility.ToJson(pending)); PlayerPrefs.Save(); }
        private IEnumerator ReportCatalog()
        {
            var available = new List<string>(); var missing = new List<string>();
            foreach (string id in IAPProductIDs.All) (prices.ContainsKey(id) ? available : missing).Add(id);
            string json = "{\"available\":[\"" + string.Join("\",\"", available) + "\"],\"missing\":[\"" +
                string.Join("\",\"", missing) + "\"]}";
            using (var request = new UnityEngine.Networking.UnityWebRequest(
                "https://candycraze.onrender.com/api/diagnostics/billing-catalog", "POST"))
            {
                request.uploadHandler = new UnityEngine.Networking.UploadHandlerRaw(Encoding.UTF8.GetBytes(json));
                request.downloadHandler = new UnityEngine.Networking.DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json"); request.timeout = 15;
                yield return request.SendWebRequest();
            }
        }
        private void Failed(string message) { IsBusy = false; SetStatus(message); var callback = onFailure; onFailure = null; onSuccess = null; callback?.Invoke(message); }
        private void SetStatus(string message) { StatusMessage = message; OnChanged?.Invoke(); }
        private void OnApplicationFocus(bool focus) { if (focus && !IsBusy) RestorePurchases(); }
    }
}
