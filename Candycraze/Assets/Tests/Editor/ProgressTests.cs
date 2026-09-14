using NUnit.Framework;
using UnityEngine;
using CandyCraze;

public class ProgressTests
{
    [Test] public void FreshProfileUsesDefaultProgress()
    {
        var data = new SaveData();
        Assert.AreEqual(3, data.CurrentLevel);
        Assert.AreEqual(0, data.LevelEntries.Count);
        Assert.AreEqual(0, data.Coins);
        Assert.IsFalse(data.IsLevelUnlocked(0));
        Assert.IsFalse(data.IsLevelUnlocked(4));
    }
    [Test] public void ReplayingLevelNeverDuplicatesItsRecordOrStars()
    {
        var data = new SaveData();
        data.SetLevelComplete(3, 3, 1000);
        data.SetLevelComplete(3, 1, 500);
        Assert.AreEqual(1, data.LevelEntries.Count);
        Assert.AreEqual(3, data.TotalStars);
        Assert.AreEqual(1000, data.GetEntry(3).HighScore);
        Assert.AreEqual(4, data.CurrentLevel);
    }
    [Test] public void FullProfileRoundTripsThroughUnityJson()
    {
        var data = new SaveData { Coins = 125, BoosterHammer = 2, SoundOn = false, TotalMoves = 45, PlaySeconds = 123.5 };
        data.RecordAttempt(4); data.SetLevelComplete(4, 2, 800);
        var restored = JsonUtility.FromJson<SaveData>(JsonUtility.ToJson(data));
        restored.Normalise();
        Assert.AreEqual(5, restored.CurrentLevel);
        Assert.AreEqual(125, restored.Coins);
        Assert.AreEqual(2, restored.BoosterHammer);
        Assert.IsFalse(restored.SoundOn);
        Assert.AreEqual(45, restored.TotalMoves);
        Assert.AreEqual(123.5, restored.PlaySeconds);
        Assert.AreEqual(1, restored.GetEntry(4).Attempts);
    }
    [Test] public void OldDuplicateLevelsNormaliseWithoutLosingBestResult()
    {
        var data = new SaveData { TotalStars = 999, Coins = -10, DailyRewardDay = -1 };
        data.LevelEntries.Add(new LevelSaveEntry { LevelNumber = 8, Completed = true, StarsEarned = 2, HighScore = 200 });
        data.LevelEntries.Add(new LevelSaveEntry { LevelNumber = 8, StarsEarned = 3, HighScore = 300 });
        data.LevelEntries.Add(null);
        data.Normalise();
        Assert.AreEqual(1, data.LevelEntries.Count);
        Assert.AreEqual(3, data.TotalStars);
        Assert.AreEqual(300, data.GetEntry(8).HighScore);
        Assert.AreEqual(9, data.CurrentLevel);
        Assert.AreEqual(0, data.Coins);
        Assert.AreEqual(0, data.DailyRewardDay);
    }
}
