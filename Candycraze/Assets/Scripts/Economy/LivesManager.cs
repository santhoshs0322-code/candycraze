// ============================================================
// LivesManager.cs
// Manages the player's lives, regen timer and UI display.
// Implemented fully in Phase 6.  Stub here for compilation.
// ============================================================

using System;
using UnityEngine;
using UnityEngine.Events;

namespace CandyCraze
{
    public class LivesManager : MonoBehaviour
    {
        public static LivesManager Instance { get; private set; }

        public UnityEvent<int> OnLivesChanged = new UnityEvent<int>();

        public int CurrentLives
        {
            get
            {
                RegenerateLives();
                return SaveManager.Instance?.Data.Lives ?? Constants.MAX_LIVES;
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        public bool HasLives() => CurrentLives > 0;

        public void SpendLife()
        {
            if (SaveManager.Instance == null) return;
            var data = SaveManager.Instance.Data;
            if (data.Lives > 0)
            {
                data.Lives--;
                data.LastLifeLostTicks = DateTime.UtcNow.Ticks;
                SaveManager.Instance.Save();
                OnLivesChanged.Invoke(data.Lives);
            }
        }

        public void AddLife(int amount = 1)
        {
            if (SaveManager.Instance == null) return;
            var data = SaveManager.Instance.Data;
            data.Lives = Mathf.Min(data.Lives + amount, Constants.MAX_LIVES);
            SaveManager.Instance.Save();
            OnLivesChanged.Invoke(data.Lives);
        }

        private void RegenerateLives()
        {
            if (SaveManager.Instance == null) return;
            var data = SaveManager.Instance.Data;
            if (data.Lives >= Constants.MAX_LIVES) return;

            long lastLostTicks = data.LastLifeLostTicks;
            if (lastLostTicks == 0) return;

            TimeSpan elapsed = DateTime.UtcNow - new DateTime(lastLostTicks, DateTimeKind.Utc);
            int lifeRegenMinutes = Constants.LIFE_REGEN_MINUTES;

            int livesRegened = (int)(elapsed.TotalMinutes / lifeRegenMinutes);
            if (livesRegened <= 0) return;

            data.Lives = Mathf.Min(data.Lives + livesRegened, Constants.MAX_LIVES);
            // Advance the last-lost time forward by the regenerated amount
            data.LastLifeLostTicks = new DateTime(lastLostTicks, DateTimeKind.Utc)
                .AddMinutes(livesRegened * lifeRegenMinutes).Ticks;
            SaveManager.Instance.Save();
        }
    }
}
