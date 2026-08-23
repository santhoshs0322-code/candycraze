// ============================================================
// CandyCrazeSetup.cs  (EDITOR ONLY — placed in an Editor folder)
// Unity menu tool:  CandyCraze → Setup Project
//
// Creates:
//   • 6 GemDefinition ScriptableObjects
//   • 20 LevelData ScriptableObjects
//   • 1 GameConfig ScriptableObject  (auto-populated)
//   • Folder structure under Assets/
//
// Run this ONCE after importing the scripts into a new project.
// You can re-run it safely — it will not overwrite existing assets.
// ============================================================

#if UNITY_EDITOR

using System.IO;
using UnityEditor;
using UnityEngine;

namespace CandyCraze.Editor
{
    public static class CandyCrazeSetup
    {
        private static readonly string[] GemNames   = { "Ruby", "Sapphire", "Emerald", "Amethyst", "Topaz", "Diamond" };
        private static readonly Color[]  GemColors  =
        {
            new Color(0.9f, 0.15f, 0.15f),   // Ruby       — red
            new Color(0.15f, 0.35f, 0.9f),   // Sapphire   — blue
            new Color(0.15f, 0.8f,  0.25f),  // Emerald    — green
            new Color(0.6f,  0.15f, 0.9f),   // Amethyst   — purple
            new Color(1f,    0.75f, 0.1f),   // Topaz      — yellow
            new Color(0.9f,  0.95f, 1f),     // Diamond    — white-blue
        };

        // ── Level templates: (MoveLimit, Star1, Star2, Star3) ─
        private static readonly (int moves, int s1, int s2, int s3)[] LevelTemplates =
        {
            (25,  1000,  3000,  6000),    // 01
            (25,  1200,  3500,  7000),    // 02
            (22,  1500,  4000,  8000),    // 03
            (22,  1800,  4500,  9000),    // 04
            (20,  2000,  5000, 10000),    // 05
            (20,  2500,  5500, 11000),    // 06
            (18,  3000,  6000, 12000),    // 07
            (18,  3500,  6500, 13000),    // 08
            (16,  4000,  7000, 14000),    // 09
            (16,  4500,  7500, 15000),    // 10
            (15,  5000,  8000, 16000),    // 11
            (15,  5500,  8500, 17000),    // 12
            (14,  6000,  9000, 18000),    // 13
            (14,  6500,  9500, 19000),    // 14
            (13,  7000, 10000, 20000),    // 15
            (13,  7500, 11000, 21000),    // 16
            (12,  8000, 12000, 22000),    // 17
            (12,  9000, 13000, 23000),    // 18
            (10, 10000, 14000, 24000),    // 19
            (10, 12000, 16000, 28000),    // 20
        };

        // ────────────────────────────────────────────────────

        [MenuItem("CandyCraze/Setup Project (Create Assets)")]
        public static void SetupProject()
        {
            CreateFolders();

            GemDefinition[] gemDefs = CreateGemDefinitions();
            LevelData[]     levels  = CreateLevelDataAssets();
            CreateGameConfig(gemDefs, levels);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[CandyCrazeSetup] ✓ Project setup complete!");
            EditorUtility.DisplayDialog(
                "CandyCraze Setup",
                "Assets created successfully!\n\n" +
                "Next steps:\n" +
                "1. Assign gem sprites to each GemDefinition in Assets/ScriptableObjects/Gems/\n" +
                "2. Assign gem prefabs to each GemDefinition\n" +
                "3. Place GameConfig.asset in Assets/Resources/\n" +
                "4. Build and test the Bootstrap scene.",
                "Got it");
        }

        // ── Folder Creation ──────────────────────────────────

