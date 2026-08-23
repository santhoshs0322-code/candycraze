// ============================================================
// ObjectiveManager.cs
// Tracks progress toward each level objective and determines
// whether all objectives have been met.
// ============================================================

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace CandyCraze
{
    /// <summary>Runtime tracker for one objective.</summary>
    public class ObjectiveProgress
    {
        public ObjectiveData Data;
        public int           Current;
        public bool          IsComplete => Current >= Target;

        public int Target
        {
            get
            {
                return Data.Type switch
                {
                    ObjectiveType.ReachScore      => Data.TargetScore,
                    ObjectiveType.CollectGemType  => Data.TargetAmount,
                    ObjectiveType.ClearObstacles  => Data.TargetAmount,
                    _                             => 1
                };
            }
        }
    }

    public class ObjectiveManager : MonoBehaviour
    {
        // ── State ────────────────────────────────────────────
        private List<ObjectiveProgress> _objectives = new List<ObjectiveProgress>();
        private ScoreManager _scoreManager;

        // ── Events ───────────────────────────────────────────
        /// <summary>Fires when any objective's progress changes.</summary>
        public UnityEvent OnObjectivesUpdated = new UnityEvent();

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            _scoreManager = FindObjectOfType<ScoreManager>();

            if (_scoreManager != null)
                _scoreManager.OnScoreChanged.AddListener(OnScoreChanged);
        }

        private void OnDestroy()
        {
            if (_scoreManager != null)
                _scoreManager.OnScoreChanged.RemoveListener(OnScoreChanged);
        }

        // ── Public API ───────────────────────────────────────

        public void Initialise(LevelData level)
        {
            _objectives.Clear();

            if (level.Objectives == null) return;

            foreach (var data in level.Objectives)
            {
                _objectives.Add(new ObjectiveProgress { Data = data, Current = 0 });
            }

            Debug.Log($"[ObjectiveManager] Loaded {_objectives.Count} objective(s).");
            OnObjectivesUpdated.Invoke();
        }

        /// <summary>Called by BoardManager when a gem is matched.</summary>
        public void OnGemMatched(int gemTypeID)
        {
            bool changed = false;

            foreach (var obj in _objectives)
            {
                if (obj.IsComplete) continue;

                if (obj.Data.Type == ObjectiveType.CollectGemType &&
                    obj.Data.GemTypeID == gemTypeID)
                {
                    obj.Current++;
                    changed = true;
                }
            }

            if (changed) OnObjectivesUpdated.Invoke();
        }

        /// <summary>Called by BoardManager when an obstacle tile is cleared.</summary>
        public void OnObstacleCleared()
        {
            bool changed = false;
            foreach (var obj in _objectives)
            {
                if (!obj.IsComplete && obj.Data.Type == ObjectiveType.ClearObstacles)
                {
                    obj.Current++;
                    changed = true;
                }
            }
            if (changed) OnObjectivesUpdated.Invoke();
        }

        /// <returns>True when every objective is met.</returns>
        public bool AllObjectivesMet()
        {
            if (_objectives.Count == 0) return false;
            foreach (var obj in _objectives)
                if (!obj.IsComplete) return false;
            return true;
        }

        public List<ObjectiveProgress> GetAllObjectives() => _objectives;

        // ── Private ──────────────────────────────────────────
        private void OnScoreChanged(int newScore)
        {
            bool changed = false;
            foreach (var obj in _objectives)
            {
                if (!obj.IsComplete && obj.Data.Type == ObjectiveType.ReachScore)
                {
                    obj.Current = newScore;
                    changed = true;
                }
            }
            if (changed) OnObjectivesUpdated.Invoke();
        }
    }
}
