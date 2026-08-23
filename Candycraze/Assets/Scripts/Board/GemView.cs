// ============================================================
// GemView.cs
// Visual representation of one gem on the board.
// Handles: sprite, animations, idle glow, selection,
//          spawn pop, destroy shatter, move tweening.
// ============================================================

using System.Collections;
using UnityEngine;

namespace CandyCraze
{
    public enum GemSpecialType
    {
        None, LineBlast, AreaBomb, ColorCrystal
    }

    [RequireComponent(typeof(SpriteRenderer))]
    public class GemView : MonoBehaviour
    {
        // ── Data ─────────────────────────────────────────────
        public int            GemTypeID   { get; private set; }
        public GemSpecialType SpecialType { get; private set; }
        public int            Row         { get; set; }
        public int            Col         { get; set; }
        public bool           IsMoving    { get; private set; }
        public bool           IsMatched   { get; set; }

        // ── Components ───────────────────────────────────────
        private SpriteRenderer _sr;
        private SpriteRenderer _highlightSr;
        private SpriteRenderer _glowSr;
        private GemDefinition  _def;

        // ── Animation handles ─────────────────────────────────
        private Coroutine _moveCoroutine;
        private Coroutine _idleCoroutine;
        private Coroutine _spawnCoroutine;

        // ── Fallback sprite (white square) ───────────────────
        private static Sprite _fallback;

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            _sr = GetComponent<SpriteRenderer>();
            var hl = transform.Find("Highlight");
            if (hl) _highlightSr = hl.GetComponent<SpriteRenderer>();
            var gl = transform.Find("Glow");
            if (gl) _glowSr = gl.GetComponent<SpriteRenderer>();
        }

        // ── Initialise ───────────────────────────────────────

        public void Initialise(GemDefinition def, int row, int col,
                               GemSpecialType special = GemSpecialType.None)
        {
            _def        = def;
            GemTypeID   = def.GemTypeID;
            SpecialType = special;
            Row = row; Col = col;
            IsMatched = false;

            // Apply sprite
            _sr.sprite = def.NormalSprite != null ? def.NormalSprite : GetFallback();
            _sr.color  = Color.white;

            // Set glow to gem colour (invisible until selected)
            if (_glowSr != null)
            {
                _glowSr.sprite = _sr.sprite;
                _glowSr.color  = new Color(def.GemColor.r, def.GemColor.g,
                                           def.GemColor.b, 0f);
            }
            if (_highlightSr != null)
            {
                _highlightSr.sprite = _sr.sprite;
                _highlightSr.color  = new Color(1f,1f,1f,0f);
            }

            // Special visual
            ApplySpecialTint();

            // Spawn animation
            transform.localScale = Vector3.zero;
            if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
            _spawnCoroutine = StartCoroutine(SpawnAnim());

            // Start idle glow after spawn
            StartCoroutine(StartIdleAfterSpawn());
        }

        // ── Selection ────────────────────────────────────────

        public void SetHighlight(bool on)
        {
            if (_idleCoroutine != null) { StopCoroutine(_idleCoroutine); _idleCoroutine = null; }

            if (on)
            {
                // Pulsing glow when selected
                _idleCoroutine = StartCoroutine(SelectPulse());
                transform.localScale = Vector3.one * 1.12f;
                HapticFeedback.Light();
            }
            else
            {
                // Return to idle
                transform.localScale = Vector3.one;
                if (_glowSr != null) _glowSr.color = new Color(
                    _glowSr.color.r, _glowSr.color.g, _glowSr.color.b, 0f);
                if (_highlightSr != null) _highlightSr.color = new Color(1f,1f,1f,0f);
                _idleCoroutine = StartCoroutine(IdleGlow());
            }
        }

        // ── Movement ─────────────────────────────────────────

        public void MoveTo(Vector3 target, float duration, System.Action onDone = null)
        {
            if (_moveCoroutine != null) StopCoroutine(_moveCoroutine);
            _moveCoroutine = StartCoroutine(MoveRoutine(target, duration, onDone));
        }

        public void SnapTo(Vector3 pos)
        {
            if (_moveCoroutine != null) { StopCoroutine(_moveCoroutine); _moveCoroutine = null; }
            transform.position = pos;
            IsMoving = false;
        }

        // ── Destruction ──────────────────────────────────────

        public void PlayDestroyAnimation(System.Action onDone = null)
        {
            // Spawn particle
            if (_def?.DestroyParticlePrefab != null)
                Instantiate(_def.DestroyParticlePrefab, transform.position, Quaternion.identity);

            // Code particle fallback
            ParticleManager.Instance?.PlayMatchBurst(transform.position,
                _def?.GemColor ?? Color.white);

            StartCoroutine(DestroyAnim(onDone));
        }

        // ── Animations ───────────────────────────────────────

