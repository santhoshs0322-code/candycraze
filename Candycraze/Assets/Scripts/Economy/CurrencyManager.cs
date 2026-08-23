// ============================================================
// CurrencyManager.cs
// Manages the in-game Crystal currency.
// Stub for Phase 2 — expanded in Phase 6.
// ============================================================

using UnityEngine;
using UnityEngine.Events;

namespace CandyCraze
{
    public class CurrencyManager : MonoBehaviour
    {
        public static CurrencyManager Instance { get; private set; }

        public UnityEvent<int> OnCoinsChanged = new UnityEvent<int>();

        public int Coins => SaveManager.Instance?.Data.Coins ?? 0;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public void AddCoins(int amount)
        {
            if (SaveManager.Instance == null || amount <= 0) return;
            SaveManager.Instance.Data.Coins += amount;
            SaveManager.Instance.Save();
            OnCoinsChanged.Invoke(Coins);
        }

        public bool SpendCoins(int amount)
        {
            if (SaveManager.Instance == null) return false;
            var data = SaveManager.Instance.Data;
            if (data.Coins < amount) return false;
            data.Coins -= amount;
            SaveManager.Instance.Save();
            OnCoinsChanged.Invoke(Coins);
            return true;
        }
    }
}
