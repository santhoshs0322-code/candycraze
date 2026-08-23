// ============================================================
// HomeMenuFX.cs
// Drives all the home-screen "juice":
//   • Logo bounce (gentle continuous scale pulse)
//   • Floating candy decorations (bob + rotate + drift)
//   • Button press feedback (squish + click SFX)
//   • Periodic sparkle particles across the scene
//
// Registered elements are added by CandyHomeMenu after it builds
// the UI. Everything is pure UI-space so it works on any device.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

namespace CandyCraze
{
    public class HomeMenuFX : MonoBehaviour
    {
        struct Floater
        {
            public RectTransform rt;
            public Vector2 basePos;
            public float bobAmp;
            public float bobSpeed;
            public float phase;
            public float spin;
            public float driftAmp;
        }

        readonly List<Floater> _floaters = new List<Floater>();
        RectTransform _logo;
        Vector3 _logoBaseScale = Vector3.one;
        RectTransform _sparkleLayer;
        float _sparkleTimer;

        // ── Registration API (called by builder) ─────────────

        public void RegisterLogo(RectTransform logo)
        {
            _logo = logo;
            if (logo != null) _logoBaseScale = logo.localScale;
        }

        public void SetSparkleLayer(RectTransform layer) => _sparkleLayer = layer;

        public void RegisterFloater(RectTransform rt, float bobAmp, float bobSpeed, float spin, float driftAmp)
        {
            if (rt == null) return;
            _floaters.Add(new Floater
            {
                rt = rt,
                basePos = rt.anchoredPosition,
                bobAmp = bobAmp,
                bobSpeed = bobSpeed,
                phase = Random.Range(0f, Mathf.PI * 2f),
                spin = spin,
                driftAmp = driftAmp
            });
        }

        /// <summary>Adds press feedback + a click sound to a button.</summary>
        public void RegisterButton(Button btn)
        {
            if (btn == null) return;
            // Press animation only — the click SFX is played by each
            // button's own action handler to avoid double sounds.
            var target = btn.targetGraphic != null ? btn.targetGraphic.transform : btn.transform;
            btn.onClick.AddListener(() => StartCoroutine(Press(target)));
        }

        // ── Update loop ──────────────────────────────────────

        void Update()
        {
            float t = Time.unscaledTime;

            // Logo bounce
            if (_logo != null)
            {
                float s = 1f + Mathf.Sin(t * 2.2f) * 0.045f;
                _logo.localScale = _logoBaseScale * s;
                _logo.localRotation = Quaternion.Euler(0, 0, Mathf.Sin(t * 1.3f) * 1.5f);
            }

            // Floating candies
            for (int i = 0; i < _floaters.Count; i++)
            {
                var f = _floaters[i];
                if (f.rt == null) continue;
                float y = Mathf.Sin(t * f.bobSpeed + f.phase) * f.bobAmp;
                float x = Mathf.Cos(t * f.bobSpeed * 0.6f + f.phase) * f.driftAmp;
                f.rt.anchoredPosition = f.basePos + new Vector2(x, y);
                if (f.spin != 0f)
                    f.rt.localRotation = Quaternion.Euler(0, 0, t * f.spin + f.phase * 20f);
            }

            // Ambient sparkles
            if (_sparkleLayer != null)
            {
                _sparkleTimer -= Time.unscaledDeltaTime;
                if (_sparkleTimer <= 0f)
                {
                    _sparkleTimer = Random.Range(0.35f, 0.8f);
                    SpawnSparkle();
                }
            }
        }

        // ── Button press coroutine ───────────────────────────
        IEnumerator Press(Transform tr)
        {
            Vector3 orig = Vector3.one;
            float d = 0.09f, e = 0f;
            while (e < d) { e += Time.unscaledDeltaTime; tr.localScale = orig * Mathf.Lerp(1f, 0.9f, e / d); yield return null; }
            e = 0f;
            while (e < d * 1.6f) { e += Time.unscaledDeltaTime; tr.localScale = orig * Mathf.Lerp(0.9f, 1.06f, e / (d * 1.6f)); yield return null; }
            e = 0f;
            while (e < d) { e += Time.unscaledDeltaTime; tr.localScale = orig * Mathf.Lerp(1.06f, 1f, e / d); yield return null; }
            tr.localScale = orig;
        }

        // ── Sparkle ──────────────────────────────────────────
        void SpawnSparkle()
        {
            var go = new GameObject("Sparkle");
            go.transform.SetParent(_sparkleLayer, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            float w = _sparkleLayer.rect.width, h = _sparkleLayer.rect.height;
            rt.anchoredPosition = new Vector2(Random.Range(-w * 0.5f, w * 0.5f),
                                              Random.Range(-h * 0.5f, h * 0.5f));
            float sz = Random.Range(10f, 26f);
            rt.sizeDelta = new Vector2(sz, sz);
            var img = go.AddComponent<Image>();
            img.sprite = MenuArt.Star(Color.white, 48);
            img.raycastTarget = false;
            Color tint = Random.value < 0.5f ? Color.white : new Color(1f, 0.95f, 0.6f);
            StartCoroutine(SparkleLife(rt, img, tint));
        }

        IEnumerator SparkleLife(RectTransform rt, Image img, Color tint)
        {
            float life = Random.Range(0.6f, 1.1f), e = 0f;
            float spin = Random.Range(-180f, 180f);
            Vector2 start = rt.anchoredPosition;
            while (e < life)
            {
                e += Time.unscaledDeltaTime;
                float t = e / life;
                float scale = Mathf.Sin(t * Mathf.PI); // grow then shrink
                rt.localScale = Vector3.one * scale;
                rt.localRotation = Quaternion.Euler(0, 0, spin * t);
                rt.anchoredPosition = start + Vector2.up * (t * 24f);
                img.color = tint.WithAlpha(scale);
                yield return null;
            }
            Destroy(rt.gameObject);
        }
    }
}
