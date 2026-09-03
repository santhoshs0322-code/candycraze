// ============================================================
// LevelRulesSetup.cs (EDITOR ONLY)
// CandyCraze → Setup Level Rules (Objectives + Moves + Stars)
//
// Configures each level with:
//   - Move limit
//   - Star score thresholds
//   - Collection objectives (collect N red, N blue, etc.)
//   - Increasing difficulty
//
// Gem type IDs: 0=Ruby(red) 1=Sapphire(blue) 2=Emerald(green)
//               3=Amethyst(purple) 4=Topaz(gold) 5=Diamond(white)
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CandyCraze.Editor
{
    public static class LevelRulesSetup
    {
        [MenuItem("CandyCraze/Setup Level Rules")]
        public static void Setup()
        {
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/GameConfig.asset");
            if (config == null || config.Levels == null)
            {
                EditorUtility.DisplayDialog("Error",
                    "GameConfig or levels not found. Run 'Create 100 Levels' first.", "OK");
                return;
            }

            int count = 0;
            for (int i = 0; i < config.Levels.Length; i++)
            {
                var lvl = config.Levels[i];
                if (lvl == null) continue;
                ConfigureRules(lvl, i + 1);
                EditorUtility.SetDirty(lvl);
                count++;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Level Rules Set!",
                $"Configured {count} levels with:\n\n" +
                "• Move limits (25 → 15 as difficulty rises)\n" +
                "• Star thresholds based on score\n" +
                "• Collection objectives (collect N of each gem)\n" +
                "• Progressive difficulty",
                "Done");
        }

        static void ConfigureRules(LevelData d, int n)
        {
            d.LevelNumber = n;
            d.LevelName   = $"Level {n}";
            d.Rows = 8; d.Cols = 8;

            int tier = n <= 10 ? 1 : n <= 30 ? 2 : n <= 60 ? 3 : 4;

            // ── Complexity factor: more objectives + tighter targets ──
            // Higher levels are more complex → MORE moves AND MORE score.
            int numObjectives = tier;   // tier 1=1 obj, tier 4=4 objs

            // ── Move limit — scales UP with complexity ───────
            // Base moves + extra per objective (more tasks = more moves)
            int baseMoves = tier switch {
                1 => 22,
                2 => 24,
                3 => 26,
                4 => 30,   // hardest levels get the most moves
                _ => 24
            };
            baseMoves += numObjectives * 3;      // +3 moves per objective
            baseMoves += (n / 20);               // slight increase deeper in
            if (n % 5 == 0) baseMoves -= 4;      // every 5th = challenge (fewer)
            d.MoveLimit = Mathf.Clamp(baseMoves, 15, 40);

            // ── Score thresholds — scale UP with complexity ──
            // More complex levels reward more points and require more.
            int complexityMult = 1 + tier;       // 2x..5x
            int baseScore = (800 + n * 250) * complexityMult / 2;
            d.StarThreshold1 = baseScore;
            d.StarThreshold2 = Mathf.RoundToInt(baseScore * 1.8f);
            d.StarThreshold3 = Mathf.RoundToInt(baseScore * 3f);

            // ── Objectives ───────────────────────────────────
            d.Objectives = BuildObjectives(n, tier, baseScore);

            // All 6 gem types spawn
            d.GemSpawnWeights = new float[] { 1f, 1f, 1f, 1f, 1f, 1f };
        }

        static ObjectiveData[] BuildObjectives(int n, int tier, int baseScore)
        {
            string[] gemNames = { "Red", "Blue", "Green", "Purple", "Gold", "White" };

            // Levels 1-3: Just reach a score (learn the game)
            if (n <= 3)
                return new[] { Score(baseScore) };

            // Levels 4-10: Score + blast a small amount of one colour
            if (n <= 10)
            {
                int g = (n - 4) % 6;
                int amt = 5 + n;                       // 9, 10, 11...
                return new[] {
                    Score(baseScore),
                    Collect(g, amt, $"Blast {amt} {gemNames[g]}")
                };
            }

            // Levels 11-30: Blast TWO colours (e.g. 9 red + 5 blue)
            if (n <= 30)
            {
                int gA = n % 6;
                int gB = (n + 2) % 6;
                int amtA = 9 + (n - 10) / 2;           // 9 → ~19
                int amtB = 5 + (n - 10) / 3;           // 5 → ~11
                return new[] {
                    Collect(gA, amtA, $"Blast {amtA} {gemNames[gA]}"),
                    Collect(gB, amtB, $"Blast {amtB} {gemNames[gB]}")
                };
            }

            // Levels 31-60: Blast THREE colours (increasing amounts)
            if (n <= 60)
            {
                int gA = n % 6, gB = (n + 2) % 6, gC = (n + 4) % 6;
                int amtA = 12 + (n - 30) / 3;
                int amtB = 8  + (n - 30) / 4;
                int amtC = 5  + (n - 30) / 5;
                return new[] {
                    Collect(gA, amtA, $"Blast {amtA} {gemNames[gA]}"),
                    Collect(gB, amtB, $"Blast {amtB} {gemNames[gB]}"),
                    Collect(gC, amtC, $"Blast {amtC} {gemNames[gC]}")
                };
            }

            // Levels 61-100: Score + blast three colours (very hard)
            {
                int gA = n % 6, gB = (n + 3) % 6, gC = (n + 5) % 6;
                int amtA = 15 + (n - 60) / 3;
                int amtB = 12 + (n - 60) / 4;
                int amtC = 10 + (n - 60) / 5;
                return new[] {
                    Score(baseScore),
                    Collect(gA, amtA, $"Blast {amtA} {gemNames[gA]}"),
                    Collect(gB, amtB, $"Blast {amtB} {gemNames[gB]}"),
                    Collect(gC, amtC, $"Blast {amtC} {gemNames[gC]}")
                };
            }
        }

        static ObjectiveData Score(int target) => new ObjectiveData {
            Type = ObjectiveType.ReachScore,
            TargetScore = target,
            Description = $"Score {target:N0}"
        };

        static ObjectiveData Collect(int gemType, int amount, string desc) => new ObjectiveData {
            Type = ObjectiveType.CollectGemType,
            GemTypeID = gemType,
            TargetAmount = amount,
            Description = desc
        };
    }
}
#endif
