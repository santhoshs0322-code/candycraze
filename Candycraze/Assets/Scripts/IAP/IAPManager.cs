// ============================================================
// IAPManager.cs
// Abstraction layer for in-app purchases.
// Phase 2 — stub implementation.
// Replace internals with Google Play Billing / Unity IAP in Phase 8.
// ============================================================

using System;
using UnityEngine;

namespace CandyCraze
{
    public static class IAPProductIDs
    {
        // These IDs must exactly match the product IDs set up in Google Play Console.
        // They are intentionally left as configurable strings — do NOT hardcode
        // real payment credentials here or commit them to a public repo.
        public const string Coins_Small   = "com.yourcompany.candycraze.coins_small";
        public const string Coins_Medium  = "com.yourcompany.candycraze.coins_medium";
        public const string Coins_Large   = "com.yourcompany.candycraze.coins_large";
        public const string BoosterPack   = "com.yourcompany.candycraze.boosterpack";
    }

    public class IAPManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static IAPManager Instance { get; private set; }

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Initialise();
        }

        private void Initialise()
        {
            // TODO Phase 8: UnityPurchasing.Initialize(this, builder);
            Debug.Log("[IAPManager] Stub initialised.  Real billing: Phase 8.");
        }

        // ── Public API ───────────────────────────────────────

        /// <summary>
        /// Initiates a purchase flow for the given product ID.
        /// <paramref name="onSuccess"/> called with the product ID on success.
        /// <paramref name="onFailure"/> called with a reason string on failure.
        /// </summary>
        public void BuyProduct(string productId,
                               Action<string> onSuccess,
                               Action<string> onFailure = null)
        {
#if UNITY_EDITOR
            Debug.Log($"[IAPManager] Simulating purchase of: {productId}");
            onSuccess?.Invoke(productId);
#else
            // TODO Phase 8: controller.InitiatePurchase(productId);
            Debug.Log($"[IAPManager] IAP not available (stub). Product: {productId}");
            onFailure?.Invoke("IAP not configured.");
#endif
        }

    }
}
