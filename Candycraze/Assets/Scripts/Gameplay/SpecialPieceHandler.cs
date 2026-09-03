// ============================================================
// SpecialPieceHandler.cs
// Handles activation of all special piece types:
//   LineBlast   — clears entire row OR column
//   AreaBomb    — clears 3×3 area around the gem
//   ColorCrystal— clears ALL gems of a target type
//
// Called by BoardManager after a match group is identified
// as containing a special piece.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CandyCraze
{
    public class SpecialPieceHandler : MonoBehaviour
    {
        // ── Dependencies ─────────────────────────────────────
        private BoardManager    _board;
        private ScoreManager    _score;
        private ObjectiveManager _objectives;
        private GameConfig      _config;
        private BlastAnimator   _blastAnimator;

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            _board         = FindObjectOfType<BoardManager>();
            _score         = FindObjectOfType<ScoreManager>();
            _objectives    = FindObjectOfType<ObjectiveManager>();
            _config        = Resources.Load<GameConfig>("GameConfig");
            _blastAnimator = FindObjectOfType<BlastAnimator>();
        }

        // ── Public API ───────────────────────────────────────

        /// <summary>
        /// Activates a special gem and returns all gems it will destroy.
        /// Does NOT destroy them — BoardManager handles destruction.
        /// </summary>
        public List<GemView> GetAffectedGems(GemView special,
            GemView[,] grid, int rows, int cols)
        {
            return special.SpecialType switch
            {
                GemSpecialType.LineBlast    => GetLineBlastGems(special, grid, rows, cols),
                GemSpecialType.AreaBomb     => GetAreaBombGems(special, grid, rows, cols),
                GemSpecialType.ColorCrystal => GetColorCrystalGems(special, grid, rows, cols),
                _                           => new List<GemView>()
            };
        }

        /// <summary>
        /// Determines what special type to create from a match group.
        /// Returns None for standard 3-matches.
        /// </summary>
        public static GemSpecialType DetermineSpecialType(List<GemView> matchGroup)
        {
            int count = matchGroup.Count;

            if (count >= 5) return GemSpecialType.ColorCrystal;
            if (count == 4) return GemSpecialType.LineBlast;

            // Check for L or T shape (count==4+ handled above, but cross/L can be 5)
            if (IsLOrTShape(matchGroup)) return GemSpecialType.AreaBomb;

            return GemSpecialType.None;
        }

        // ── Line Blast ───────────────────────────────────────

        private List<GemView> GetLineBlastGems(GemView gem,
            GemView[,] grid, int rows, int cols)
        {
            var result = new List<GemView>();

            // Directional: a bomb made from a VERTICAL 4-match clears its
            // COLUMN; one from a HORIZONTAL 4-match clears its ROW.
            if (gem.LineBlastVertical)
            {
                for (int r = 0; r < rows; r++)
                    if (grid[r, gem.Col] != null)
                        result.Add(grid[r, gem.Col]);
            }
            else
            {
                for (int c = 0; c < cols; c++)
                    if (grid[gem.Row, c] != null)
                        result.Add(grid[gem.Row, c]);
            }

            return result;
        }

        // ── Area Bomb ────────────────────────────────────────

        private List<GemView> GetAreaBombGems(GemView gem,
            GemView[,] grid, int rows, int cols)
        {
            var result = new List<GemView>();

            for (int dr = -2; dr <= 2; dr++)
            for (int dc = -2; dc <= 2; dc++)
            {
                int r = gem.Row + dr;
                int c = gem.Col + dc;
                if (r >= 0 && r < rows && c >= 0 && c < cols && grid[r, c] != null)
                    result.Add(grid[r, c]);
            }

            return result;
        }

        // ── Color Crystal ────────────────────────────────────

        private List<GemView> GetColorCrystalGems(GemView gem,
            GemView[,] grid, int rows, int cols)
        {
            var result = new List<GemView>();

            // Clears all gems of the same type as the gem it was swapped with
            // At creation time, target type = gem's own type
            int targetType = gem.GemTypeID;

            for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
                if (grid[r, c] != null && grid[r, c].GemTypeID == targetType)
                    result.Add(grid[r, c]);

            return result;
        }

        // ── Shape Detection ──────────────────────────────────

        /// <summary>
        /// True if the match group is a straight VERTICAL run (all gems share
        /// the same column). Used to orient the LineBlast bomb.
        /// </summary>
        public static bool IsVerticalMatch(List<GemView> group)
        {
            if (group == null || group.Count == 0) return false;
            int col = group[0].Col;
            foreach (var g in group)
                if (g.Col != col) return false;
            return true;
        }

        private static bool IsLOrTShape(List<GemView> group)
        {
            if (group.Count < 4) return false;

            // Count unique rows and unique cols in the group
            var rows = new HashSet<int>();
            var cols = new HashSet<int>();
            foreach (var g in group) { rows.Add(g.Row); cols.Add(g.Col); }

            // L or T shape spans at least 2 rows AND 2 cols
            return rows.Count >= 2 && cols.Count >= 2;
        }
    }
}
