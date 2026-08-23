// ============================================================
// GravityController.cs
// After gems are destroyed, drops remaining gems downward
// to fill the gaps, updating both the logical grid and the
// GemView world positions.
// ============================================================

using System.Collections;
using UnityEngine;

namespace CandyCraze
{
    public class GravityController : MonoBehaviour
    {
        // ── Public API ───────────────────────────────────────

        /// <summary>
        /// Drops all gems in each column as far as possible to fill gaps.
        /// Awaits all movement coroutines before returning.
        /// </summary>
        public IEnumerator ApplyGravity(GemView[,] grid, int rows, int cols,
                                        BoardManager board)
        {
            bool anyMoved = false;

            for (int c = 0; c < cols; c++)
            {
                anyMoved |= DropColumn(grid, rows, cols, c, board);
            }

            if (anyMoved)
            {
                // Wait for the longest possible fall to complete
                // (rough estimate: max rows / fall speed)
                float maxFallTime = rows * Constants.CELL_SIZE / Constants.GEM_FALL_SPEED + 0.1f;
                yield return new WaitForSeconds(maxFallTime);
            }
        }

        // ── Private ──────────────────────────────────────────

        /// <summary>
        /// Scans a single column bottom-up.  For each empty slot, finds
        /// the first gem above it and drops it down.
        /// Returns true if any gem moved.
        /// </summary>
        private bool DropColumn(GemView[,] grid, int rows, int cols, int col,
                                 BoardManager board)
        {
            bool moved = false;

            // Iterate from row 0 (bottom) upward
            for (int emptyRow = 0; emptyRow < rows; emptyRow++)
            {
                if (grid[emptyRow, col] != null) continue;   // Not empty

                // Find the next gem above this gap
                for (int gemRow = emptyRow + 1; gemRow < rows; gemRow++)
                {
                    if (grid[gemRow, col] == null) continue;   // Also empty

                    GemView gem = grid[gemRow, col];

                    // Move in grid
                    grid[emptyRow, col] = gem;
                    grid[gemRow, col]   = null;
                    gem.Row = emptyRow;

                    // Animate to new world position
                    Vector3 target = board.CellToWorld(emptyRow, col);
                    float dist     = Vector3.Distance(gem.transform.position, target);
                    float dur      = dist / Constants.GEM_FALL_SPEED;

                    gem.MoveTo(target, dur);
                    moved = true;
                    break;
                }
            }

            return moved;
        }
    }
}
