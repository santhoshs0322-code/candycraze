// ============================================================
// MassLevelSetup.cs (EDITOR ONLY)
// CandyCraze → Create 100 Levels
// Generates 100 LevelData assets with increasing complexity.
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CandyCraze.Editor
{
    public static class MassLevelSetup
    {
        [MenuItem("CandyCraze/Create 100 Levels")]
        public static void Create100Levels()
        {
            string folder = "Assets/ScriptableObjects/Levels";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects","Levels");

            var levels = new LevelData[100];

            for (int i = 0; i < 100; i++)
            {
                int n = i + 1;
                string path = $"{folder}/LevelData_{n:D3}.asset";

                LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (data == null)
                {
                    data = ScriptableObject.CreateInstance<LevelData>();
                    AssetDatabase.CreateAsset(data, path);
                }

                ConfigLevel(data, n);
                EditorUtility.SetDirty(data);
                levels[i] = data;
            }

            // Update GameConfig
            string cfgPath = "Assets/Resources/GameConfig.asset";
            GameConfig cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(cfgPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(cfg, cfgPath);
            }

            // Keep existing gem definitions
            cfg.Levels = levels;
            EditorUtility.SetDirty(cfg);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("100 Levels Created!",
                "All 100 levels generated with:\n\n" +
                "• Levels 1-10: Tutorial (3 gem types)\n" +
                "• Levels 11-30: Easy (4 gem types)\n" +
                "• Levels 31-60: Medium (5 gem types)\n" +
                "• Levels 61-100: Hard (6 gem types)\n\n" +
                "Each level has unique objectives and scoring.",
                "Awesome!");
        }

        static void ConfigLevel(LevelData d, int n)
        {
            d.LevelNumber = n;
            d.LevelName   = $"Level {n}";
            d.Rows = 8; d.Cols = 8;

            // ── Difficulty tiers ─────────────────────────────
            // Tier 1: 1-10 (Tutorial)
            // Tier 2: 11-30 (Easy)
            // Tier 3: 31-60 (Medium)
            // Tier 4: 61-100 (Hard)

            int tier = n <= 10 ? 1 : n <= 30 ? 2 : n <= 60 ? 3 : 4;

            // Moves — decrease with difficulty
            d.MoveLimit = tier switch {
                1 => Mathf.Max(18, 30 - n),
                2 => Mathf.Max(14, 28 - (n-10)/2),
                3 => Mathf.Max(10, 22 - (n-30)/3),
                4 => Mathf.Max(8,  18 - (n-60)/5),
                _ => 20
            };

            // Score thresholds — scale with level
            int baseScore = 500 + n * 200;
            d.StarThreshold1 = baseScore;
            d.StarThreshold2 = baseScore * 2;
            d.StarThreshold3 = baseScore * 4;

            // Gem spawn weights — introduce more types gradually
            d.GemSpawnWeights = tier switch {
                1 => new float[]{3f,3f,3f,0f,0f,0f},  // 3 types
                2 => new float[]{2f,2f,2f,2f,0f,0f},  // 4 types
                3 => new float[]{1f,1f,1f,1f,1f,0f},  // 5 types
                4 => new float[]{1f,1f,1f,1f,1f,1f},  // 6 types
                _ => new float[]{1f,1f,1f,1f,1f,1f}
            };

            // Objectives — variety based on level number
            d.Objectives = BuildObjectives(n, tier, baseScore);
        }

        static ObjectiveData[] BuildObjectives(int n, int tier, int baseScore)
        {
            // Tier 1 — simple score only
            if (tier == 1)
                return new[] { Score(baseScore, $"Score {baseScore:N0}") };

            // Tier 2 — score + small collect
            if (tier == 2)
            {
                int collect = 8 + (n - 10);
                return new[] {
                    Score(baseScore, $"Score {baseScore:N0}"),
                    Collect(n % 4, collect, $"Collect {collect} gems")
                };
            }

            // Tier 3 — two collects or score + collect
            if (tier == 3)
            {
                if (n % 3 == 0)
                {
                    int c1 = 15 + (n-30)/2, c2 = 12 + (n-30)/3;
                    return new[] {
                        Collect(n%6, c1, $"Collect {c1} gems"),
                        Collect((n+2)%6, c2, $"Collect {c2} crystals")
                    };
                }
                int collect = 20 + (n-30)/2;
                return new[] {
                    Score(baseScore, $"Score {baseScore:N0}"),
                    Collect(n%6, collect, $"Collect {collect} gems")
                };
            }

            // Tier 4 — complex multi-objective
            {
                int c1 = 25 + (n-60)/3, c2 = 20 + (n-60)/4;
                return new[] {
                    Score(baseScore, $"Score {baseScore:N0}"),
                    Collect(n%6, c1, $"Collect {c1} gems"),
                    Collect((n+3)%6, c2, $"Collect {c2} crystals")
                };
            }
        }

        static ObjectiveData Score(int target, string desc) => new ObjectiveData {
            Type=ObjectiveType.ReachScore, TargetScore=target, Description=desc };

        static ObjectiveData Collect(int typeID, int amount, string desc) => new ObjectiveData {
            Type=ObjectiveType.CollectGemType, GemTypeID=typeID%6,
            TargetAmount=amount, Description=desc };
    }
}
#endif
