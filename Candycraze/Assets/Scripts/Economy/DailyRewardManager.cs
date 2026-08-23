// ============================================================
// DailyRewardManager.cs
// 7-day rotating daily reward system.
// ============================================================

using System;
using UnityEngine;
using UnityEngine.Events;

namespace CandyCraze
{
    public enum RewardType { Coins, Booster }

    [Serializable]
    public class DailyRewardEntry
    {
        public RewardType Type;
        public int        CoinsAmount;
        public string     BoosterName;
        public string     DisplayText;
    }

    public class DailyRewardManager : MonoBehaviour
    {
        public static DailyRewardManager Instance { get; private set; }

        public UnityEvent<DailyRewardEntry> OnRewardClaimed = new UnityEvent<DailyRewardEntry>();

        // 7-day reward schedule
        private static readonly DailyRewardEntry[] _rewards = new DailyRewardEntry[]
        {
            new DailyRewardEntry { Type=RewardType.Coins,   CoinsAmount=50,  DisplayText="50 Crystals" },
            new DailyRewardEntry { Type=RewardType.Booster, BoosterName="Hammer",     DisplayText="Hammer Booster" },
            new DailyRewardEntry { Type=RewardType.Coins,   CoinsAmount=100, DisplayText="100 Crystals" },
            new DailyRewardEntry { Type=RewardType.Booster, BoosterName="Shuffle",    DisplayText="Shuffle Booster" },
            new DailyRewardEntry { Type=RewardType.Coins,   CoinsAmount=200, DisplayText="200 Crystals" },
            new DailyRewardEntry { Type=RewardType.Booster, BoosterName="ColorBlast", DisplayText="Color Blast Booster" },
            new DailyRewardEntry { Type=RewardType.Coins,   CoinsAmount=500, DisplayText="500 Crystals ⭐" },
        };

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Public API ───────────────────────────────────────

        public bool CanClaimToday()
        {
            if (SaveManager.Instance == null) return false;
            long lastTicks = SaveManager.Instance.Data.LastDailyRewardTicks;
            if (lastTicks == 0) return true;

            DateTime last = new DateTime(lastTicks, DateTimeKind.Utc);
            DateTime now  = DateTime.UtcNow;
            return now.Date > last.Date;
        }

        public DailyRewardEntry GetTodaysReward()
        {
            if (SaveManager.Instance == null) return _rewards[0];
            int day = SaveManager.Instance.Data.DailyRewardDay % _rewards.Length;
            return _rewards[day];
        }

        public int GetCurrentDay()
        {
            if (SaveManager.Instance == null) return 1;
            return (SaveManager.Instance.Data.DailyRewardDay % _rewards.Length) + 1;
        }

        public int GetTotalDays() => _rewards.Length;

        public bool ClaimReward()
        {
            if (!CanClaimToday()) return false;
            if (SaveManager.Instance == null) return false;

            var data   = SaveManager.Instance.Data;
            var reward = GetTodaysReward();

            // Apply reward
            if (reward.Type == RewardType.Coins)
            {
                data.Coins += reward.CoinsAmount;
            }
            else
            {
                switch (reward.BoosterName)
                {
                    case "Hammer":     data.BoosterHammer++;     break;
                    case "Shuffle":    data.BoosterShuffle++;    break;
                    case "ColorBlast": data.BoosterColorBlast++; break;
                    case "RowBlast":   data.BoosterRowBlast++;   break;
                    default:           data.BoosterHammer++;     break;
                }
            }

            data.LastDailyRewardTicks = DateTime.UtcNow.Ticks;
            data.DailyRewardDay       = (data.DailyRewardDay + 1) % _rewards.Length;
            SaveManager.Instance.Save();

            OnRewardClaimed.Invoke(reward);
            Debug.Log($"[DailyReward] Claimed: {reward.DisplayText}");
            return true;
        }

        public TimeSpan TimeUntilNextReward()
        {
            if (SaveManager.Instance == null) return TimeSpan.Zero;
            long ticks = SaveManager.Instance.Data.LastDailyRewardTicks;
            if (ticks == 0) return TimeSpan.Zero;

            DateTime next = new DateTime(ticks, DateTimeKind.Utc).Date.AddDays(1);
            TimeSpan diff = next - DateTime.UtcNow;
            return diff < TimeSpan.Zero ? TimeSpan.Zero : diff;
        }
    }
}
