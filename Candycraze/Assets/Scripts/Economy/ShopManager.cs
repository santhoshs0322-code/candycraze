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
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
        }

        // ── Coin Packs ───────────────────────────────────────
        public void BuyCoinsSmall()  => BuyCrystalPack(IAPProductIDs.Coins_Small, "500 Crystals");
        public void BuyCoinsMedium() => BuyCrystalPack(IAPProductIDs.Coins_Medium, "1200 Crystals");
        public void BuyCoinsLarge()  => BuyCrystalPack(IAPProductIDs.Coins_Large, "2800 Crystals");

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
        public void BuyHammer() => BuyBoosters(BoosterType.Hammer, 1);
        public void BuyRowBlast() => BuyBoosters(BoosterType.RowBlast, 1);
        public void BuyShuffle() => BuyBoosters(BoosterType.Shuffle, 1);
        public void BuyExtraMoves() => BuyBoosters(BoosterType.ExtraMoves, 1);
        public void BuyColorBlast() => BuyBoosters(BoosterType.ColorBlast, 1);

        public bool BuyBoosters(BoosterType type, int quantity)
        {
            if (SaveManager.Instance == null || quantity < 1 || quantity > 20 ||
                (int)type < 0 || (int)type >= 5) return false;
            int[] unitCosts = { 30, 40, 25, 50, 45 };
            int cost = unitCosts[(int)type] * quantity;
            if (!CanAfford(cost)) return false;
            var data = SaveManager.Instance.Data;
            data.Coins -= cost;
            switch (type)
            {
                case BoosterType.Hammer: data.BoosterHammer += quantity; break;
                case BoosterType.RowBlast: data.BoosterRowBlast += quantity; break;
                case BoosterType.Shuffle: data.BoosterShuffle += quantity; break;
                case BoosterType.ExtraMoves: data.BoosterExtraMoves += quantity; break;
                case BoosterType.ColorBlast: data.BoosterColorBlast += quantity; break;
                default: return false;
            }
            Finish(quantity + "x " + type);
            return true;
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

        private void BuyCrystalPack(string productId, string label)
        {
            IAPManager.Instance?.BuyProduct(
                productId,
                onSuccess: id =>
                {
                    if (SaveManager.Instance != null)
                    {
                        // The backend has already granted and restored these crystals.
                        OnPurchaseComplete.Invoke();
                        Debug.Log($"[Shop] {label} purchased.");
                    }
                },
                onFailure: err => Debug.Log($"[Shop] Failed: {err}"));
        }
    }
}
