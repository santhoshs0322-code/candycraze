// ============================================================
// MatchDetector.cs
// Finds all valid matches on the board.
// Detects:
//   • Horizontal 3+
//   • Vertical 3+
//   • L-shapes
//   • T-shapes
//   • Cross shapes
//   • 4-in-a-row  → marks for LineBlast special
//   • 5-in-a-row  → marks for ColorCrystal special
//
// The detector is purely functional — it does NOT modify the
// grid or create/destroy gems.  It returns grouped lists that
// BoardManager acts upon.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace CandyCraze
{
    public class MatchDetector : MonoBehaviour
    {
        // ── Public API ───────────────────────────────────────

        /// <summary>
        /// Scans the entire grid and returns a list of match groups.
        /// Each inner list is one distinct match set (may overlap for
        /// L/T shapes).  Duplicates within groups are removed.
        /// </summary>
        public List<List<GemView>> FindAllMatches(GemView[,] grid, int rows, int cols)
        {
            // Use a HashSet to mark gems already included in a match
            HashSet<GemView> matched = new HashSet<GemView>();
            List<List<GemView>> result = new List<List<GemView>>();

            // ── Horizontal runs ──────────────────────────────
            for (int r = 0; r < rows; r++)
            {
                int c = 0;
                while (c < cols)
                {
                    List<GemView> run = GetHorizontalRun(grid, rows, cols, r, c);
                    if (run.Count >= Constants.MIN_MATCH_LENGTH)
                    {
                        AddGroup(result, matched, run);
                    }
                    c += Mathf.Max(1, run.Count);
                }
            }

            // ── Vertical runs ────────────────────────────────
            for (int c = 0; c < cols; c++)
            {
                int r = 0;
                while (r < rows)
                {
                    List<GemView> run = GetVerticalRun(grid, rows, cols, r, c);
                    if (run.Count >= Constants.MIN_MATCH_LENGTH)
                    {
                        AddGroup(result, matched, run);
                    }
                    r += Mathf.Max(1, run.Count);
                }
            }

            // ── Merge overlapping groups (L/T/cross shapes) ──
            result = MergeOverlapping(result);

            return result;
        }

        // ── Private helpers ──────────────────────────────────

        private List<GemView> GetHorizontalRun(GemView[,] grid, int rows, int cols, int row, int startCol)
        {
            List<GemView> run = new List<GemView>();
            GemView first = grid[row, startCol];
            if (first == null || first.IsMatched) return run;

            run.Add(first);
            for (int c = startCol + 1; c < cols; c++)
            {
                GemView gem = grid[row, c];
                if (gem == null || gem.GemTypeID != first.GemTypeID) break;
                run.Add(gem);
            }
            return run;
        }

        private List<GemView> GetVerticalRun(GemView[,] grid, int rows, int cols, int startRow, int col)
        {
            List<GemView> run = new List<GemView>();
            GemView first = grid[startRow, col];
            if (first == null || first.IsMatched) return run;

            run.Add(first);
            for (int r = startRow + 1; r < rows; r++)
            {
                GemView gem = grid[r, col];
                if (gem == null || gem.GemTypeID != first.GemTypeID) break;
                run.Add(gem);
            }
            return run;
        }

        /// <summary>
        /// Merges groups that share at least one GemView.
        /// This naturally handles L, T, and cross shapes.
        /// </summary>
        private List<List<GemView>> MergeOverlapping(List<List<GemView>> groups)
        {
            List<List<GemView>> merged = new List<List<GemView>>();

            foreach (var group in groups)
            {
                bool foundMerge = false;
                foreach (var existing in merged)
                {
                    if (SharesGem(existing, group))
                    {
                        // Merge group into existing
                        foreach (var gem in group)
                        {
                            if (!existing.Contains(gem))
                                existing.Add(gem);
                        }
                        foundMerge = true;
                        break;
                    }
                }
                if (!foundMerge)
                {
                    merged.Add(new List<GemView>(group));
                }
            }

            // Second pass: merge groups that now share gems (catches complex shapes)
            bool changed = true;
            while (changed)
            {
                changed = false;
                for (int i = 0; i < merged.Count; i++)
                {
                    for (int j = i + 1; j < merged.Count; j++)
                    {
                        if (SharesGem(merged[i], merged[j]))
                        {
                            foreach (var gem in merged[j])
                                if (!merged[i].Contains(gem))
                                    merged[i].Add(gem);
                            merged.RemoveAt(j);
                            j--;
                            changed = true;
                        }
                    }
                }
            }

            return merged;
        }

        private bool SharesGem(List<GemView> a, List<GemView> b)
        {
            foreach (var gem in a)
                if (b.Contains(gem)) return true;
            return false;
        }

        private void AddGroup(List<List<GemView>> result, HashSet<GemView> matched, List<GemView> run)
        {
            // Still add even if some gems are already in another group
            // (MergeOverlapping will unify them)
            result.Add(new List<GemView>(run));
        }
    }
}
