// ============================================================
// Extensions.cs
// Utility extension methods used throughout the project.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace CandyCraze
{
    public static class Extensions
    {
        // ── List Extensions ──────────────────────────────────

        /// <summary>Fisher-Yates shuffle for any list.</summary>
        public static void Shuffle<T>(this IList<T> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Random.Range(0, n + 1);
                (list[k], list[n]) = (list[n], list[k]);
            }
        }

        // ── Vector Extensions ────────────────────────────────

        /// <summary>Returns a Vector2Int from a world position snapped to grid.</summary>
        public static Vector2Int ToGridPos(this Vector3 worldPos, float cellSize = Constants.CELL_SIZE)
        {
            return new Vector2Int(
                Mathf.RoundToInt(worldPos.x / cellSize),
                Mathf.RoundToInt(worldPos.y / cellSize)
            );
        }

        // ── Transform Extensions ─────────────────────────────

        /// <summary>Destroys all children of a transform.</summary>
        public static void DestroyAllChildren(this Transform parent)
        {
            for (int i = parent.childCount - 1; i >= 0; i--)
            {
                Object.Destroy(parent.GetChild(i).gameObject);
            }
        }

        // ── Color Extensions ─────────────────────────────────

        /// <summary>Returns the same color with a new alpha value.</summary>
        public static Color WithAlpha(this Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, alpha);
        }

        // ── Array utility ────────────────────────────────────

        /// <summary>Returns true if the given row and col are within bounds of a 2D array.</summary>
        public static bool IsInBounds<T>(this T[,] array, int row, int col)
        {
            return row >= 0 && row < array.GetLength(0)
                && col >= 0 && col < array.GetLength(1);
        }
    }
}
