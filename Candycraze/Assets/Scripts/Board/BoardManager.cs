// ============================================================
// BoardManager.cs
// Phase 3 update: Special pieces + combos + cascade feedback
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CandyCraze
{
    public class BoardManager : MonoBehaviour
    {
        // ── Inspector ────────────────────────────────────────
        [Header("References (auto-found if empty)")]
        [SerializeField] private TileManager        _tileManager;
        [SerializeField] private MatchDetector      _matchDetector;
        [SerializeField] private GravityController  _gravityController;
        [SerializeField] private ScoreManager       _scoreManager;
        [SerializeField] private ObjectiveManager   _objectiveManager;
        private SpecialPieceHandler _specialHandler;
        private BlastAnimator       _blastAnimator;

        [Header("Board Pivot")]
        [SerializeField] private Transform _boardRoot;

        // ── State ────────────────────────────────────────────
        private GemView[,] _grid;
        private LevelData  _levelData;
        private bool       _isBusy;

        public int Rows => _levelData?.Rows ?? Constants.DEFAULT_BOARD_ROWS;
        public int Cols => _levelData?.Cols ?? Constants.DEFAULT_BOARD_COLS;

        private GameConfig _config;

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            if (_tileManager      == null) _tileManager      = FindObjectOfType<TileManager>();
            if (_matchDetector    == null) _matchDetector    = FindObjectOfType<MatchDetector>();
            if (_gravityController== null) _gravityController= FindObjectOfType<GravityController>();
            if (_scoreManager     == null) _scoreManager     = FindObjectOfType<ScoreManager>();
            if (_objectiveManager == null) _objectiveManager = FindObjectOfType<ObjectiveManager>();
            if (_specialHandler   == null) _specialHandler   = FindObjectOfType<SpecialPieceHandler>();
            if (_blastAnimator    == null) _blastAnimator    = FindObjectOfType<BlastAnimator>();

            _config = Resources.Load<GameConfig>("GameConfig");
        }

        // ── Public API ───────────────────────────────────────

        public void Initialise(LevelData levelData)
        {
            _levelData = levelData;
            _grid = new GemView[Rows, Cols];

            if (_boardRoot != null)
                _boardRoot.DestroyAllChildren();

            FillBoard();
            ResolveStartingMatches();
        }

        public bool TrySwap(int rowA, int colA, int rowB, int colB)
        {
            if (_isBusy) return false;
            if (!IsInBounds(rowA, colA) || !IsInBounds(rowB, colB)) return false;

            GemView gemA = _grid[rowA, colA];
            GemView gemB = _grid[rowB, colB];

            if (gemA == null || gemB == null) return false;
            if (gemA.IsMoving || gemB.IsMoving) return false;

            StartCoroutine(SwapRoutine(gemA, gemB));
            return true;
        }

        public GemView GetGem(int row, int col)
        {
            if (!IsInBounds(row, col)) return null;
            return _grid[row, col];
        }

        public bool IsInBounds(int row, int col)
            => row >= 0 && row < Rows && col >= 0 && col < Cols;

        public Vector3 CellToWorld(int row, int col)
        {
            Vector3 origin = _boardRoot != null ? _boardRoot.position : Vector3.zero;
            return origin + new Vector3(col * Constants.CELL_SIZE, row * Constants.CELL_SIZE, 0f);
        }

        // ── Board Fill ───────────────────────────────────────

        private void FillBoard()
        {
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                if (_grid[r, c] != null) continue;
                TileData tile = _levelData.GetTileData(r, c);
                if (tile.Type == TileType.Empty || tile.Type == TileType.Locked) continue;
                GemDefinition def = _tileManager.GetRandomGemDefinition(_levelData, r, c);
                SpawnGem(def, r, c);
            }
        }

        private GemView SpawnGem(GemDefinition def, int row, int col,
                                  GemSpecialType special = GemSpecialType.None)
        {
            if (def == null) return null;

            Vector3 spawnPos = CellToWorld(row, col);
            Vector3 abovePos = spawnPos + Vector3.up * (Rows + 1) * Constants.CELL_SIZE;

            GameObject go = _tileManager.CreateGemObject(def, abovePos, _boardRoot);
            if (go == null) return null;

            GemView gem = go.GetComponent<GemView>();
            gem.Initialise(def, row, col, special);
            _grid[row, col] = gem;

            float dur = Constants.GEM_FALL_SPEED > 0
                ? Vector3.Distance(abovePos, spawnPos) / Constants.GEM_FALL_SPEED
                : 0.3f;
            gem.MoveTo(spawnPos, dur);

            return gem;
        }

        // ── Swap ─────────────────────────────────────────────

        private IEnumerator SwapRoutine(GemView gemA, GemView gemB)
        {
            SetBusy(true);

            int rA = gemA.Row, cA = gemA.Col;
            int rB = gemB.Row, cB = gemB.Col;

            DoGridSwap(gemA, gemB);

            float dur = _config?.SwapDuration ?? Constants.SWAP_DURATION;
            bool doneA = false, doneB = false;
            gemA.MoveTo(CellToWorld(rB, cB), dur, () => doneA = true);
            gemB.MoveTo(CellToWorld(rA, cA), dur, () => doneB = true);
            yield return new WaitUntil(() => doneA && doneB);

            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Swap);

            // Check if either gem is a special piece being activated
            bool specialActivated = false;
            if (gemA.SpecialType != GemSpecialType.None)
            {
                specialActivated = true;
                yield return StartCoroutine(ActivateSpecialPiece(gemA, gemB));
            }
            else if (gemB.SpecialType != GemSpecialType.None)
            {
                specialActivated = true;
                yield return StartCoroutine(ActivateSpecialPiece(gemB, gemA));
            }
            else
            {
                // Normal swap — check matches
                var matches = _matchDetector.FindAllMatches(_grid, Rows, Cols);

                if (matches.Count == 0)
                {
                    // Invalid — swap back
                    DoGridSwap(gemA, gemB);
                    float ret = _config?.InvalidSwapReturn ?? Constants.INVALID_SWAP_RETURN;
                    bool retA = false, retB = false;
                    gemA.MoveTo(CellToWorld(rA, cA), ret, () => retA = true);
                    gemB.MoveTo(CellToWorld(rB, cB), ret, () => retB = true);
                    yield return new WaitUntil(() => retA && retB);
                    AudioManager.Instance?.PlaySFX(AudioManager.SFX.InvalidSwap);
                    SetBusy(false);
                    yield break;
                }

                // Check for special piece creation BEFORE destroying
                CheckAndCreateSpecials(matches);

                GameManager.Instance?.ConsumeMove();
                yield return StartCoroutine(ResolveMatches(matches, cascadeLevel: 0));
            }
        }

        // ── Special Piece Activation ─────────────────────────

        private IEnumerator ActivateSpecialPiece(GemView special, GemView swappedWith)
        {
            if (_specialHandler == null) yield break;

            AudioManager.Instance?.PlaySFX(AudioManager.SFX.SpecialPiece);

            List<GemView> affected;

            if (special.SpecialType == GemSpecialType.ColorCrystal && swappedWith != null)
            {
                affected = new List<GemView>();
                for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (_grid[r, c] != null && _grid[r, c].GemTypeID == swappedWith.GemTypeID)
                        affected.Add(_grid[r, c]);
            }
            else
            {
                affected = _specialHandler.GetAffectedGems(special, _grid, Rows, Cols);
            }

            // ── Play blast animation BEFORE destroying ────────
            Color blastColor = _config?.GetGemDefinition(special.GemTypeID)?.GemColor
                               ?? Color.white;

            if (_blastAnimator != null)
                _blastAnimator.PlayBlast(special.SpecialType,
                    special.transform.position, blastColor, affected);

            // Brief pause for animation to show
            yield return new WaitForSeconds(0.25f);

            GameManager.Instance?.ConsumeMove();
            yield return StartCoroutine(DestroyGemList(affected, cascadeLevel: 0));
        }

        // ── Match Resolution ─────────────────────────────────

        private IEnumerator ResolveMatches(List<List<GemView>> matches, int cascadeLevel)
        {
            // Collect all gems to destroy
            var toDestroy = new List<GemView>();
            foreach (var group in matches)
                foreach (var gem in group)
                    if (!toDestroy.Contains(gem))
                        toDestroy.Add(gem);

            yield return StartCoroutine(DestroyGemList(toDestroy, cascadeLevel));
        }

        private IEnumerator DestroyGemList(List<GemView> toDestroy, int cascadeLevel)
        {
            if (toDestroy.Count == 0) { SetBusy(false); GameManager.Instance?.OnBoardResolved(); yield break; }

            // Score + objectives
            foreach (var gem in toDestroy)
            {
                gem.IsMatched = true;
                _objectiveManager?.OnGemMatched(gem.GemTypeID);

                // Spawn match particle at each gem position
                if (ParticleManager.Instance != null && _config != null)
                {
                    var def = _config.GetGemDefinition(gem.GemTypeID);
                    Color col = def != null ? def.GemColor : Color.white;
                    if (cascadeLevel >= 2)
                        ParticleManager.Instance.PlayComboBurst(gem.transform.position, col);
                    else
                        ParticleManager.Instance.PlayMatchBurst(gem.transform.position, col);
                }
            }

            int score = CalculateScore(toDestroy.Count, cascadeLevel);
            _scoreManager?.AddScore(score);

            // Combo feedback
            if (cascadeLevel >= 1)
            {
                UIManager.Instance?.ShowComboText(cascadeLevel + 1);
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.Combo);
                ScreenShake.Instance?.Shake();
                HapticFeedback.Medium();
            }
            else
            {
                AudioManager.Instance?.PlaySFX(AudioManager.SFX.Match);
                HapticFeedback.Light();
            }

            // Fire match burst animation for normal matches
            if (_blastAnimator != null)
            {
                Vector3 centre = Vector3.zero;
                foreach (var g in toDestroy) centre += g.transform.position;
                if (toDestroy.Count > 0)
                {
                    centre /= toDestroy.Count;
                    Color col = _config?.GetGemDefinition(toDestroy[0].GemTypeID)?.GemColor
                                ?? Color.white;
                    _blastAnimator.PlayMatchDestroy(centre, col, cascadeLevel);
                }
            }

            // Pause then destroy
            yield return new WaitForSeconds(_config?.MatchDestroyDelay ?? Constants.MATCH_DESTROY_DELAY);

            int pending = toDestroy.Count;
            foreach (var gem in toDestroy)
            {
                if (gem == null) { pending--; continue; }
                _grid[gem.Row, gem.Col] = null;
                gem.PlayDestroyAnimation(() => pending--);
            }
            yield return new WaitUntil(() => pending <= 0);

            // Gravity + refill
            yield return StartCoroutine(_gravityController.ApplyGravity(_grid, Rows, Cols, this));
            yield return StartCoroutine(RefillBoard());

            // Cascade check
            yield return new WaitForSeconds(_config?.CascadeCheckDelay ?? Constants.CASCADE_CHECK_DELAY);

            var newMatches = _matchDetector.FindAllMatches(_grid, Rows, Cols);
            if (newMatches.Count > 0)
            {
                CheckAndCreateSpecials(newMatches);
                yield return StartCoroutine(ResolveMatches(newMatches, cascadeLevel + 1));
            }
            else
            {
                SetBusy(false);
                GameManager.Instance?.OnBoardResolved();
            }
        }

        // ── Special Piece Creation ───────────────────────────

        /// <summary>
        /// Checks match groups for special-piece-creating patterns.
        /// Removes creating gem from grid and spawns a special in its place.
        /// </summary>
        private void CheckAndCreateSpecials(List<List<GemView>> matches)
        {
            foreach (var group in matches)
            {
                GemSpecialType special = SpecialPieceHandler.DetermineSpecialType(group);
                if (special == GemSpecialType.None) continue;

                // Pick centre gem of the group as the spawn point
                GemView centre = group[group.Count / 2];
                int row = centre.Row, col = centre.Col;
                int typeID = centre.GemTypeID;

                // Mark this gem to NOT be destroyed — it becomes the special
                group.Remove(centre);
                centre.IsMatched = false;

                // Schedule replacement after destruction
                StartCoroutine(SpawnSpecialAfterDelay(typeID, row, col, special, 0.35f));
            }
        }

        private IEnumerator SpawnSpecialAfterDelay(int typeID, int row, int col,
                                                    GemSpecialType special, float delay)
        {
            yield return new WaitForSeconds(delay);

            if (_grid[row, col] != null) yield break; // Already refilled

            GemDefinition def = _config?.GetGemDefinition(typeID);
            if (def == null) yield break;

            SpawnGem(def, row, col, special);
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.SpecialPiece);
        }

        // ── Refill ───────────────────────────────────────────

        private IEnumerator RefillBoard()
        {
            float delay = _config?.SpawnDelay ?? Constants.SPAWN_DELAY;
            bool any = false;

            for (int c = 0; c < Cols; c++)
            for (int r = 0; r < Rows; r++)
            {
                if (_grid[r, c] != null) continue;
                TileData tile = _levelData.GetTileData(r, c);
                if (tile.Type == TileType.Empty || tile.Type == TileType.Locked) continue;

                GemDefinition def = _tileManager.GetRandomGemDefinition(_levelData, r, c);
                SpawnGem(def, r, c);
                any = true;

                if (delay > 0) yield return new WaitForSeconds(delay);
            }

            if (any) yield return new WaitForSeconds(0.3f);
        }

        // ── Helpers ──────────────────────────────────────────

        private void DoGridSwap(GemView a, GemView b)
        {
            int rA = a.Row, cA = a.Col, rB = b.Row, cB = b.Col;
            _grid[rA, cA] = b; _grid[rB, cB] = a;
            a.Row = rB; a.Col = cB;
            b.Row = rA; b.Col = cA;
        }

        private void SetBusy(bool busy)
        {
            _isBusy = busy;
            GameManager.Instance?.SetBoardBusy(busy);
        }

        private int CalculateScore(int gemCount, int cascadeLevel)
        {
            int base_ = _config?.ScorePerGem ?? Constants.SCORE_PER_GEM;
            int combo = _config?.ComboScoreMultiplier ?? Constants.COMBO_SCORE_MULTIPLIER;
            return gemCount * base_ + cascadeLevel * combo;
        }

        private void ResolveStartingMatches()
        {
            for (int safety = 0; safety < 100; safety++)
            {
                var matches = _matchDetector.FindAllMatches(_grid, Rows, Cols);
                if (matches.Count == 0) break;

                foreach (var group in matches)
                foreach (var gem in group)
                {
                    if (gem == null) continue;
                    _grid[gem.Row, gem.Col] = null;
                    Destroy(gem.gameObject);
                }

                for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                {
                    if (_grid[r, c] != null) continue;
                    TileData tile = _levelData.GetTileData(r, c);
                    if (tile.Type == TileType.Empty || tile.Type == TileType.Locked) continue;
                    SpawnGem(_tileManager.GetRandomGemDefinition(_levelData, r, c), r, c);
                }
            }
        }
    }
}
