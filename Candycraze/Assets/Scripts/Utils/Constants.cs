// ============================================================
// Constants.cs
// Global constants used across the entire project.
// Add new constants here rather than scattering magic numbers
// throughout the codebase.
// ============================================================

namespace CandyCraze
{
    public static class Constants
    {
        // ── Board ────────────────────────────────────────────
        public const int DEFAULT_BOARD_ROWS    = 8;
        public const int DEFAULT_BOARD_COLS    = 8;
        public const float CELL_SIZE           = 1.0f;   // World units per cell
        public const float GEM_FALL_SPEED      = 10f;    // Units per second when falling
        public const float SWAP_DURATION       = 0.2f;   // Seconds for a swap animation
        public const float INVALID_SWAP_RETURN = 0.15f;  // Seconds to return an invalid swap

        // ── Gem counts ───────────────────────────────────────
        public const int GEM_TYPE_COUNT        = 6;      // Normal gem types
        public const int MIN_MATCH_LENGTH      = 3;

        // ── Timing ───────────────────────────────────────────
        public const float MATCH_DESTROY_DELAY = 0.25f;  // Pause before gems disappear
        public const float GRAVITY_DELAY       = 0.05f;  // Delay between fall steps
        public const float CASCADE_CHECK_DELAY = 0.15f;  // Delay before checking for new matches
        public const float SPAWN_DELAY         = 0.05f;  // Stagger between spawning new gems

        // ── Scoring ──────────────────────────────────────────
        public const int SCORE_PER_GEM         = 60;
        public const int COMBO_SCORE_MULTIPLIER = 50;    // Extra per cascade level
        public const int SPECIAL_GEM_SCORE     = 200;

        // ── Lives ────────────────────────────────────────────
        public const int MAX_LIVES             = 5;
        public const int LIFE_REGEN_MINUTES    = 30;

        // ── Economy ──────────────────────────────────────────
        public const int COINS_PER_STAR        = 10;
        public const int COINS_PER_LEVEL_WIN   = 25;

        // ── Scene Names ──────────────────────────────────────
        public const string SCENE_BOOTSTRAP    = "Bootstrap";
        public const string SCENE_MAIN_MENU    = "MainMenu";
        public const string SCENE_LEVEL_MAP    = "LevelMap";
        public const string SCENE_GAME         = "Game";
        public const string SCENE_LOADING      = "Loading";

        // ── PlayerPrefs Keys ─────────────────────────────────
        public const string PREF_SAVE_DATA     = "SaveData";
        public const string PREF_SOUND_ON      = "SoundOn";
        public const string PREF_MUSIC_ON      = "MusicOn";

        // ── Tags ─────────────────────────────────────────────
        public const string TAG_GEM            = "Gem";
        public const string TAG_BOARD          = "Board";

        // ── Sorting Layers ───────────────────────────────────
        public const string LAYER_BOARD        = "Board";
        public const string LAYER_GEMS         = "Gems";
        public const string LAYER_EFFECTS      = "Effects";
        public const string LAYER_UI           = "UI";
    }
}
