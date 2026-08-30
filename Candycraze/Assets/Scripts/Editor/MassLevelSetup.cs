// ============================================================
// MassLevelSetup.cs (EDITOR ONLY)
// CandyCraze → Create 100 / 1000 Levels
// Generates LevelData assets with a scaling difficulty curve and
// wires them into GameConfig.Levels.
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CandyCraze.Editor
{
    public static class MassLevelSetup
    {
        [MenuItem("CandyCraze/Create 100 Levels")]
        public static void Create100Levels() => CreateLevels(100);

        [MenuItem("CandyCraze/Create 1000 Levels")]
        public static void Create1000Levels() => CreateLevels(1000);

        // ────────────────────────────────────────────────────
        // Generate `count` LevelData assets and wire them into GameConfig.
        // ────────────────────────────────────────────────────
        static void CreateLevels(int count)
        {
            string folder = "Assets/ScriptableObjects/Levels";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/ScriptableObjects","Levels");

            // Remove legacy 3-digit level files (LevelData_001.asset …) so
            // they don't linger as orphans beside the new 4-digit assets.
            for (int old = 1; old <= 100; old++)
            {
                string oldPath = $"{folder}/LevelData_{old:D3}.asset";
                if (AssetDatabase.LoadAssetAtPath<LevelData>(oldPath) != null)
                    AssetDatabase.DeleteAsset(oldPath);
            }

            var levels = new LevelData[count];

            // Batch asset edits for speed with large counts
            AssetDatabase.StartAssetEditing();
            try
            {
                for (int i = 0; i < count; i++)
                {
                    int n = i + 1;
                    // 4-digit padding keeps 1000+ levels sorted correctly
                    string path = $"{folder}/LevelData_{n:D4}.asset";

                    LevelData data = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                    if (data == null)
                    {
                        data = ScriptableObject.CreateInstance<LevelData>();
                        AssetDatabase.CreateAsset(data, path);
                    }

                    ConfigLevel(data, n);
                    EditorUtility.SetDirty(data);
                    levels[i] = data;

                    if (i % 50 == 0)
                        EditorUtility.DisplayProgressBar("Creating Levels",
                            $"Level {n} / {count}", n / (float)count);
                }
            }
            finally
            {
                AssetDatabase.StopAssetEditing();
                EditorUtility.ClearProgressBar();
            }

            // Update GameConfig
            string cfgPath = "Assets/Resources/GameConfig.asset";
            GameConfig cfg = AssetDatabase.LoadAssetAtPath<GameConfig>(cfgPath);
            if (cfg == null)
            {
                cfg = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(cfg, cfgPath);
            }

            // Keep existing gem definitions; replace the level list
            cfg.Levels = levels;
            EditorUtility.SetDirty(cfg);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog($"{count} Levels Created!",
                $"All {count} levels generated with a scaling difficulty curve:\n\n" +
                "• Tutorial → Easy → Medium → Hard → Expert\n" +
                "• More gem types, tighter moves, and multi-objective\n" +
                "  goals as the level number climbs.\n\n" +
                "GameConfig.Levels now holds every level.",
                "Awesome!");
        }

        static void ConfigLevel(LevelData d, int n)
        {
            d.LevelNumber = n;
            d.LevelName   = $"Level {n}";
            d.Rows = 8; d.Cols = 8;

            // ── Difficulty tiers (scale across 1000 levels) ──
            // Tier 1: 1-10       Tutorial   (3 gem types)
            // Tier 2: 11-30      Easy       (4 gem types)
            // Tier 3: 31-60      Medium     (5 gem types)
            // Tier 4: 61-150     Hard       (6 gem types)
            // Tier 5: 151+       Expert     (6 gem types, tight moves)
            int tier = n <= 10 ? 1 : n <= 30 ? 2 : n <= 60 ? 3 : n <= 150 ? 4 : 5;

            // Moves — decrease with difficulty, clamped to a fair floor
            d.MoveLimit = tier switch {
                1 => Mathf.Max(18, 30 - n),
                2 => Mathf.Max(14, 28 - (n-10)/2),
                3 => Mathf.Max(12, 22 - (n-30)/3),
                4 => Mathf.Max(10, 20 - (n-60)/9),
                5 => Mathf.Max(8,  16 - (n-150)/120),   // very gentle taper for 151..1000
                _ => 20
            };

            // Score thresholds — scale with level (mild sqrt-ish growth so
            // late levels stay achievable rather than exploding linearly).
            int baseScore = 500 + n * 150 + Mathf.RoundToInt(Mathf.Sqrt(n) * 200f);
            d.StarThreshold1 = baseScore;
            d.StarThreshold2 = Mathf.RoundToInt(baseScore * 1.8f);
            d.StarThreshold3 = Mathf.RoundToInt(baseScore * 3.2f);

            // Gem spawn weights — introduce more types gradually
            d.GemSpawnWeights = tier switch {
                1 => new float[]{3f,3f,3f,0f,0f,0f},  // 3 types
                2 => new float[]{2f,2f,2f,2f,0f,0f},  // 4 types
                3 => new float[]{1f,1f,1f,1f,1f,0f},  // 5 types
                _ => new float[]{1f,1f,1f,1f,1f,1f},  // 6 types
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

            // Tier 4 & 5 — complex multi-objective. Collect targets grow
            // slowly and are capped so late levels remain beatable.
            {
                int over = n - 60;
                int c1 = Mathf.Min(60, 25 + over/12);
                int c2 = Mathf.Min(50, 20 + over/16);
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
