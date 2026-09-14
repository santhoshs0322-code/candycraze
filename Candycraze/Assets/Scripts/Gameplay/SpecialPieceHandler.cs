// ============================================================
// SpecialPieceHandler.cs
// Handles activation of all special piece types:
//   LineBlast   — clears entire row OR column
//   AreaBomb    — clears the 3×3 area twice around the candy
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
            if (matchGroup == null || matchGroup.Count < 3) return GemSpecialType.None;
            int count = matchGroup.Count;

            // A straight five has priority even when a crossing run shares it.
            // Otherwise an L/T/cross creates a wrapped candy.
            if (HasStraightFive(matchGroup)) return GemSpecialType.ColorCrystal;
            if (count >= 5 && IsLOrTShape(matchGroup)) return GemSpecialType.AreaBomb;

            if (count == 4) return GemSpecialType.LineBlast;

            return GemSpecialType.None;
        }

        /// <summary>Trigger each matched/hit power once, including powers reached by another blast.</summary>
        public List<GemView> ExpandSpecialChain(List<GemView> seed, GemView[,] grid, int rows, int cols, GemView alreadyActivated = null, GemView alsoActivated = null)
        {
            var result = new List<GemView>();
            var seen = new HashSet<GemView>();
            foreach (var gem in seed)
                if (gem != null && seen.Add(gem)) result.Add(gem);
            for (int i = 0; i < result.Count; i++)
            {
                var gem = result[i];
                if (gem == alreadyActivated || gem == alsoActivated || gem.SpecialType == GemSpecialType.None) continue;
                foreach (var hit in GetAffectedGems(gem, grid, rows, cols))
                    if (hit != null && seen.Add(hit)) result.Add(hit);
            }
            return result;
        }

        // ── Line Blast ───────────────────────────────────────

        private List<GemView> GetLineBlastGems(GemView gem,
            GemView[,] grid, int rows, int cols)
        {
            var result = new List<GemView>();

            // Stripes indicate the blast direction. Horizontal four-matches
            // create vertical stripes; vertical four-matches create horizontal stripes.
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

            for (int dr = -1; dr <= 1; dr++)
            for (int dc = -1; dc <= 1; dc++)
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

        public static bool HasStraightFive(List<GemView> group)
        {
            foreach (var origin in group)
            {
                int horizontal=0, vertical=0;
                for(int offset=0;offset<5;offset++)
                {
                    if(group.Exists(g=>g.Row==origin.Row && g.Col==origin.Col+offset)) horizontal++;
                    if(group.Exists(g=>g.Col==origin.Col && g.Row==origin.Row+offset)) vertical++;
                }
                if(horizontal==5 || vertical==5) return true;
            }
            return false;
        }

        public static GemView ChooseSpawn(List<GemView> group, GemView moved=null, GemView other=null)
        {
            if(moved!=null && group.Contains(moved)) return moved;
            if(other!=null && group.Contains(other)) return other;
            return group[group.Count/2];
        }

        public static bool CanCombine(GemView a, GemView b) =>
            a!=null && b!=null && (a.SpecialType==GemSpecialType.ColorCrystal ||
            b.SpecialType==GemSpecialType.ColorCrystal ||
            (a.SpecialType!=GemSpecialType.None && b.SpecialType!=GemSpecialType.None));

        public static List<GemView> Area(GemView[,] grid,int row,int col,int radius)
        {
            var result=new List<GemView>();
            for(int r=0;r<grid.GetLength(0);r++) for(int c=0;c<grid.GetLength(1);c++)
                if(Mathf.Abs(r-row)<=radius && Mathf.Abs(c-col)<=radius && grid[r,c]!=null) result.Add(grid[r,c]);
            return result;
        }

        public static List<GemView> Cross(GemView[,] grid,int row,int col,int radius=0)
        {
            var result=new List<GemView>();
            for(int r=0;r<grid.GetLength(0);r++) for(int c=0;c<grid.GetLength(1);c++)
                if((Mathf.Abs(r-row)<=radius || Mathf.Abs(c-col)<=radius) && grid[r,c]!=null) result.Add(grid[r,c]);
            return result;
        }

        // Both operands have already swapped. The dragged candy's destination is the combo center.
        public static List<GemView> Combination(GemView a,GemView b,GemView[,] grid,List<Vector3Int> repeats)
        {
            var result=new List<GemView>();
            if(!CanCombine(a,b)) return result;
            bool aBall=a.SpecialType==GemSpecialType.ColorCrystal, bBall=b.SpecialType==GemSpecialType.ColorCrystal;
            if(aBall || bBall)
            {
                var partner=aBall?b:a;
                for(int r=0;r<grid.GetLength(0);r++) for(int c=0;c<grid.GetLength(1);c++)
                {
                    var gem=grid[r,c];
                    if(gem==null) continue;
                    if(aBall && bBall) { result.Add(gem); continue; }
                    if(gem.GemTypeID!=partner.GemTypeID || gem.SpecialType==GemSpecialType.ColorCrystal) continue;
                    if(partner.SpecialType==GemSpecialType.LineBlast)
                        gem.SetPower(GemSpecialType.LineBlast,Random.value<.5f);
                    else if(partner.SpecialType==GemSpecialType.AreaBomb)
                        gem.SetPower(GemSpecialType.AreaBomb);
                    result.Add(gem);
                }
                // Partner is suppressed by the caller; explicitly include its effect.
                if(partner.SpecialType==GemSpecialType.LineBlast)
                    for(int r=0;r<grid.GetLength(0);r++) for(int c=0;c<grid.GetLength(1);c++)
                        if(grid[r,c]!=null && (partner.LineBlastVertical?c==partner.Col:r==partner.Row)) result.Add(grid[r,c]);
                if(partner.SpecialType==GemSpecialType.AreaBomb)
                { result.AddRange(Area(grid,partner.Row,partner.Col,1)); repeats.Add(new Vector3Int(partner.Row,partner.Col,1)); }
            }
            else if(a.SpecialType==GemSpecialType.LineBlast && b.SpecialType==GemSpecialType.LineBlast)
                result.AddRange(Cross(grid,a.Row,a.Col));
            else if(a.SpecialType==GemSpecialType.AreaBomb && b.SpecialType==GemSpecialType.AreaBomb)
            { result.AddRange(Area(grid,a.Row,a.Col,2)); repeats.Add(new Vector3Int(a.Row,a.Col,2)); }
            else result.AddRange(Cross(grid,a.Row,a.Col,1));
            result.Add(a); result.Add(b);
            return new List<GemView>(new HashSet<GemView>(result));
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
