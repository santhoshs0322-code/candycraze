// ============================================================
// ScoreManager.cs
// Tracks the current score for one level.
// Fires events when the score changes so the HUD can update.
// ============================================================

using UnityEngine;
using UnityEngine.Events;

namespace CandyCraze
{
    public class ScoreManager : MonoBehaviour
    {
        // ── State ────────────────────────────────────────────
        public int CurrentScore { get; private set; }

        // ── Events ───────────────────────────────────────────
        public UnityEvent<int> OnScoreChanged = new UnityEvent<int>();

        // ── Public API ───────────────────────────────────────

        public void Reset()
        {
            CurrentScore = 0;
            OnScoreChanged.Invoke(CurrentScore);
        }

        public void AddScore(int amount)
        {
            if (amount <= 0) return;
            CurrentScore += amount;
            OnScoreChanged.Invoke(CurrentScore);
            Debug.Log($"[ScoreManager] Score: {CurrentScore} (+{amount})");
        }

        /// <summary>Returns how many stars the current score earns for the given level.</summary>
        public int GetStars(LevelData level)
        {
            if (level == null) return 0;
            if (CurrentScore >= level.StarThreshold3) return 3;
            if (CurrentScore >= level.StarThreshold2) return 2;
            if (CurrentScore >= level.StarThreshold1) return 1;
            return 0;
        }
    }
}
