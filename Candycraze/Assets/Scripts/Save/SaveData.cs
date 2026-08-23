// ============================================================
// SaveData.cs
// Plain C# serialisable class — everything persisted between
// sessions.
//
// CurrentLevel = 3  means levels 1, 2, and 3 are all unlocked
// on a fresh install (IsLevelUnlocked returns true for any
// levelNumber <= CurrentLevel).
// ============================================================

using System;
using System.Collections.Generic;

namespace CandyCraze
{
    [Serializable]
    public class LevelSaveEntry
    {
        public int  LevelNumber;
        public int  StarsEarned;   // 0-3
        public int  HighScore;
        public bool Completed;
    }

    [Serializable]
    public class SaveData
    {
        // ── Progress ──────────────────────────────────────────────
        /// <summary>
        /// Levels 1 through CurrentLevel are all unlocked.
        /// Default = 3  →  first 3 levels available from the start.
        /// </summary>
        public int CurrentLevel  = 3;
        public int TotalStars    = 0;

        public List<LevelSaveEntry> LevelEntries = new List<LevelSaveEntry>();

        // ── Economy ───────────────────────────────────────────────
        public int  Coins           = 0;
        public int  Lives           = Constants.MAX_LIVES;

        /// <summary>UTC ticks of when the last life was spent (for regen timer).</summary>
        public long LastLifeLostTicks = 0;

        // ── Boosters ──────────────────────────────────────────────
        public int BoosterHammer      = 0;
        public int BoosterRowBlast    = 0;
        public int BoosterShuffle     = 0;
        public int BoosterExtraMoves  = 0;
        public int BoosterColorBlast  = 0;

        // ── Daily Reward ──────────────────────────────────────────
        public long LastDailyRewardTicks = 0;
        public int  DailyRewardDay       = 0;   // 0-6 (cycles through 7 days)

        // ── Settings ──────────────────────────────────────────────
        public bool SoundOn = true;
        public bool MusicOn = true;

        // ── Helpers ───────────────────────────────────────────────

        public LevelSaveEntry GetEntry(int levelNumber)
        {
            foreach (var e in LevelEntries)
                if (e.LevelNumber == levelNumber) return e;
            return null;
        }

        public void SetLevelComplete(int levelNumber, int stars, int score)
        {
            var entry = GetEntry(levelNumber);
            if (entry == null)
            {
                entry = new LevelSaveEntry { LevelNumber = levelNumber };
                LevelEntries.Add(entry);
            }

            entry.Completed = true;

            if (stars > entry.StarsEarned)
            {
                TotalStars += stars - entry.StarsEarned;
                entry.StarsEarned = stars;
            }

            if (score > entry.HighScore)
                entry.HighScore = score;

            // Unlock the next level
            if (levelNumber >= CurrentLevel)
                CurrentLevel = levelNumber + 1;
        }

        /// <summary>Returns true when levelNumber &lt;= CurrentLevel.</summary>
        public bool IsLevelUnlocked(int levelNumber)
        {
            return levelNumber <= CurrentLevel;
        }

        public int GetStars(int levelNumber)
        {
            return GetEntry(levelNumber)?.StarsEarned ?? 0;
        }
    }
}
