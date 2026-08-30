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

            // Defensive: ensure all dependencies exist
            if (_tileManager       == null) _tileManager       = FindObjectOfType<TileManager>(true);
            if (_matchDetector     == null) _matchDetector     = FindObjectOfType<MatchDetector>(true);
            if (_gravityController == null) _gravityController = FindObjectOfType<GravityController>(true);
            if (_config            == null) _config            = Resources.Load<GameConfig>("GameConfig");

            if (_boardRoot == null)
            {
                var br = GameObject.Find("BoardRoot");
                if (br != null) _boardRoot = br.transform;
                else
                {
                    var newBr = new GameObject("BoardRoot");
                    newBr.transform.position = Vector3.zero;
                    _boardRoot = newBr.transform;
                }
            }

            if (_tileManager == null)
            {
                Debug.LogError("[BoardManager] TileManager missing — cannot spawn gems!");
                return;
            }

            _grid = new GemView[Rows, Cols];
            _boardRoot.DestroyAllChildren();

            Debug.Log($"[BoardManager] Initialising {Rows}x{Cols} board at {_boardRoot.position}");
            FillBoard();
            ResolveStartingMatches();

            // Spawn 1-2 random Color Balls at start (based on level difficulty)
            SpawnStartingColorBalls();

            Debug.Log($"[BoardManager] Board filled. BoardRoot has {_boardRoot.childCount} gems.");
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

        // ── Starting Color Balls ─────────────────────────────

        private void SpawnStartingColorBalls()
        {
            // Give 1 color ball to start; harder levels get 2
            int levelNum = _levelData != null ? _levelData.LevelNumber : 1;
            int count = levelNum > 30 ? 2 : 1;

            for (int i = 0; i < count; i++)
            {
                // Pick a random cell that has a normal gem
                int tries = 20;
                while (tries-- > 0)
                {
                    int r = Random.Range(0, Rows);
                    int c = Random.Range(0, Cols);
                    var existing = _grid[r, c];
                    if (existing != null && existing.SpecialType == GemSpecialType.None)
                    {
                        int typeID = existing.GemTypeID;
                        // Destroy the normal gem and replace with a color ball
                        _grid[r, c] = null;
                        Destroy(existing.gameObject);

                        GemDefinition def = _config != null ? _config.GetGemDefinition(typeID) : null;
                        if (def == null) def = _tileManager.GetRandomGemDefinition(_levelData, r, c);
                        SpawnGem(def, r, c, GemSpecialType.ColorCrystal);
                        break;
                    }
                }
            }
            Debug.Log($"[BoardManager] Spawned {count} starting color ball(s).");
        }

        // ── Board Fill ───────────────────────────────────────

        private void FillBoard()
        {
            int spawned = 0, skipped = 0, nullDef = 0;
            for (int r = 0; r < Rows; r++)
            for (int c = 0; c < Cols; c++)
            {
                if (_grid[r, c] != null) continue;

                TileData tile = _levelData.GetTileData(r, c);
                if (tile != null && (tile.Type == TileType.Empty || tile.Type == TileType.Locked))
                {
                    skipped++;
                    continue;
                }

                GemDefinition def = _tileManager.GetRandomGemDefinition(_levelData, r, c);
                if (def == null)
                {
                    nullDef++;
                    continue;
                }

                var gem = SpawnGem(def, r, c);
                if (gem != null) spawned++;
            }
            Debug.Log($"[BoardManager] FillBoard: spawned={spawned} skipped={skipped} nullDef={nullDef}");
        }

        private GemView SpawnGem(GemDefinition def, int row, int col,
                                  GemSpecialType special = GemSpecialType.None)
        {
            if (def == null) return null;

            Vector3 spawnPos = CellToWorld(row, col);

            // Spawn directly at final position (no fall animation for reliability)
            GameObject go = _tileManager.CreateGemObject(def, spawnPos, _boardRoot);
            if (go == null) return null;

            GemView gem = go.GetComponent<GemView>();
            gem.Initialise(def, row, col, special);
            gem.SnapTo(spawnPos);
            _grid[row, col] = gem;

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

                // ResolveMatches now handles special creation internally
                GameManager.Instance?.ConsumeMove();
                yield return StartCoroutine(ResolveMatches(matches, cascadeLevel: 0));
            }
        }

        // ── Special Piece Activation ─────────────────────────

        private IEnumerator ActivateSpecialPiece(GemView special, GemView swappedWith)
        {
            if (_specialHandler == null) yield break;

            AudioManager.Instance?.PlaySFX(AudioManager.SFX.SpecialPiece);

            List<GemView> affected = new List<GemView>();

            bool isColorBall  = special.SpecialType == GemSpecialType.ColorCrystal;
            bool otherIsBall  = swappedWith != null &&
                                swappedWith.SpecialType == GemSpecialType.ColorCrystal;

            if (isColorBall && otherIsBall)
            {
                // ── Color Ball + Color Ball → CLEAR ENTIRE BOARD ──
                for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (_grid[r, c] != null)
                        affected.Add(_grid[r, c]);

                ScreenShake.Instance?.ShakeHeavy();
                HapticFeedback.Heavy();
            }
            else if (isColorBall && swappedWith != null)
            {
                // ── Color Ball + normal gem → remove all of that colour ──
                int targetType = swappedWith.GemTypeID;
                for (int r = 0; r < Rows; r++)
                for (int c = 0; c < Cols; c++)
                    if (_grid[r, c] != null && _grid[r, c].GemTypeID == targetType)
                        affected.Add(_grid[r, c]);

                // Also destroy the color ball itself
                if (!affected.Contains(special)) affected.Add(special);
            }
            else
            {
                // Line Blast / Area Bomb
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

        // Pending special pieces to create after destruction
        private struct PendingSpecial { public int row, col, typeID; public GemSpecialType type; }
        private List<PendingSpecial> _pendingSpecials = new List<PendingSpecial>();

        private IEnumerator ResolveMatches(List<List<GemView>> matches, int cascadeLevel)
        {
            _pendingSpecials.Clear();

            // Determine which groups create specials — reserve their spawn cell
            foreach (var group in matches)
            {
                GemSpecialType special = SpecialPieceHandler.DetermineSpecialType(group);
                Debug.Log($"[BoardManager] Match group size={group.Count} → special={special}");
                if (special == GemSpecialType.None) continue;

                GemView centre = group[group.Count / 2];
                _pendingSpecials.Add(new PendingSpecial {
                    row = centre.Row, col = centre.Col,
                    typeID = centre.GemTypeID, type = special
                });
                Debug.Log($"[BoardManager] Reserved {special} at ({centre.Row},{centre.Col})");
            }

            // Collect all gems to destroy (ALL matched gems removed)
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

            // ── Spawn special pieces in their reserved cells NOW ──
            // (before gravity, so they stay in the correct spot)
            if (_pendingSpecials.Count > 0)
            {
                foreach (var ps in _pendingSpecials)
                {
                    if (_grid[ps.row, ps.col] != null) continue; // occupied somehow
                    GemDefinition def = _config != null ? _config.GetGemDefinition(ps.typeID) : null;
                    if (def == null) def = _tileManager.GetRandomGemDefinition(_levelData, ps.row, ps.col);
                    var special = SpawnGem(def, ps.row, ps.col, ps.type);
                    if (special != null)
                    {
                        AudioManager.Instance?.PlaySFX(AudioManager.SFX.SpecialPiece);
                        Debug.Log($"[BoardManager] Created {ps.type} at ({ps.row},{ps.col})");
                    }
                }
                _pendingSpecials.Clear();
            }

            // Gravity + refill
            yield return StartCoroutine(_gravityController.ApplyGravity(_grid, Rows, Cols, this));
            yield return StartCoroutine(RefillBoard());

            // Cascade check
            yield return new WaitForSeconds(_config?.CascadeCheckDelay ?? Constants.CASCADE_CHECK_DELAY);

            var newMatches = _matchDetector.FindAllMatches(_grid, Rows, Cols);
            if (newMatches.Count > 0)
            {
                yield return StartCoroutine(ResolveMatches(newMatches, cascadeLevel + 1));
            }
            else
            {
                SetBusy(false);
                GameManager.Instance?.OnBoardResolved();
            }
        }

        // ── Special Piece Creation ───────────────────────────

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
