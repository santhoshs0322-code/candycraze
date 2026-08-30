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

        /// <summary>
        /// Stars based on BOTH score and how efficiently moves were used.
        /// - 3 stars: used ≤ 50% of moves (very efficient)
        /// - 2 stars: used ≤ 90% of moves
        /// - 1 star:  completed the level at all
        /// Combined with score thresholds for a final rating.
        /// </summary>
        public int GetStarsWithMoves(LevelData level, int movesUsed, int moveLimit)
        {
            if (level == null || moveLimit <= 0) return GetStars(level);

            float usedPct = movesUsed / (float)moveLimit;

            // Move-efficiency stars
            int moveStars;
            if (usedPct <= 0.5f)      moveStars = 3;   // used half or less
            else if (usedPct <= 0.9f) moveStars = 2;   // used up to 90%
            else                      moveStars = 1;   // used almost all moves

            // Score stars
            int scoreStars = GetStars(level);

            // Final = the higher of the two, but at least 1 for completing
            int finalStars = Mathf.Max(1, Mathf.Max(moveStars, scoreStars));
            return Mathf.Clamp(finalStars, 1, 3);
        }
    }
}
