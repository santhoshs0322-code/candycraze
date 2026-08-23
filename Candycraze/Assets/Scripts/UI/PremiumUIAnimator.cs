// ============================================================
// PremiumUIAnimator.cs
// Handles all premium UI animations:
// - Button press effects (scale + color flash)
// - Panel slide-in animations
// - Particle sparkles on buttons
// - Loading screen animation
// - Number count-up
// ============================================================

using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace CandyCraze
{
    public class PremiumUIAnimator : MonoBehaviour
    {
        public static PremiumUIAnimator Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }

        // ── Button Press ─────────────────────────────────────
        public void AnimateButtonPress(GameObject btn)
        {
            if (btn == null) return;
            StartCoroutine(BtnPressRoutine(btn.transform));
        }

        private IEnumerator BtnPressRoutine(Transform t)
        {
            Vector3 orig = t.localScale;
            float dur = 0.1f, elapsed = 0f;

            // Squish down
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1f, 0.88f, elapsed / dur);
                t.localScale = orig * s;
                yield return null;
            }
            // Bounce back
            elapsed = 0f;
            while (elapsed < dur * 1.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(0.88f, 1.05f, elapsed / (dur * 1.5f));
                t.localScale = orig * s;
                yield return null;
            }
            // Settle
            elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float s = Mathf.Lerp(1.05f, 1f, elapsed / dur);
                t.localScale = orig * s;
                yield return null;
            }
            t.localScale = orig;
        }

        // ── Panel Slide In ───────────────────────────────────
        public void SlideIn(GameObject panel, SlideDirection dir = SlideDirection.Up)
        {
            if (panel == null) return;
            panel.SetActive(true);
            StartCoroutine(SlideInRoutine(panel.GetComponent<RectTransform>(), dir));
        }

        public enum SlideDirection { Up, Down, Left, Right }

        private IEnumerator SlideInRoutine(RectTransform rt, SlideDirection dir)
        {
            if (rt == null) yield break;
            Vector2 target = rt.anchoredPosition;
            Vector2 start  = dir switch {
                SlideDirection.Up    => target + Vector2.down * 1200f,
                SlideDirection.Down  => target + Vector2.up   * 1200f,
                SlideDirection.Left  => target + Vector2.right* 800f,
                SlideDirection.Right => target + Vector2.left * 800f,
                _ => target
            };

            float dur = 0.35f, elapsed = 0f;
            rt.anchoredPosition = start;

            // Also fade in
            var cg = rt.GetComponent<CanvasGroup>() ?? rt.gameObject.AddComponent<CanvasGroup>();
            cg.alpha = 0f;

            while (elapsed < dur)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = elapsed / dur;
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                rt.anchoredPosition = Vector2.Lerp(start, target, eased);
                cg.alpha = Mathf.Lerp(0f, 1f, t * 2f);
                yield return null;
            }
            rt.anchoredPosition = target;
            cg.alpha = 1f;
        }

        // ── Count Up Number ──────────────────────────────────
        public IEnumerator CountUp(Text txt, int from, int to, float duration)
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

        // ── Floating sparkle ─────────────────────────────────
        public void SpawnSparkles(Vector3 worldPos, Color color, int count = 5)
        {
            for (int i = 0; i < count; i++)
                StartCoroutine(SparkleRoutine(worldPos, color));
        }

        private IEnumerator SparkleRoutine(Vector3 pos, Color color)
        {
            var go = new GameObject("Sparkle");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 50;
            sr.color = color;

            // Tiny white diamond
            Texture2D tex = new Texture2D(8, 8);
            for (int y = 0; y < 8; y++)
            for (int x = 0; x < 8; x++)
            {
                float u = x / 7f * 2f - 1f, v = y / 7f * 2f - 1f;
                float d = Mathf.Abs(u) + Mathf.Abs(v);
                tex.SetPixel(x, y, d < 0.8f ? Color.white : Color.clear);
            }
            tex.Apply();
            sr.sprite = Sprite.Create(tex, new Rect(0,0,8,8), Vector2.one*0.5f, 8f);
            go.transform.localScale = Vector3.one * 0.2f;

            Vector3 vel = new Vector3(
                Random.Range(-1.5f, 1.5f),
                Random.Range(1f, 3f), 0f);

            float lifetime = Random.Range(0.4f, 0.8f), elapsed = 0f;
            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / lifetime;
                go.transform.position += vel * Time.deltaTime;
                vel.y -= Time.deltaTime * 3f;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.2f, 0f, t);
                sr.color = color.WithAlpha(1f - t);
                go.transform.Rotate(0f, 0f, 200f * Time.deltaTime);
                yield return null;
            }
            Destroy(go);
        }
    }
}
