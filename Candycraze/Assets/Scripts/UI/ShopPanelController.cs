// ============================================================
// ShopPanelController.cs
// Full shop panel — coin packs, boosters, lives, remove ads.
// ============================================================

using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class ShopPanelController : MonoBehaviour
    {
        [Header("Panel")]
        [SerializeField] private GameObject _panel;

        [Header("Coins Display")]
        [SerializeField] private Text _coinsText;

        [Header("Status Message")]
        [SerializeField] private Text _statusText;

        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnEnable()
        {
            RefreshCoins();
            if (ShopManager.Instance != null)
                ShopManager.Instance.OnPurchaseComplete.AddListener(OnPurchase);
        }

        private void OnDisable()
        {
            if (ShopManager.Instance != null)
                ShopManager.Instance.OnPurchaseComplete.RemoveListener(OnPurchase);
        }

        // ── Public API ───────────────────────────────────────

        public void Show()
        {
            if (_panel != null) _panel.SetActive(true);
            RefreshCoins();
            ShowStatus("");
        }

        public void Hide()
        {
            if (_panel != null) _panel.SetActive(false);
        }

        // ── Button Callbacks ─────────────────────────────────

        public void OnBuyCoinsSmall()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            ShopManager.Instance?.BuyCoinsSmall();
        }

        public void OnBuyCoinsMedium()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            ShopManager.Instance?.BuyCoinsMedium();
        }

        public void OnBuyCoinsLarge()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            ShopManager.Instance?.BuyCoinsLarge();
        }

        public void OnBuyLives()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            if (SaveManager.Instance != null && SaveManager.Instance.Data.Coins < 50)
            {
                ShowStatus("Not enough Crystals! (Need 50)");
                return;
            }
            ShopManager.Instance?.BuyLives();
            ShowStatus("Lives refilled! ♥♥♥♥♥");
        }

        public void OnBuyHammer()     { AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button); ShopManager.Instance?.BuyHammer(); }
        public void OnBuyRowBlast()   { AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button); ShopManager.Instance?.BuyRowBlast(); }
        public void OnBuyShuffle()    { AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button); ShopManager.Instance?.BuyShuffle(); }
        public void OnBuyExtraMoves() { AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button); ShopManager.Instance?.BuyExtraMoves(); }
        public void OnBuyColorBlast() { AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button); ShopManager.Instance?.BuyColorBlast(); }

        // ── Private ──────────────────────────────────────────

        private void OnPurchase()
        {
            RefreshCoins();
            ShowStatus("Purchase successful! ✓");
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Coin);
        }

        private void RefreshCoins()
        {
            if (_coinsText == null || SaveManager.Instance == null) return;
            _coinsText.text = $"✦ {SaveManager.Instance.Data.Coins:N0} Crystals";
        }

        private void ShowStatus(string msg)
        {
            if (_statusText != null) _statusText.text = msg;
        }
    }
}
