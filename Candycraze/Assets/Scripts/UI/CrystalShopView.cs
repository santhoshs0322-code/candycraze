using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class CrystalShopView : MonoBehaviour
    {
        private Button[] buttons;
        private Text status;
        private float refreshAt;
        private readonly string[] quantities = { "500", "1,200", "2,800" };
        public void Configure(Button[] packs, Text message, Button restore)
        {
            buttons = packs; status = message;
            for (int i = 0; i < buttons.Length; i++)
            {
                string id = IAPProductIDs.All[i];
                buttons[i].onClick.AddListener(() => IAPManager.Instance?.BuyProduct(id, _ => { }));
            }
            restore.onClick.AddListener(() => IAPManager.Instance?.RestorePurchases());
        }
        private void OnEnable() { refreshAt = 0; IAPManager.Instance?.RestorePurchases(); }
        private void Update()
        {
            if (buttons == null || Time.unscaledTime < refreshAt) return;
            refreshAt = Time.unscaledTime + .25f;
            var iap = IAPManager.Instance;
            for (int i = 0; i < buttons.Length; i++)
            {
                buttons[i].interactable = iap != null && iap.CanBuy(IAPProductIDs.All[i]);
                var label = buttons[i].GetComponentInChildren<Text>();
                if (label != null) label.text = quantities[i] + " Crystals  •  " + (iap?.GetPrice(IAPProductIDs.All[i]) ?? "Loading...");
            }
            if (status != null)
                status.text = CloudSaveManager.Instance == null || !CloudSaveManager.Instance.IsSignedIn
                    ? "Sign in from the home screen to buy or restore crystals."
                    : iap?.StatusMessage ?? "Connecting to Google Play...";
        }
    }
}
