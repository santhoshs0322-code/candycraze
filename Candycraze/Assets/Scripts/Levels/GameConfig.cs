// ============================================================
// GameConfig.cs
// Single ScriptableObject that holds global game configuration.
// Create ONE asset at:
//   Assets/ScriptableObjects/Game/GameConfig.asset
// Reference it from the LevelManager or a ConfigProvider.
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    [CreateAssetMenu(
        fileName = "GameConfig",
        menuName  = "CandyCraze/Game Config")]
    public class GameConfig : ScriptableObject
    {
        [Header("Gem Definitions")]
        [Tooltip("All 6 gem definitions in order of GemTypeID (0-5).")]
        public GemDefinition[] GemDefinitions;

        [Header("Levels")]
        [Tooltip("All levels in order.  Index 0 = Level 1.")]
        public LevelData[] Levels;

        [Header("Lives")]
        public int   MaxLives          = Constants.MAX_LIVES;
        public float LifeRegenMinutes  = Constants.LIFE_REGEN_MINUTES;

        [Header("Economy")]
        public int CoinsPerStar      = Constants.COINS_PER_STAR;
        public int CoinsPerLevelWin  = Constants.COINS_PER_LEVEL_WIN;

        [Header("Board Timing")]
        public float SwapDuration         = Constants.SWAP_DURATION;
        public float InvalidSwapReturn    = Constants.INVALID_SWAP_RETURN;
        public float MatchDestroyDelay    = Constants.MATCH_DESTROY_DELAY;
        public float GravityDelay         = Constants.GRAVITY_DELAY;
        public float CascadeCheckDelay    = Constants.CASCADE_CHECK_DELAY;
        public float GemFallSpeed         = Constants.GEM_FALL_SPEED;
        public float SpawnDelay           = Constants.SPAWN_DELAY;

        [Header("Scoring")]
        public int ScorePerGem           = Constants.SCORE_PER_GEM;
        public int ComboScoreMultiplier  = Constants.COMBO_SCORE_MULTIPLIER;
        public int SpecialGemScore       = Constants.SPECIAL_GEM_SCORE;

        // ── Helpers ──────────────────────────────────────────

        /// <summary>Returns the GemDefinition for a given typeID, or null.</summary>
        public GemDefinition GetGemDefinition(int typeID)
        {
            if (GemDefinitions == null) return null;
            foreach (var def in GemDefinitions)
            {
                if (def != null && def.GemTypeID == typeID)
                    return def;
            }
            return null;
        }

        /// <summary>Returns the LevelData for a 1-based level number, or null.</summary>
        public LevelData GetLevelData(int levelNumber)
        {
            if (Levels == null || levelNumber < 1 || levelNumber > Levels.Length)
                return null;
            return Levels[levelNumber - 1];
        }

        public int TotalLevels => Levels?.Length ?? 0;
    }
}
