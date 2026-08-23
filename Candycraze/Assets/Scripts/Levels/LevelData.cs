// ============================================================
// LevelData.cs
// ScriptableObject that holds everything needed to configure
// one level.  Create one asset per level in:
//   Assets/ScriptableObjects/Levels/
// ============================================================

using System;
using UnityEngine;

namespace CandyCraze
{
    // ── Enums ────────────────────────────────────────────────

    public enum ObjectiveType
    {
        ReachScore,
        CollectGemType,
        ClearObstacles
    }

    public enum TileType
    {
        Normal,
        Empty,          // No tile — blank space in board shape
        Locked,         // Cannot have a gem placed here (wall)
        IceObstacle,    // Gem trapped in ice — needs 1 match adjacent to clear
        StoneObstacle   // Needs 2 matches adjacent to clear
    }

    // ── Supporting data structs ──────────────────────────────

    [Serializable]
    public class ObjectiveData
    {
        public ObjectiveType Type;

        [Tooltip("Target score (for ReachScore objective).")]
        public int TargetScore;

        [Tooltip("Gem type ID to collect (for CollectGemType objective).")]
        public int GemTypeID;

        [Tooltip("Amount to collect / clear.")]
        public int TargetAmount;

        [Tooltip("Display text shown in the HUD.")]
        public string Description;
    }

    [Serializable]
    public class TileData
    {
        public TileType Type = TileType.Normal;

        [Tooltip("If set, this tile will only spawn this specific gem type.  -1 = random.")]
        public int ForcedGemTypeID = -1;
    }

    // ── Main ScriptableObject ────────────────────────────────

    [CreateAssetMenu(
        fileName = "LevelData_01",
        menuName  = "CandyCraze/Level Data")]
    public class LevelData : ScriptableObject
    {
        [Header("Identity")]
        public int    LevelNumber  = 1;
        public string LevelName    = "Level 1";

        [Header("Board Layout")]
        [Tooltip("Number of rows on this level's board.")]
        public int Rows = Constants.DEFAULT_BOARD_ROWS;

        [Tooltip("Number of columns on this level's board.")]
        public int Cols = Constants.DEFAULT_BOARD_COLS;

        [Tooltip(
            "Flat array of tile data, row-major order. " +
            "Length must equal Rows × Cols. " +
            "Leave empty for a fully normal board.")]
        public TileData[] TileLayout;

        [Header("Moves & Scoring")]
        public int MoveLimit     = 25;
        public int StarThreshold1 = 1000;   // 1-star score
        public int StarThreshold2 = 3000;   // 2-star score
        public int StarThreshold3 = 6000;   // 3-star score

        [Header("Objectives")]
        public ObjectiveData[] Objectives;

        [Header("Gem Spawn Weights")]
        [Tooltip(
            "Relative weight for each gem type ID (0-5). " +
            "Higher = more frequent. Leave empty for equal weighting.")]
        public float[] GemSpawnWeights;

        [Header("Background")]
        [Tooltip("Background sprite / scene for this level.")]
        public Sprite BackgroundSprite;

        // ── Helpers ──────────────────────────────────────────

        /// <summary>
        /// Returns the TileData at (row, col), or a default Normal tile
        /// if TileLayout is empty or the index is out of range.
        /// </summary>
        public TileData GetTileData(int row, int col)
        {
            if (TileLayout == null || TileLayout.Length == 0)
                return new TileData { Type = TileType.Normal };

            int index = row * Cols + col;
            if (index < 0 || index >= TileLayout.Length)
                return new TileData { Type = TileType.Normal };

            return TileLayout[index];
        }

        /// <summary>
        /// Returns the spawn weight for a gem type, falling back to 1 if
        /// GemSpawnWeights is not configured.
        /// </summary>
        public float GetSpawnWeight(int gemTypeID)
        {
            if (GemSpawnWeights == null || gemTypeID >= GemSpawnWeights.Length)
                return 1f;
            return Mathf.Max(0f, GemSpawnWeights[gemTypeID]);
        }
    }
}
