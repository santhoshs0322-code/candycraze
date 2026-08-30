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
            // Delay one frame so all managers' Awake/Start complete first
            StartCoroutine(StartLevelDelayed());
        }

        private System.Collections.IEnumerator StartLevelDelayed()
        {
            yield return null; // wait one frame
            StartLevel();
        }

        // ── Public API ───────────────────────────────────────

        /// <summary>Called by Bootstrap / LevelManager to kick off a level.</summary>
        public void StartLevel()
        {
            // Re-find any missing managers (include inactive)
            if (_levelManager    == null) _levelManager    = FindObjectOfType<LevelManager>(true);
            if (_scoreManager    == null) _scoreManager    = FindObjectOfType<ScoreManager>(true);
            if (_objectiveManager== null) _objectiveManager= FindObjectOfType<ObjectiveManager>(true);
            if (_boardManager    == null) _boardManager    = FindObjectOfType<BoardManager>(true);

            if (_levelManager == null)
            {
                Debug.LogError("[GameManager] LevelManager missing in scene!");
                return;
            }

            LevelData data = _levelManager.CurrentLevel;
            if (data == null)
            {
                Debug.LogError("[GameManager] No LevelData loaded! " +
                    "GameConfig may be empty or level number invalid.");
                return;
            }

            _movesRemaining = data.MoveLimit;
            State = GameState.Playing;

            if (_scoreManager != null)     _scoreManager.Reset();
            else Debug.LogWarning("[GameManager] ScoreManager missing.");

            if (_objectiveManager != null) _objectiveManager.Initialise(data);
            else Debug.LogWarning("[GameManager] ObjectiveManager missing.");

            if (_boardManager != null)     _boardManager.Initialise(data);
            else Debug.LogError("[GameManager] BoardManager missing — board won't spawn!");

            OnMovesChanged.Invoke(_movesRemaining);
            Debug.Log($"[GameManager] Started level {data.LevelNumber} — {_movesRemaining} moves.");
        }

        /// <summary>
        /// Called by SwapController after the player makes a valid swap.
        /// Decrements the move counter and checks win/lose.
        /// </summary>
        public void ConsumeMove()
        {
            // Allow during Playing OR WaitingForBoard (swap in progress)
            if (State == GameState.Won || State == GameState.Lost || State == GameState.Paused)
                return;

            _movesRemaining = Mathf.Max(0, _movesRemaining - 1);
            OnMovesChanged.Invoke(_movesRemaining);
            Debug.Log($"[GameManager] Move used. Remaining: {_movesRemaining}");
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
