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
            d.BoostersUsed++;
            SaveManager.Instance.Save();
            OnInventoryChanged.Invoke();
        }

        // ── Activation ───────────────────────────────────────

        public bool TryActivate(BoosterType type)
        {
            if (_game == null || _game.State != GameState.Playing || _board == null || _board.IsBusy) return false;
            if (GetCount(type) <= 0) { Debug.Log($"[Booster] No {type} in inventory."); return false; }

            // ExtraMoves applies instantly
            if (type == BoosterType.ExtraMoves)
            {
                StartCoroutine(ApplyExtraMovesRoutine());
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
            if (!_board.ApplyBooster(type, gem)) return;
            ActiveBooster = null;
            DeductOne(type);

            OnBoosterCancelled.Invoke();
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.SpecialPiece);
        }

        // ── Booster Effects ──────────────────────────────────

        private IEnumerator ApplyExtraMovesRoutine()
        {
            ActiveBooster=null;
            OnBoosterCancelled.Invoke();
            DeductOne(BoosterType.ExtraMoves);
            UIManager.Instance?.ShowMoveBonus(5);
            yield return new WaitForSeconds(.32f);
            _game.AddMoves(5);
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.SpecialPiece);
        }

        private void ApplyShuffle()
        {
            ActiveBooster=null;
            if(_board.ShuffleCandies()) DeductOne(BoosterType.Shuffle);
            OnBoosterCancelled.Invoke();
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.SpecialPiece);
        }
    }
}
