// ============================================================
// WinScreenController.cs
// Animated win screen — star reveal, score count-up,
// coins display, next/replay buttons.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class WinScreenController : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private GameObject _panel;
        [SerializeField] private Text       _titleText;
        [SerializeField] private Text       _scoreText;
        [SerializeField] private Text       _coinsEarnedText;
        [SerializeField] private Image[]    _starImages;          // 3 stars
        [SerializeField] private Sprite     _starFilledSprite;
        [SerializeField] private Sprite     _starEmptySprite;

        [Header("Buttons")]
        [SerializeField] private Button _nextButton;
        [SerializeField] private Button _replayButton;
        [SerializeField] private Button _mapButton;

        [Header("Star Colors")]
        [SerializeField] private Color _starOnColor  = Color.yellow;
        [SerializeField] private Color _starOffColor = Color.grey;

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            if (_panel != null) _panel.SetActive(false);

            if (_nextButton   != null) _nextButton.onClick.AddListener(OnNext);
            if (_replayButton != null) _replayButton.onClick.AddListener(OnReplay);
            if (_mapButton    != null) _mapButton.onClick.AddListener(OnMap);
        }

        // ── Public API ───────────────────────────────────────

        public void Show(int score, int stars, int coinsEarned)
        {
            if (_panel == null) return;
            _panel.SetActive(true);
            StartCoroutine(AnimateWin(score, stars, coinsEarned));
        }

        // ── Animation ────────────────────────────────────────

        private IEnumerator AnimateWin(int score, int stars, int coinsEarned)
        {
            // Panel scale-in
            _panel.transform.localScale = Vector3.zero;
            yield return StartCoroutine(ScaleTo(_panel.transform, Vector3.one, 0.4f));

            // Count-up score
            if (_scoreText != null)
                yield return StartCoroutine(CountUpText(_scoreText, 0, score, 1.0f));

            // Reveal stars one by one
            if (_starImages != null)
            {
                for (int i = 0; i < _starImages.Length; i++)
                {
                    if (_starImages[i] == null) continue;
                    yield return new WaitForSeconds(0.25f);

                    bool filled = i < stars;
                    _starImages[i].color = filled ? _starOnColor : _starOffColor;

                    // Pop animation for earned stars
                    if (filled)
                        yield return StartCoroutine(PopScale(_starImages[i].transform));
                }
            }

            // Show coins
            if (_coinsEarnedText != null)
            {
                _coinsEarnedText.text = $"+{coinsEarned} Crystals";
                yield return StartCoroutine(FadeIn(_coinsEarnedText, 0.3f));
            }

            AudioManager.Instance?.PlaySFX(AudioManager.SFX.LevelWin);
        }

        // ── Button Callbacks ─────────────────────────────────

        private void OnNext()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            var lm = FindObjectOfType<LevelManager>();
            if (lm != null && lm.HasNextLevel())
            {
                lm.LoadNextLevel();
                GameManager.Instance?.StartLevel();
                if (_panel != null) _panel.SetActive(false);
            }
            else
            {
                SceneController.Instance?.GoToLevelMap();
            }
        }

        private void OnReplay()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            GameManager.Instance?.RestartLevel();
            if (_panel != null) _panel.SetActive(false);
        }

        private void OnMap()
        {
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.Button);
            SceneController.Instance?.GoToLevelMap();
        }

        // ── Utilities ────────────────────────────────────────

        private IEnumerator ScaleTo(Transform t, Vector3 target, float duration)
        {
            Vector3 start = t.localScale;
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float pct = elapsed / duration;
                // Overshoot spring
                float overshoot = Mathf.Sin(pct * Mathf.PI) * 0.1f;
                t.localScale = Vector3.Lerp(start, target * (1f + overshoot), pct);
                yield return null;
            }
            t.localScale = target;
        }

        private IEnumerator PopScale(Transform t)
        {
            float elapsed = 0f, dur = 0.3f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float s = 1f + Mathf.Sin(elapsed / dur * Mathf.PI) * 0.4f;
                t.localScale = Vector3.one * s;
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        private IEnumerator CountUpText(Text txt, int from, int to, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                int val = Mathf.RoundToInt(Mathf.Lerp(from, to, elapsed / duration));
                txt.text = val.ToString("N0");
                yield return null;
            }
            txt.text = to.ToString("N0");
        }

        private IEnumerator FadeIn(Text txt, float duration)
        {
            Color c = txt.color;
            txt.color = c.WithAlpha(0f);
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                txt.color = c.WithAlpha(elapsed / duration);
                yield return null;
            }
            txt.color = c.WithAlpha(1f);
        }
    }
}
