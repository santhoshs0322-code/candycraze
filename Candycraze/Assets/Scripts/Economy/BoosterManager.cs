// ============================================================
// BoosterManager.cs
// Manages booster inventory and activation during gameplay.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace CandyCraze
{
    public enum BoosterType
    {
        Hammer,       // Tap any gem to destroy it
        RowBlast,     // Tap any gem to blast its row+col
        Shuffle,      // Reshuffles all gems on board
        ExtraMoves,   // Adds 5 extra moves
        ColorBlast    // Destroys all gems of a tapped type
    }

    public class BoosterManager : MonoBehaviour
    {
        public static BoosterManager Instance { get; private set; }

        public UnityEvent<BoosterType> OnBoosterActivated = new UnityEvent<BoosterType>();
        public UnityEvent              OnBoosterCancelled  = new UnityEvent();
        public UnityEvent              OnInventoryChanged  = new UnityEvent();

        public BoosterType? ActiveBooster { get; private set; }

        private BoardManager _board;
        private GameManager  _game;

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        private void Start()
        {
            _board = FindObjectOfType<BoardManager>();
            _game  = FindObjectOfType<GameManager>();
        }

        // ── Inventory Helpers ────────────────────────────────

        public int GetCount(BoosterType type)
        {
            if (SaveManager.Instance == null) return 0;
            var d = SaveManager.Instance.Data;
            return type switch
            {
                BoosterType.Hammer     => d.BoosterHammer,
                BoosterType.RowBlast   => d.BoosterRowBlast,
                BoosterType.Shuffle    => d.BoosterShuffle,
                BoosterType.ExtraMoves => d.BoosterExtraMoves,
                BoosterType.ColorBlast => d.BoosterColorBlast,
                _                      => 0
            };
        }

        private void DeductOne(BoosterType type)
        {
            if (SaveManager.Instance == null) return;
            var d = SaveManager.Instance.Data;
            switch (type)
            {
                case BoosterType.Hammer:     if (d.BoosterHammer     > 0) d.BoosterHammer--;     break;
                case BoosterType.RowBlast:   if (d.BoosterRowBlast   > 0) d.BoosterRowBlast--;   break;
                case BoosterType.Shuffle:    if (d.BoosterShuffle    > 0) d.BoosterShuffle--;    break;
                case BoosterType.ExtraMoves: if (d.BoosterExtraMoves > 0) d.BoosterExtraMoves--; break;
                case BoosterType.ColorBlast: if (d.BoosterColorBlast > 0) d.BoosterColorBlast--; break;
            }
            SaveManager.Instance.Save();
            OnInventoryChanged.Invoke();
        }

        // ── Activation ───────────────────────────────────────

        public bool TryActivate(BoosterType type)
        {
            if (_game == null || _game.State != GameState.Playing) return false;
            if (GetCount(type) <= 0) { Debug.Log($"[Booster] No {type} in inventory."); return false; }

            // ExtraMoves applies instantly
            if (type == BoosterType.ExtraMoves)
            {
                ApplyExtraMoves();
                return true;
            }

            // Shuffle applies instantly
            if (type == BoosterType.Shuffle)
            {
                ApplyShuffle();
                return true;
            }

            // Others wait for player to tap a gem
            ActiveBooster = type;
            OnBoosterActivated.Invoke(type);
            Debug.Log($"[Booster] Waiting for tap: {type}");
            return true;
        }

        public void Cancel()
        {
            ActiveBooster = null;
            OnBoosterCancelled.Invoke();
        }

        /// <summary>Called by SwapController when booster mode is active and player taps a gem.</summary>
        public void OnGemTappedWithBooster(GemView gem)
        {
            if (ActiveBooster == null || gem == null) return;

            BoosterType type = ActiveBooster.Value;
            ActiveBooster = null;
            DeductOne(type);

            switch (type)
            {
                case BoosterType.Hammer:
                    StartCoroutine(ApplyHammer(gem));
                    break;
                case BoosterType.RowBlast:
                    StartCoroutine(ApplyRowBlast(gem));
                    break;
                case BoosterType.ColorBlast:
                    StartCoroutine(ApplyColorBlast(gem));
                    break;
            }

            OnBoosterCancelled.Invoke();
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.SpecialPiece);
        }

        // ── Booster Effects ──────────────────────────────────

        private IEnumerator ApplyHammer(GemView gem)
        {
            _game.SetBoardBusy(true);
            yield return new WaitForSeconds(0.1f);

            int row = gem.Row, col = gem.Col;
            if (_board.GetGem(row, col) != null)
            {
                var toDestroy = new System.Collections.Generic.List<GemView> { gem };
                // Use reflection-like pattern — call BoardManager destroy pathway
                // For now: direct destroy and notify
                FindObjectOfType<ScoreManager>()?.AddScore(Constants.SCORE_PER_GEM * 2);
                FindObjectOfType<ObjectiveManager>()?.OnGemMatched(gem.GemTypeID);
                gem.PlayDestroyAnimation(null);
                // Let BoardManager handle gravity via its own cascade check
            }
            yield return new WaitForSeconds(0.5f);
            _game.SetBoardBusy(false);
        }

        private IEnumerator ApplyRowBlast(GemView gem)
        {
            _game.SetBoardBusy(true);
            var sp = FindObjectOfType<SpecialPieceHandler>();
            if (sp != null && _board != null)
            {
                // Create a temporary LineBlast gem effect
                var affected = sp.GetAffectedGems(
                    CreateTempSpecial(gem, GemSpecialType.LineBlast),
                    GetGrid(), _board.Rows, _board.Cols);
                // Score and destroy
                foreach (var g in affected)
                {
                    FindObjectOfType<ObjectiveManager>()?.OnGemMatched(g.GemTypeID);
                    g.PlayDestroyAnimation(null);
                }
                FindObjectOfType<ScoreManager>()?.AddScore(affected.Count * Constants.SCORE_PER_GEM);
            }
            yield return new WaitForSeconds(0.6f);
            _game.SetBoardBusy(false);
        }

        private IEnumerator ApplyColorBlast(GemView gem)
        {
            _game.SetBoardBusy(true);
            if (_board != null)
            {
                int targetType = gem.GemTypeID;
                var grid = GetGrid();
                if (grid != null)
                {
                    for (int r = 0; r < _board.Rows; r++)
                    for (int c = 0; c < _board.Cols; c++)
                    {
                        var g = grid[r, c];
                        if (g != null && g.GemTypeID == targetType)
                        {
                            FindObjectOfType<ObjectiveManager>()?.OnGemMatched(g.GemTypeID);
                            FindObjectOfType<ScoreManager>()?.AddScore(Constants.SCORE_PER_GEM);
                            g.PlayDestroyAnimation(null);
                        }
                    }
                }
            }
            yield return new WaitForSeconds(0.6f);
            _game.SetBoardBusy(false);
        }

        private void ApplyExtraMoves()
        {
            DeductOne(BoosterType.ExtraMoves);
            // Fire moves event via GameManager
            if (_game != null)
            {
                // Add 5 moves — access via reflection workaround
                var field = typeof(GameManager).GetField("_movesRemaining",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                if (field != null)
                {
                    int current = (int)field.GetValue(_game);
                    field.SetValue(_game, current + 5);
                    _game.OnMovesChanged.Invoke(current + 5);
                    Debug.Log($"[Booster] Extra moves! Now: {current + 5}");
                }
            }
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.SpecialPiece);
        }

        private void ApplyShuffle()
        {
            DeductOne(BoosterType.Shuffle);
            // Re-initialise board with same level (shuffles gems)
            var lm = FindObjectOfType<LevelManager>();
            var bm = FindObjectOfType<BoardManager>();
            if (lm != null && bm != null)
                bm.Initialise(lm.CurrentLevel);
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.SpecialPiece);
            Debug.Log("[Booster] Board shuffled.");
        }

        // ── Helpers ──────────────────────────────────────────

        private GemView[,] GetGrid()
        {
            if (_board == null) return null;
            // Access grid via public method pattern
            var grid = new GemView[_board.Rows, _board.Cols];
            for (int r = 0; r < _board.Rows; r++)
            for (int c = 0; c < _board.Cols; c++)
                grid[r, c] = _board.GetGem(r, c);
            return grid;
        }

        private GemView CreateTempSpecial(GemView source, GemSpecialType type)
        {
            // Create a wrapper with the special type set
            var go  = new GameObject("TempSpecial");
            var gv  = go.AddComponent<GemView>();
            // Manually set fields via reflection
            var rowF = typeof(GemView).GetProperty("Row");
            var colF = typeof(GemView).GetProperty("Col");
            var stF  = typeof(GemView).GetProperty("SpecialType");
            rowF?.SetValue(gv, source.Row);
            colF?.SetValue(gv, source.Col);
            stF?.SetValue(gv, type);
            // Clean up after one frame
            Destroy(go, 0.1f);
            return gv;
        }
    }
}