        private IEnumerator SpawnAnim()
        {
            float dur = 0.28f, elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                // Spring overshoot
                float s = t < 0.7f
                    ? Mathf.Lerp(0f, 1.15f, t / 0.7f)
                    : Mathf.Lerp(1.15f, 1f, (t - 0.7f) / 0.3f);
                transform.localScale = Vector3.one * s;
                yield return null;
            }
            transform.localScale = Vector3.one;
            _spawnCoroutine = null;
        }

        private IEnumerator StartIdleAfterSpawn()
        {
            yield return new WaitForSeconds(0.3f);
            if (_idleCoroutine != null) StopCoroutine(_idleCoroutine);
            _idleCoroutine = StartCoroutine(IdleGlow());
        }

        // Subtle breathing glow on idle
        private IEnumerator IdleGlow()
        {
            if (_glowSr == null) yield break;
            Color gc = new Color(_def?.GemColor.r ?? 1f,
                                 _def?.GemColor.g ?? 1f,
                                 _def?.GemColor.b ?? 1f);
            float offset = Random.Range(0f, Mathf.PI * 2f); // stagger timing
            while (true)
            {
                float alpha = (Mathf.Sin(Time.time * 1.8f + offset) + 1f) * 0.5f * 0.18f;
                _glowSr.color = new Color(gc.r, gc.g, gc.b, alpha);
                yield return null;
            }
        }

        // Bright pulsing glow when selected
        private IEnumerator SelectPulse()
        {
            if (_glowSr == null && _highlightSr == null) yield break;
            Color gc = new Color(_def?.GemColor.r ?? 1f,
                                 _def?.GemColor.g ?? 1f,
                                 _def?.GemColor.b ?? 1f);
            while (true)
            {
                float t = (Mathf.Sin(Time.time * 6f) + 1f) * 0.5f;
                if (_glowSr != null)
                    _glowSr.color = new Color(gc.r, gc.g, gc.b, Mathf.Lerp(0.3f, 0.7f, t));
                if (_highlightSr != null)
                    _highlightSr.color = new Color(1f,1f,1f, Mathf.Lerp(0f, 0.25f, t));
                transform.localScale = Vector3.one * Mathf.Lerp(1.08f, 1.16f, t);
                yield return null;
            }
        }

        private IEnumerator MoveRoutine(Vector3 target, float duration, System.Action onDone)
        {
            IsMoving = true;
            Vector3 start = transform.position;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                // Ease out cubic
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                transform.position = Vector3.Lerp(start, target, eased);
                yield return null;
            }
            transform.position = target;
            IsMoving = false;
            _moveCoroutine = null;
            onDone?.Invoke();
        }

        private IEnumerator DestroyAnim(System.Action onDone)
        {
            // Stop idle
            if (_idleCoroutine != null) { StopCoroutine(_idleCoroutine); _idleCoroutine = null; }

            float dur = 0.22f, elapsed = 0f;
            Vector3 startScale = transform.localScale;
            Color   startColor = _sr.color;

            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                // Scale up briefly then collapse
                float s = t < 0.3f
                    ? Mathf.Lerp(1f, 1.25f, t / 0.3f)
                    : Mathf.Lerp(1.25f, 0f, (t - 0.3f) / 0.7f);
                transform.localScale = Vector3.one * s;
                _sr.color = startColor.WithAlpha(1f - t);
                if (_glowSr != null) _glowSr.color = _glowSr.color.WithAlpha(0f);
                yield return null;
            }

            onDone?.Invoke();
            if (ObjectPool.Instance != null)
                ObjectPool.Instance.ReturnToPool(gameObject);
            else
                Destroy(gameObject);
        }

        // ── Special tint ─────────────────────────────────────

        private void ApplySpecialTint()
        {
            switch (SpecialType)
            {
                case GemSpecialType.LineBlast:
                    // Gold ring overlay
                    _sr.color = Color.white;
                    StartCoroutine(SpecialPulse(new Color(1f, 0.85f, 0.1f)));
                    break;
                case GemSpecialType.AreaBomb:
                    _sr.color = Color.white;
                    StartCoroutine(SpecialPulse(new Color(1f, 0.4f, 0.1f)));
                    break;
                case GemSpecialType.ColorCrystal:
                    _sr.color = Color.white;
                    StartCoroutine(RainbowPulse());
                    break;
            }
        }

        private IEnumerator SpecialPulse(Color glowColor)
        {
            while (true)
            {
                float t = (Mathf.Sin(Time.time * 4f) + 1f) * 0.5f;
                _sr.color = Color.Lerp(Color.white, glowColor, t * 0.5f);
                transform.localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, t);
                if (_glowSr != null)
                    _glowSr.color = new Color(glowColor.r, glowColor.g, glowColor.b,
                                              Mathf.Lerp(0.3f, 0.8f, t));
                yield return null;
            }
        }

        private IEnumerator RainbowPulse()
        {
            float hue = 0f;
            while (true)
            {
                hue = (hue + Time.deltaTime * 0.8f) % 1f;
                Color rainbow = Color.HSVToRGB(hue, 0.8f, 1f);
                _sr.color = Color.Lerp(Color.white, rainbow, 0.6f);
                if (_glowSr != null) _glowSr.color = rainbow.WithAlpha(0.6f);
                float s = 1f + Mathf.Sin(Time.time * 5f) * 0.06f;
                transform.localScale = Vector3.one * s;
                yield return null;
            }
        }

        // ── Fallback sprite ───────────────────────────────────
        private static Sprite GetFallback()
        {
            if (_fallback != null) return _fallback;
            var tex = new Texture2D(64, 64);
            var pixels = new Color[64*64];

            // Draw a rounded square
            for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                float u = x/63f*2f-1f, v = y/63f*2f-1f;
                float r = Mathf.Pow(Mathf.Abs(u), 4f) + Mathf.Pow(Mathf.Abs(v), 4f);
                float a = Mathf.Clamp01((0.7f - r) / 0.1f);
                float bright = 1f - Mathf.Sqrt(u*u+v*v)*0.4f;
                pixels[y*64+x] = new Color(bright,bright,bright,a);
            }
            tex.SetPixels(pixels); tex.Apply();
            _fallback = Sprite.Create(tex, new Rect(0,0,64,64), new Vector2(0.5f,0.5f), 64f);
            return _fallback;
        }
    }
}
