// ============================================================
// AdManager.cs
// Abstraction layer for advertisements.
// Phase 2 — stub implementation (no real SDK).
// Replace the stub internals with the real AdMob/Unity Ads
// SDK calls in Phase 8.  The public interface will not change.
// ============================================================

using System;
using UnityEngine;

namespace CandyCraze
{
    public class AdManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static AdManager Instance { get; private set; }

        // ── Config (set via GameConfig or remote config later)
        [Header("Ad Config (fill in Phase 8)")]
        [Tooltip("AdMob App ID — leave empty until Phase 8.")]
        [SerializeField] private string _appId               = "";
        [SerializeField] private string _rewardedAdUnitId    = "";
        [SerializeField] private string _interstitialAdUnitId = "";

        // Suppress CS0414 — fields used in Phase 8 SDK calls
        #pragma warning disable 0414

        [Tooltip("Simulate ads in the editor without real SDK.")]
        [SerializeField] private bool _simulateAdsInEditor = true;
        #pragma warning restore 0414

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

        // ── Initialisation ───────────────────────────────────

        private void Initialise()
        {
            // TODO Phase 8: MobileAds.Initialize(_appId);
            Debug.Log("[AdManager] Stub initialised.  Real SDK: Phase 8.");
        }

        // ── Public API ───────────────────────────────────────

        /// <summary>
        /// Shows a rewarded advertisement.
        /// <paramref name="onRewarded"/> is called if the user earns the reward.
        /// <paramref name="onFailed"/> is called if the ad is unavailable.
        /// </summary>
        public void ShowRewardedAd(Action onRewarded, Action onFailed = null)
        {
#if UNITY_EDITOR
            if (_simulateAdsInEditor)
            {
                Debug.Log("[AdManager] Simulating rewarded ad — reward granted.");
                onRewarded?.Invoke();
                return;
            }
#endif
            // TODO Phase 8: Load and show real rewarded ad.
            Debug.Log("[AdManager] Rewarded ad not available (stub).");
            onFailed?.Invoke();
        }

        /// <summary>
        /// Shows an interstitial advertisement at a natural break point.
        /// Does not award rewards.
        /// </summary>
        public void ShowInterstitial(Action onClosed = null)
        {
#if UNITY_EDITOR
            if (_simulateAdsInEditor)
            {
                Debug.Log("[AdManager] Simulating interstitial ad.");
                onClosed?.Invoke();
                return;
            }
#endif
            // TODO Phase 8: Load and show real interstitial.
            Debug.Log("[AdManager] Interstitial not available (stub).");
            onClosed?.Invoke();
        }

        /// <summary>Returns true if a rewarded ad is loaded and ready.</summary>
        public bool IsRewardedAdReady()
        {
#if UNITY_EDITOR
            return _simulateAdsInEditor;
#else
            // TODO Phase 8: return real ad readiness.
            return false;
#endif
        }
    }
}
