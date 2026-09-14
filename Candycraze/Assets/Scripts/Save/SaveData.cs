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
        public int Attempts;
        public int Wins;
        public string LastPlayedAtUtc;
    }

    [Serializable]
    public class SaveData
    {
        public int SchemaVersion = 2;
        public int LevelsStarted, LevelsWon, LevelsLost, TotalMoves, BoostersUsed, DailyRewardsClaimed;
        public double PlaySeconds;
        public string UpdatedAtUtc, AppVersion;
        public int LastPlayedLevel = 1;

        public void RecordAttempt(int level)
        {
            LevelsStarted++;
            LastPlayedLevel = level;
            var entry = GetEntry(level);
            if (entry == null) { entry = new LevelSaveEntry { LevelNumber = level }; LevelEntries.Add(entry); }
            entry.Attempts++;
            entry.LastPlayedAtUtc = DateTime.UtcNow.ToString("O");
        }

        public void Normalise()
        {
            SchemaVersion = 2;
            CurrentLevel = Math.Max(3, Math.Min(10001, CurrentLevel));
            LastPlayedLevel = Math.Max(1, Math.Min(10000, LastPlayedLevel));
            LevelsStarted = Math.Max(0, LevelsStarted);
            LevelsWon = Math.Max(0, LevelsWon);
            LevelsLost = Math.Max(0, LevelsLost);
            TotalMoves = Math.Max(0, TotalMoves);
            BoostersUsed = Math.Max(0, BoostersUsed);
            DailyRewardsClaimed = Math.Max(0, DailyRewardsClaimed);
            if (double.IsNaN(PlaySeconds) || double.IsInfinity(PlaySeconds)) PlaySeconds = 0;
            PlaySeconds = Math.Max(0, Math.Min(1e10, PlaySeconds));
            Coins = Math.Max(0, Coins);
            Lives = Math.Max(0, Math.Min(Constants.MAX_LIVES, Lives));
            DailyRewardDay = Math.Max(0, DailyRewardDay) % 7;
            LastDailyRewardTicks = Math.Max(0, Math.Min(DateTime.MaxValue.Ticks, LastDailyRewardTicks));
            LastLifeLostTicks = Math.Max(0, Math.Min(DateTime.MaxValue.Ticks, LastLifeLostTicks));
            BoosterHammer = Math.Max(0, BoosterHammer);
            BoosterRowBlast = Math.Max(0, BoosterRowBlast);
            BoosterShuffle = Math.Max(0, BoosterShuffle);
            BoosterExtraMoves = Math.Max(0, BoosterExtraMoves);
            BoosterColorBlast = Math.Max(0, BoosterColorBlast);
            LevelEntries ??= new List<LevelSaveEntry>();
            var unique = new Dictionary<int, LevelSaveEntry>();
            foreach (var entry in LevelEntries)
            {
                if (entry == null || entry.LevelNumber < 1 || entry.LevelNumber > 10000) continue;
                entry.Attempts = Math.Max(0, entry.Attempts);
                entry.Wins = Math.Max(0, entry.Wins);
                entry.StarsEarned = Math.Max(0, Math.Min(3, entry.StarsEarned));
                entry.HighScore = Math.Max(0, entry.HighScore);
                if (unique.TryGetValue(entry.LevelNumber, out var prior))
                {
                    prior.StarsEarned = Math.Max(prior.StarsEarned, entry.StarsEarned);
                    prior.HighScore = Math.Max(prior.HighScore, entry.HighScore);
                    prior.Completed |= entry.Completed;
                    prior.Attempts = Math.Max(prior.Attempts, entry.Attempts);
                    prior.Wins = Math.Max(prior.Wins, entry.Wins);
                }
                else unique.Add(entry.LevelNumber, entry);
            }
            LevelEntries = new List<LevelSaveEntry>(unique.Values);
            TotalStars = 0;
            foreach (var entry in LevelEntries)
            {
                TotalStars += entry.StarsEarned;
                if (entry.Completed) CurrentLevel = Math.Max(CurrentLevel, entry.LevelNumber + 1);
            }
        }
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
            entry.Wins++;

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
            return levelNumber >= 1 && levelNumber <= CurrentLevel;
        }

        public int GetStars(int levelNumber)
        {
            return GetEntry(levelNumber)?.StarsEarned ?? 0;
        }
    }
}
