// ============================================================
// LevelDataSetup.cs  (EDITOR ONLY)
// CandyCraze → Setup Levels (Detailed)
//
// Overwrites all 20 LevelData assets with rich configurations:
//   - Varied move limits
//   - Mixed objective types (Score, Collect, Clear)
//   - Increasing difficulty
//   - Different spawn weights per level
// ============================================================

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace CandyCraze.Editor
{
    public static class LevelDataSetup
    {
        [MenuItem("CandyCraze/Setup Levels (Detailed Config)")]
        public static void SetupLevels()
        {
            for (int i = 1; i <= 20; i++)
            {
                string path = $"Assets/ScriptableObjects/Levels/LevelData_{i:D2}.asset";
                LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (data == null)
                {
                    Debug.LogWarning($"[LevelDataSetup] Not found: {path}");
                    continue;
                }
                ConfigureLevel(data, i);
                EditorUtility.SetDirty(data);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Levels Configured",
                "All 20 levels have been configured with:\n" +
                "• Varied objectives\n• Increasing difficulty\n• Different spawn weights",
                "Done");
        }

        private static void ConfigureLevel(LevelData d, int n)
        {
            d.LevelNumber = n;
            d.LevelName   = $"Level {n}";
            d.Rows        = 8;
            d.Cols        = 8;

            // ── Spawn Weights ─────────────────────────────────
            // Early levels: fewer colours → easier matches
            // Later levels: all 6 colours → harder
            d.GemSpawnWeights = n switch
            {
                1  => new float[] { 2f, 2f, 2f, 0f, 0f, 0f },  // 3 colours
                2  => new float[] { 2f, 2f, 2f, 0f, 0f, 0f },
                3  => new float[] { 2f, 2f, 2f, 1f, 0f, 0f },  // 4 colours
                4  => new float[] { 2f, 2f, 2f, 1f, 0f, 0f },
                5  => new float[] { 1f, 1f, 1f, 1f, 1f, 0f },  // 5 colours
                6  => new float[] { 1f, 1f, 1f, 1f, 1f, 0f },
                _  => new float[] { 1f, 1f, 1f, 1f, 1f, 1f },  // 6 colours
            };

            // ── Move Limit & Star Thresholds ──────────────────
            switch (n)
            {
                case 1:  d.MoveLimit=30; d.StarThreshold1=1000;  d.StarThreshold2=3000;  d.StarThreshold3=6000;  break;
                case 2:  d.MoveLimit=28; d.StarThreshold1=1200;  d.StarThreshold2=3500;  d.StarThreshold3=7000;  break;
                case 3:  d.MoveLimit=26; d.StarThreshold1=1500;  d.StarThreshold2=4000;  d.StarThreshold3=8000;  break;
                case 4:  d.MoveLimit=25; d.StarThreshold1=2000;  d.StarThreshold2=5000;  d.StarThreshold3=9000;  break;
                case 5:  d.MoveLimit=24; d.StarThreshold1=2500;  d.StarThreshold2=5500;  d.StarThreshold3=10000; break;
                case 6:  d.MoveLimit=22; d.StarThreshold1=3000;  d.StarThreshold2=6000;  d.StarThreshold3=11000; break;
                case 7:  d.MoveLimit=22; d.StarThreshold1=3500;  d.StarThreshold2=7000;  d.StarThreshold3=13000; break;
                case 8:  d.MoveLimit=20; d.StarThreshold1=4000;  d.StarThreshold2=8000;  d.StarThreshold3=14000; break;
                case 9:  d.MoveLimit=20; d.StarThreshold1=4500;  d.StarThreshold2=8500;  d.StarThreshold3=15000; break;
                case 10: d.MoveLimit=18; d.StarThreshold1=5000;  d.StarThreshold2=9000;  d.StarThreshold3=16000; break;
                case 11: d.MoveLimit=18; d.StarThreshold1=5500;  d.StarThreshold2=10000; d.StarThreshold3=17000; break;
                case 12: d.MoveLimit=16; d.StarThreshold1=6000;  d.StarThreshold2=11000; d.StarThreshold3=18000; break;
                case 13: d.MoveLimit=16; d.StarThreshold1=7000;  d.StarThreshold2=12000; d.StarThreshold3=20000; break;
                case 14: d.MoveLimit=15; d.StarThreshold1=8000;  d.StarThreshold2=13000; d.StarThreshold3=21000; break;
                case 15: d.MoveLimit=15; d.StarThreshold1=9000;  d.StarThreshold2=15000; d.StarThreshold3=23000; break;
                case 16: d.MoveLimit=14; d.StarThreshold1=10000; d.StarThreshold2=16000; d.StarThreshold3=25000; break;
                case 17: d.MoveLimit=14; d.StarThreshold1=11000; d.StarThreshold2=17000; d.StarThreshold3=26000; break;
                case 18: d.MoveLimit=12; d.StarThreshold1=12000; d.StarThreshold2=18000; d.StarThreshold3=28000; break;
                case 19: d.MoveLimit=12; d.StarThreshold1=13000; d.StarThreshold2=20000; d.StarThreshold3=30000; break;
                case 20: d.MoveLimit=10; d.StarThreshold1=15000; d.StarThreshold2=22000; d.StarThreshold3=35000; break;
            }

            // ── Objectives ────────────────────────────────────
            d.Objectives = BuildObjectives(n);
        }

        private static ObjectiveData[] BuildObjectives(int n)
        {
            // Levels 1-5: Score only
            if (n <= 5)
                return new[]
                {
                    new ObjectiveData
                    {
                        Type        = ObjectiveType.ReachScore,
                        TargetScore = ScoreTarget(n),
                        Description = $"Score {ScoreTarget(n):N0}"
                    }
                };

            // Levels 6-10: Score + Collect one gem type
            if (n <= 10)
                return new[]
                {
                    new ObjectiveData
                    {
                        Type        = ObjectiveType.ReachScore,
                        TargetScore = ScoreTarget(n),
                        Description = $"Score {ScoreTarget(n):N0}"
                    },
                    new ObjectiveData
                    {
                        Type         = ObjectiveType.CollectGemType,
                        GemTypeID    = (n - 1) % 6,
                        TargetAmount = 10 + n,
                        Description  = $"Collect {10+n} gems"
                    }
                };

            // Levels 11-15: Collect two different gem types
            if (n <= 15)
                return new[]
                {
                    new ObjectiveData
                    {
                        Type         = ObjectiveType.CollectGemType,
                        GemTypeID    = (n) % 6,
                        TargetAmount = 12 + n,
                        Description  = $"Collect {12+n} red gems"
                    },
                    new ObjectiveData
                    {
                        Type         = ObjectiveType.CollectGemType,
                        GemTypeID    = (n + 2) % 6,
                        TargetAmount = 10 + n,
                        Description  = $"Collect {10+n} blue gems"
                    }
                };

            // Levels 16-20: Score + two gem collections
            return new[]
            {
                new ObjectiveData
                {
                    Type        = ObjectiveType.ReachScore,
                    TargetScore = ScoreTarget(n),
                    Description = $"Score {ScoreTarget(n):N0}"
                },
                new ObjectiveData
                {
                    Type         = ObjectiveType.CollectGemType,
                    GemTypeID    = (n) % 6,
                    TargetAmount = 15 + n,
                    Description  = $"Collect {15+n} gems"
                },
                new ObjectiveData
                {
                    Type         = ObjectiveType.CollectGemType,
                    GemTypeID    = (n + 3) % 6,
                    TargetAmount = 12 + n,
                    Description  = $"Collect {12+n} gems"
                }
            };
        }

        private static int ScoreTarget(int n) => n switch
        {
            1 => 1000,  2 => 1200,  3 => 1500,  4 => 2000,  5 => 2500,
            6 => 3000,  7 => 3500,  8 => 4000,  9 => 4500, 10 => 5000,
            11=> 6000, 12=> 7000,  13=> 8000,  14=> 9000,  15=>10000,
            16=>11000, 17=>12000,  18=>13000,  19=>15000,  20=>18000,
            _  => 1000
        };
    }
}

#endif
