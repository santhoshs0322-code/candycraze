// ============================================================
// HUDController.cs
// In-game HUD — score, moves, objectives, booster buttons.
// Animated move warning when moves get low.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class HUDController : MonoBehaviour
    {
        [Header("Score")]
        [SerializeField] private Text  _scoreText;
        [SerializeField] private Text  _highScoreText;

        [Header("Moves")]
        [SerializeField] private Text  _movesText;
        [SerializeField] private Image _movesBackground;
        [SerializeField] private int   _lowMovesThreshold = 5;

        [Header("Objectives")]
        [SerializeField] private Text  _objectiveText;

        [Header("Level Info")]
        [SerializeField] private Text  _levelNameText;

        // ── Colours ──────────────────────────────────────────
        private static readonly Color _normalMovesColor = new Color(0.2f, 0.2f, 0.8f);
        private static readonly Color _lowMovesColor    = new Color(0.9f, 0.2f, 0.1f);

        private Coroutine _movesWarningCoroutine;

        // ────────────────────────────────────────────────────
        private void Start()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnMovesChanged.AddListener(OnMovesChanged);

            var sm = FindObjectOfType<ScoreManager>();
            if (sm != null) sm.OnScoreChanged.AddListener(OnScoreChanged);

            var om = FindObjectOfType<ObjectiveManager>();
            if (om != null) om.OnObjectivesUpdated.AddListener(OnObjectivesUpdated);

            var lm = FindObjectOfType<LevelManager>();
            if (lm != null && _levelNameText != null)
                _levelNameText.text = lm.CurrentLevel?.LevelName ?? "";

            // Init high score
            if (_highScoreText != null && lm?.CurrentLevel != null)
            {
                var entry = SaveManager.Instance?.Data.GetEntry(lm.CurrentLevel.LevelNumber);
                _highScoreText.text = $"Best: {(entry?.HighScore ?? 0):N0}";
            }
        }

        private void OnDestroy()
        {
            if (GameManager.Instance != null)
                GameManager.Instance.OnMovesChanged.RemoveListener(OnMovesChanged);
        }

        // ── Callbacks ────────────────────────────────────────

        private void OnScoreChanged(int score)
        {
            if (_scoreText != null) _scoreText.text = score.ToString("N0");
        }

        private void OnMovesChanged(int moves)
        {
            if (_movesText != null) _movesText.text = moves.ToString();

            if (moves <= _lowMovesThreshold)
            {
                if (_movesBackground != null)
                    _movesBackground.color = _lowMovesColor;

                if (_movesWarningCoroutine != null)
                    StopCoroutine(_movesWarningCoroutine);

                if (moves > 0)
                    _movesWarningCoroutine = StartCoroutine(PulseMovesWarning());
            }
            else
            {
                if (_movesBackground != null)
                    _movesBackground.color = _normalMovesColor;
            }
        }

        private void OnObjectivesUpdated()
        {
            if (_objectiveText == null) return;

            var om = FindObjectOfType<ObjectiveManager>();
            if (om == null) return;

            var sb = new System.Text.StringBuilder();
            foreach (var obj in om.GetAllObjectives())
            {
                int current = Mathf.Min(obj.Current, obj.Target);
                string tick = obj.IsComplete ? "✓ " : "";
                sb.AppendLine($"{tick}{obj.Data.Description}: {current}/{obj.Target}");
            }
            _objectiveText.text = sb.ToString().TrimEnd();
        }

        // ── Low Moves Pulse ──────────────────────────────────
        private IEnumerator PulseMovesWarning()
        {
            if (_movesText == null) yield break;
            float t = 0f;
            while (true)
            {
                t += Time.deltaTime * 3f;
                float scale = 1f + Mathf.Sin(t) * 0.15f;
                _movesText.transform.localScale = Vector3.one * scale;
                yield return null;
            }
        }
    }
}
