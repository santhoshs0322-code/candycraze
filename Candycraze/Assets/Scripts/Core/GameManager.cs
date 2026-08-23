// ============================================================
// GameManager.cs
// Central coordinator for a single gameplay session.
// Lives in the Game scene.  Orchestrates the board, score,
// moves, objectives and win/lose state.
//
// This class intentionally does NOT contain board/match logic.
// It delegates to the specialist managers.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace CandyCraze
{
    public enum GameState
    {
        Idle,
        Playing,
        Paused,
        WaitingForBoard,   // Board is resolving (gravity / cascade)
        Won,
        Lost
    }

    public class GameManager : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static GameManager Instance { get; private set; }

        // ── Inspector references ─────────────────────────────
        [Header("Managers (auto-found if left empty)")]
        [SerializeField] private BoardManager    _boardManager;
        [SerializeField] private ScoreManager    _scoreManager;
        [SerializeField] private LevelManager    _levelManager;
        [SerializeField] private ObjectiveManager _objectiveManager;

        // ── State ────────────────────────────────────────────
        public GameState State { get; private set; } = GameState.Idle;

        private int _movesRemaining;
        public  int MovesRemaining => _movesRemaining;

        // ── Events ───────────────────────────────────────────
        /// <summary>Fires every time the move count changes.</summary>
        public UnityEvent<int> OnMovesChanged   = new UnityEvent<int>();
        public UnityEvent      OnGameWon         = new UnityEvent();
        public UnityEvent      OnGameLost        = new UnityEvent();
        /// <summary>Fires when the board is busy (no input allowed).</summary>
        public UnityEvent<bool> OnBoardBusy      = new UnityEvent<bool>();

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // Auto-find managers in scene if not assigned
            if (_boardManager    == null) _boardManager    = FindObjectOfType<BoardManager>();
            if (_scoreManager    == null) _scoreManager    = FindObjectOfType<ScoreManager>();
            if (_levelManager    == null) _levelManager    = FindObjectOfType<LevelManager>();
            if (_objectiveManager == null) _objectiveManager = FindObjectOfType<ObjectiveManager>();
        }

        private void Start()
        {
            StartLevel();
        }

        // ── Public API ───────────────────────────────────────

        /// <summary>Called by Bootstrap / LevelManager to kick off a level.</summary>
        public void StartLevel()
        {
            LevelData data = _levelManager.CurrentLevel;
            if (data == null)
            {
                Debug.LogError("[GameManager] No LevelData loaded!");
                return;
            }

            _movesRemaining = data.MoveLimit;
            State = GameState.Playing;

            _scoreManager.Reset();
            _objectiveManager.Initialise(data);
            _boardManager.Initialise(data);

            OnMovesChanged.Invoke(_movesRemaining);
            Debug.Log($"[GameManager] Started level {data.LevelNumber} — {_movesRemaining} moves.");
        }

        /// <summary>
        /// Called by SwapController after the player makes a valid swap.
        /// Decrements the move counter and checks win/lose.
        /// </summary>
        public void ConsumeMove()
        {
            if (State != GameState.Playing) return;

            _movesRemaining = Mathf.Max(0, _movesRemaining - 1);
            OnMovesChanged.Invoke(_movesRemaining);
            Debug.Log($"[GameManager] Moves remaining: {_movesRemaining}");
        }

        /// <summary>
        /// Called by BoardManager when the board finishes resolving
        /// (all gravity and cascades complete).
        /// </summary>
        public void OnBoardResolved()
        {
            SetBoardBusy(false);

            if (State != GameState.Playing) return;

            // Check win first
            if (_objectiveManager.AllObjectivesMet())
            {
                TriggerWin();
                return;
            }

            // Check lose (no moves left and objectives not met)
            if (_movesRemaining <= 0)
            {
                TriggerLose();
            }
        }

        /// <summary>Blocks or unblocks player input.</summary>
        public void SetBoardBusy(bool busy)
        {
            if (busy)
                State = GameState.WaitingForBoard;
            else if (State == GameState.WaitingForBoard)
                State = GameState.Playing;

            OnBoardBusy.Invoke(busy);
        }

        public void PauseGame()
        {
            if (State == GameState.Playing)
            {
                State = GameState.Paused;
                Time.timeScale = 0f;
            }
        }

        public void ResumeGame()
        {
            if (State == GameState.Paused)
            {
                State = GameState.Playing;
                Time.timeScale = 1f;
            }
        }

        public void RestartLevel()
        {
            Time.timeScale = 1f;
            StartLevel();
        }

        // ── Private ──────────────────────────────────────────
        private void TriggerWin()
        {
            State = GameState.Won;
            Debug.Log("[GameManager] *** LEVEL WON ***");
            OnGameWon.Invoke();
        }

        private void TriggerLose()
        {
            State = GameState.Lost;
            Debug.Log("[GameManager] *** LEVEL LOST ***");
            OnGameLost.Invoke();
        }
    }
}
