// ============================================================
// ShopManager.cs — Fixed ref parameter issue
// ============================================================

using UnityEngine;
using UnityEngine.Events;

namespace CandyCraze
{
    public class ShopManager : MonoBehaviour
    {
        public static ShopManager Instance { get; private set; }
        public UnityEvent OnPurchaseComplete = new UnityEvent();

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Coin Packs ───────────────────────────────────────
        public void BuyCoinsSmall()  => SimulateBuy(IAPProductIDs.Coins_Small,  500,  "500 Crystals");
        public void BuyCoinsMedium() => SimulateBuy(IAPProductIDs.Coins_Medium, 1200, "1200 Crystals");
        public void BuyCoinsLarge()  => SimulateBuy(IAPProductIDs.Coins_Large,  2800, "2800 Crystals");

        // ── Lives ────────────────────────────────────────────
        public void BuyLives()
        {
            if (SaveManager.Instance == null) return;
            var data = SaveManager.Instance.Data;
            if (data.Coins >= 50)
            {
                data.Coins -= 50;
                data.Lives = Constants.MAX_LIVES;
                SaveManager.Instance.Save();
                OnPurchaseComplete.Invoke();
            }
        }

        // ── Boosters — no ref, direct field access ────────────
        public void BuyHammer()
        {
            if (!CanAfford(30)) return;
            SaveManager.Instance.Data.Coins -= 30;
            SaveManager.Instance.Data.BoosterHammer++;
            Finish("Hammer");
        }

        public void BuyRowBlast()
        {
            if (!CanAfford(40)) return;
            SaveManager.Instance.Data.Coins -= 40;
            SaveManager.Instance.Data.BoosterRowBlast++;
            Finish("Row Blast");
        }

        public void BuyShuffle()
        {
            if (!CanAfford(25)) return;
            SaveManager.Instance.Data.Coins -= 25;
            SaveManager.Instance.Data.BoosterShuffle++;
            Finish("Shuffle");
        }

        public void BuyExtraMoves()
        {
            if (!CanAfford(50)) return;
            SaveManager.Instance.Data.Coins -= 50;
            SaveManager.Instance.Data.BoosterExtraMoves++;
            Finish("Extra Moves");
        }

        public void BuyColorBlast()
        {
            if (!CanAfford(45)) return;
            SaveManager.Instance.Data.Coins -= 45;
            SaveManager.Instance.Data.BoosterColorBlast++;
            Finish("Color Blast");
        }

        // ── Remove Ads ───────────────────────────────────────
        public void BuyRemoveAds()
        {
            IAPManager.Instance?.BuyProduct(
                IAPProductIDs.RemoveAds,
                onSuccess: id =>
                {
                    IAPManager.Instance.SetAdsRemoved();
                    OnPurchaseComplete.Invoke();
                },
                onFailure: err => Debug.Log($"[Shop] Failed: {err}"));
        }

        // ── Watch Ad for coins ───────────────────────────────
        public void WatchAdForCoins()
        {
            AdManager.Instance?.ShowRewardedAd(
                onRewarded: () =>
                {
                    if (SaveManager.Instance != null)
                    {
                        SaveManager.Instance.Data.Coins += 25;
                        SaveManager.Instance.Save();
                        OnPurchaseComplete.Invoke();
                    }
                });
        }

        // ── Private ──────────────────────────────────────────
        private bool CanAfford(int cost)
        {
            if (SaveManager.Instance == null) return false;
            return SaveManager.Instance.Data.Coins >= cost;
        }

        private void Finish(string name)
        {
            SaveManager.Instance.Save();
            Debug.Log($"[Shop] Bought {name}");
            OnPurchaseComplete.Invoke();
        }

        private void SimulateBuy(string productId, int coins, string label)
        {
            IAPManager.Instance?.BuyProduct(
                productId,
                onSuccess: id =>
                {
                    if (SaveManager.Instance != null)
                    {
                        SaveManager.Instance.Data.Coins += coins;
                        SaveManager.Instance.Save();
                        OnPurchaseComplete.Invoke();
                        Debug.Log($"[Shop] {label} purchased.");
                    }
                },
                onFailure: err => Debug.Log($"[Shop] Failed: {err}"));
        }
    }
}