        private static void CreateFolders()
        {
            string[] folders =
            {
                "Assets/Art/Gems",
                "Assets/Art/UI",
                "Assets/Art/Backgrounds",
                "Assets/Art/Effects",
                "Assets/Audio/Music",
                "Assets/Audio/SFX",
                "Assets/Animations",
                "Assets/Materials",
                "Assets/Prefabs/Gems",
                "Assets/Prefabs/UI",
                "Assets/Prefabs/Effects",
                "Assets/Scenes",
                "Assets/Scripts/Core",
                "Assets/Scripts/Board",
                "Assets/Scripts/Gameplay",
                "Assets/Scripts/Levels",
                "Assets/Scripts/UI",
                "Assets/Scripts/Economy",
                "Assets/Scripts/Ads",
                "Assets/Scripts/IAP",
                "Assets/Scripts/Save",
                "Assets/Scripts/Audio",
                "Assets/Scripts/Utils",
                "Assets/Scripts/Editor",
                "Assets/ScriptableObjects/Gems",
                "Assets/ScriptableObjects/Levels",
                "Assets/ScriptableObjects/Game",
                "Assets/Resources",
                "Assets/Plugins",
            };

            foreach (string path in folders)
            {
                if (!AssetDatabase.IsValidFolder(path))
                {
                    string parent = Path.GetDirectoryName(path).Replace('\\', '/');
                    string folder = Path.GetFileName(path);
                    AssetDatabase.CreateFolder(parent, folder);
                    Debug.Log($"[CandyCrazeSetup] Created folder: {path}");
                }
            }
        }

        // ── Gem Definitions ──────────────────────────────────

        private static GemDefinition[] CreateGemDefinitions()
        {
            GemDefinition[] defs = new GemDefinition[GemNames.Length];

            for (int i = 0; i < GemNames.Length; i++)
            {
                string assetPath = $"Assets/ScriptableObjects/Gems/GemDefinition_{GemNames[i]}.asset";

                GemDefinition existing = AssetDatabase.LoadAssetAtPath<GemDefinition>(assetPath);
                if (existing != null)
                {
                    defs[i] = existing;
                    continue;
                }

                GemDefinition def = ScriptableObject.CreateInstance<GemDefinition>();
                def.GemTypeID  = i;
                def.GemName    = GemNames[i];
                def.GemColor   = GemColors[i];

                AssetDatabase.CreateAsset(def, assetPath);
                defs[i] = def;
                Debug.Log($"[CandyCrazeSetup] Created GemDefinition: {GemNames[i]}");
            }

            return defs;
        }

        // ── Level Data ───────────────────────────────────────

        private static LevelData[] CreateLevelDataAssets()
        {
            LevelData[] levels = new LevelData[LevelTemplates.Length];

            for (int i = 0; i < LevelTemplates.Length; i++)
            {
                int levelNum  = i + 1;
                string path   = $"Assets/ScriptableObjects/Levels/LevelData_{levelNum:D2}.asset";

                LevelData existing = AssetDatabase.LoadAssetAtPath<LevelData>(path);
                if (existing != null)
                {
                    levels[i] = existing;
                    continue;
                }

                var (moves, s1, s2, s3) = LevelTemplates[i];

                LevelData data = ScriptableObject.CreateInstance<LevelData>();
                data.LevelNumber      = levelNum;
                data.LevelName        = $"Level {levelNum}";
                data.Rows             = 8;
                data.Cols             = 8;
                data.MoveLimit        = moves;
                data.StarThreshold1   = s1;
                data.StarThreshold2   = s2;
                data.StarThreshold3   = s3;

                // Default objectives: reach score
                data.Objectives = new ObjectiveData[]
                {
                    new ObjectiveData
                    {
                        Type        = ObjectiveType.ReachScore,
                        TargetScore = s1,
                        Description = $"Score {s1:N0}"
                    }
                };

                // Equal spawn weights
                data.GemSpawnWeights = new float[] { 1f, 1f, 1f, 1f, 1f, 1f };

                AssetDatabase.CreateAsset(data, path);
                levels[i] = data;
                Debug.Log($"[CandyCrazeSetup] Created LevelData_{levelNum:D2}");
            }

            return levels;
        }

        // ── Game Config ──────────────────────────────────────

        private static void CreateGameConfig(GemDefinition[] gems, LevelData[] levels)
        {
            // Must live in Resources/ for Runtime.Load to find it
            string path = "Assets/Resources/GameConfig.asset";

            GameConfig config = AssetDatabase.LoadAssetAtPath<GameConfig>(path);
            if (config == null)
            {
                config = ScriptableObject.CreateInstance<GameConfig>();
                AssetDatabase.CreateAsset(config, path);
                Debug.Log("[CandyCrazeSetup] Created GameConfig.asset");
            }

            config.GemDefinitions = gems;
            config.Levels         = levels;

            EditorUtility.SetDirty(config);
        }
    }
}

#endif
