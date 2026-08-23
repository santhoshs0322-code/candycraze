// ============================================================
// LoadingScreenController.cs
// Animated premium loading / splash screen.
// Attach to the Bootstrap canvas.
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class LoadingScreenController : MonoBehaviour
    {
        [Header("UI Elements")]
        [SerializeField] private Text  _titleText;
        [SerializeField] private Text  _taglineText;
        [SerializeField] private Image _logoGlow;
        [SerializeField] private Image _progressBar;
        [SerializeField] private Text  _loadingText;
        [SerializeField] private Image _bgGradient;

        private static readonly string[] _loadingDots = { "Loading.", "Loading..", "Loading..." };

        private void Start()
        {
            StartCoroutine(AnimateLoadingScreen());
        }

        private IEnumerator AnimateLoadingScreen()
        {
            // Set initial state
            if (_titleText    != null) { _titleText.transform.localScale    = Vector3.zero; }
            if (_taglineText  != null) { _taglineText.color                 = _taglineText.color.WithAlpha(0f); }
            if (_progressBar  != null) { _progressBar.fillAmount            = 0f; }

            // 1. Background colour cycle
            if (_bgGradient != null)
                StartCoroutine(CycleBgColor());

            // 2. Title pop in
            yield return new WaitForSeconds(0.3f);
            if (_titleText != null)
                yield return StartCoroutine(PopInText(_titleText.transform, 0.5f));

            // 3. Tagline fade in
            yield return new WaitForSeconds(0.2f);
            if (_taglineText != null)
                yield return StartCoroutine(FadeIn(_taglineText, 0.4f));

            // 4. Logo glow pulse
            if (_logoGlow != null)
                StartCoroutine(PulseGlow(_logoGlow));

            // 5. Progress bar fill
            yield return new WaitForSeconds(0.1f);
            if (_progressBar != null)
                yield return StartCoroutine(FillProgress(_progressBar, 0.8f));

            // 6. Loading dots
            if (_loadingText != null)
                StartCoroutine(LoadingDots(_loadingText));

            // 7. Hold
            yield return new WaitForSeconds(0.5f);
        }

        private IEnumerator PopInText(Transform t, float dur)
        {
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float p = elapsed / dur;
                // Overshoot spring
                float s = p < 0.7f
                    ? Mathf.Lerp(0f, 1.2f, p / 0.7f)
                    : Mathf.Lerp(1.2f, 1f, (p-0.7f) / 0.3f);
                t.localScale = Vector3.one * s;
                yield return null;
            }
            t.localScale = Vector3.one;
        }

        private IEnumerator FadeIn(Text txt, float dur)
        {
            float elapsed = 0f;
            Color c = txt.color;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                txt.color = c.WithAlpha(elapsed / dur);
                yield return null;
            }
            txt.color = c.WithAlpha(1f);
        }

        private IEnumerator FillProgress(Image bar, float dur)
        {
            float elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                bar.fillAmount = Mathf.Lerp(0f, 1f, elapsed / dur);
                yield return null;
            }
            bar.fillAmount = 1f;
        }

        private IEnumerator PulseGlow(Image glow)
        {
            while (true)
            {
                float t = (Mathf.Sin(Time.time * 2f) + 1f) * 0.5f;
                Color c = glow.color;
                glow.color = c.WithAlpha(Mathf.Lerp(0.3f, 0.9f, t));
                glow.transform.localScale = Vector3.one * Mathf.Lerp(0.95f, 1.05f, t);
                yield return null;
            }
        }

        private IEnumerator LoadingDots(Text txt)
        {
            int idx = 0;
            while (true)
            {
                txt.text = _loadingDots[idx % 3];
                idx++;
                yield return new WaitForSeconds(0.4f);
            }
        }

        private IEnumerator CycleBgColor()
        {
            Color[] colors = {
                new Color(0.07f,0.04f,0.18f),
                new Color(0.10f,0.04f,0.22f),
                new Color(0.07f,0.06f,0.20f),
            };
            int idx = 0;
            while (true)
            {
                Color from = colors[idx % colors.Length];
                Color to   = colors[(idx+1) % colors.Length];
                float elapsed = 0f, dur = 2f;
                while (elapsed < dur)
                {
                    elapsed += Time.deltaTime;
                    _bgGradient.color = Color.Lerp(from, to, elapsed / dur);
                    yield return null;
                }
                idx++;
            }
        }
    }
}
