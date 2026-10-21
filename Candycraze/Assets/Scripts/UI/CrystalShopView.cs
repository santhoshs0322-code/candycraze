using System;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class CrystalShopView : MonoBehaviour
    {
        private Button[] buttons;
        private Text status, balance;
        private Text[] priceLabels, balanceLabels;
        private float refreshAt;
        private readonly string[] quantities = { "500", "1,200", "2,800" };
        public static CrystalShopView Build(RectTransform card, GameObject overlay, Action close, Action<string> purchased = null)
        {
            if (CloudSaveManager.Instance == null || !CloudSaveManager.Instance.IsSignedIn)
            {
                CandyTheme.Label(card,"Sign in to continue purchasing.\nGo to Home to sign in and restore your account before buying crystals.",.08f,.38f,.92f,.74f,32);
                CandyTheme.Button(card,"SIGN IN ON HOME",.08f,.20f,.92f,.34f,CandyTheme.Purple,
                    () => SceneController.NavigateTo(Constants.SCENE_MAIN_MENU),30);
                CandyTheme.Button(card,"NOT NOW",.08f,.035f,.92f,.17f,CandyTheme.Pink,() => close(),28);
                return null;
            }
            card.anchorMin = new Vector2(.06f,.16f);
            card.anchorMax = new Vector2(.94f,.84f);
            var balanceCard = CandyTheme.Card(card,"CrystalBalance",.07f,.67f,.93f,.77f,CandyTheme.Ink,false);
            CandyTheme.Label(balanceCard.transform,"YOUR BALANCE",.04f,.53f,.96f,.95f,24,Color.white);
            var balanceText = CandyTheme.Label(balanceCard.transform,"",.04f,.04f,.96f,.58f,40,Color.white);
            var packs = new Button[3];
            var prices = new Text[3];
            var totals = new Text[3];
            int[] amounts = { 500, 1200, 2800 };
            for (int i = 0; i < packs.Length; i++)
            {
                float top = .65f - i * .15f;
                packs[i] = CandyTheme.Button(card,"",.07f,top-.13f,.93f,top,CandyTheme.Purple,() => { });
                CandyTheme.Label(packs[i].transform,amounts[i].ToString("N0") + " CRYSTALS",.04f,.43f,.62f,.92f,34,Color.white);
                prices[i] = CandyTheme.Label(packs[i].transform,"Loading...",.63f,.43f,.96f,.92f,34,CandyTheme.Gold);
                totals[i] = CandyTheme.Label(packs[i].transform,"",.04f,.06f,.96f,.40f,25,Color.white);
            }
            var message = CandyTheme.Label(card,"",.07f,.13f,.93f,.215f,25);
            var restore = CandyTheme.Button(card,"RESTORE",.07f,.025f,.49f,.12f,CandyTheme.Purple,() => { },28);
            CandyTheme.Button(card,"CLOSE",.51f,.025f,.93f,.12f,CandyTheme.Pink,() => close(),28);
            var view = overlay.AddComponent<CrystalShopView>();
            view.balance = balanceText;
            view.priceLabels = prices;
            view.balanceLabels = totals;
            view.Configure(packs,message,restore,purchased);
            return view;
        }
        public void Configure(Button[] packs, Text message, Button restore, Action<string> purchased = null)
        {
            buttons = packs; status = message;
            for (int i = 0; i < buttons.Length; i++)
            {
                string id = IAPProductIDs.All[i];
                buttons[i].onClick.AddListener(() => IAPManager.Instance?.BuyProduct(id, purchased ?? (_ => { })));
            }
            restore.onClick.AddListener(() => IAPManager.Instance?.RestorePurchases());
        }
        private void OnEnable() { refreshAt = 0; IAPManager.Instance?.RestorePurchases(); }
        private void Update()
        {
            if (buttons == null || Time.unscaledTime < refreshAt) return;
            refreshAt = Time.unscaledTime + .25f;
            var iap = IAPManager.Instance;
            int crystals = SaveManager.Instance != null ? SaveManager.Instance.Data.Coins : 0;
            if (balance != null) balance.text = crystals.ToString("N0") + " crystals";
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].interactable = iap != null && iap.CanBuy(IAPProductIDs.All[i]);
                var label = buttons[i].GetComponentInChildren<Text>();
                if (priceLabels != null)
                {
                    priceLabels[i].text = iap?.GetPrice(IAPProductIDs.All[i]) ?? "Loading...";
                    int amount = i == 0 ? 500 : i == 1 ? 1200 : 2800;
                    balanceLabels[i].text = "Balance after purchase: " + ((long)crystals + amount).ToString("N0") + " crystals";
                }
                else if (label != null) label.text = quantities[i] + " Crystals - " + (iap?.GetPrice(IAPProductIDs.All[i]) ?? "Loading...");
            }
            if (status != null)
                status.text = CloudSaveManager.Instance == null || !CloudSaveManager.Instance.IsSignedIn
                    ? "Sign in from the home screen to buy or restore crystals."
                    : iap?.StatusMessage ?? "Connecting to Google Play...";
        }
    }
}
