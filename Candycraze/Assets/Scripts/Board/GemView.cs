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

        /// <summary>
        /// For a LineBlast special: true = clears its COLUMN (vertical bomb),
        /// false = clears its ROW (horizontal bomb). Set from the match shape.
        /// </summary>
        public bool           LineBlastVertical { get; set; }

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

            // Ensure SpriteRenderer exists (defensive)
            if (_sr == null) _sr = GetComponent<SpriteRenderer>();
            if (_sr == null) _sr = gameObject.AddComponent<SpriteRenderer>();

            // Apply sprite — the glossy sprite already has colour baked in
            if (def.NormalSprite != null)
            {
                _sr.sprite = def.NormalSprite;
                _sr.color  = Color.white;  // don't tint — sprite has the gem colour
            }
            else
            {
                _sr.sprite = GetFallback();
                _sr.color  = def.GemColor;
            }
            _sr.sortingOrder = 5;

            // Glow (optional)
            if (_glowSr != null)
            {
                _glowSr.sprite = _sr.sprite;
                _glowSr.color  = new Color(def.GemColor.r, def.GemColor.g, def.GemColor.b, 0f);
            }
            if (_highlightSr != null)
            {
                _highlightSr.sprite = _sr.sprite;
                _highlightSr.color  = new Color(1f,1f,1f,0f);
            }

            // Set scale to 1 immediately — visible right away
            _baseScale = Vector3.one;
            transform.localScale = _baseScale;

            // Show a marker for special pieces (line bomb / color bomb).
            RefreshSpecialOverlay();

            // Optional pop animation on top
            if (gameObject.activeInHierarchy)
            {
                if (_spawnCoroutine != null) StopCoroutine(_spawnCoroutine);
                _spawnCoroutine = StartCoroutine(SpawnAnim());
            }
        }

        // ── Special-piece visual marker ──────────────────────
        private GameObject _specialOverlay;
        // The gem's intended resting scale (1 for normal gems; adjusted for
        // special pieces so their PNG matches normal gem size). SpawnAnim and
        // other animations return to THIS instead of hard-coded Vector3.one.
        private Vector3 _baseScale = Vector3.one;

        /// <summary>
        /// Draws a clear marker on top of the gem so players can tell it is a
        /// special piece: ↔ / ↕ for line bombs, ✦ (rainbow) for a color bomb.
        /// Call again after LineBlastVertical is set so the arrow points right.
        /// </summary>
        public void RefreshSpecialOverlay()
        {
            if (_specialOverlay != null) { Destroy(_specialOverlay); _specialOverlay = null; }
            if (_sr == null) return;

            Sprite special = null;
            switch (SpecialType)
            {
                case GemSpecialType.ColorCrystal:
                    special = Resources.Load<Sprite>("Gems/ColorBomb");
                    break;
                case GemSpecialType.LineBlast:
                    // Vertical 4-match clears a COLUMN → vertical stripes.
                    // Horizontal 4-match clears a ROW → horizontal stripes.
                    special = Resources.Load<Sprite>(LineBlastVertical ? "Gems/Stripe_V" : "Gems/Stripe_H");
                    break;
                // AreaBomb keeps its normal gem sprite for now.
            }

            if (special == null) return;

            _sr.sprite = special;
            _sr.color  = Color.white;
            // Match the special sprite's on-screen size to a normal gem so it
            // isn't oversized when its PNG has different dimensions/padding.
            FitSpriteToNormalGemSize(special);
        }

        // Scales this gem so the special sprite renders the SAME world size as
        // a normal gem, regardless of the special PNG's pixels-per-unit.
        private void FitSpriteToNormalGemSize(Sprite special)
        {
            if (special == null) return;

            // Reference size = the normal gem sprite's world size (unscaled).
            float refSize = 1f;
            if (_def != null && _def.NormalSprite != null)
                refSize = Mathf.Max(_def.NormalSprite.bounds.size.x, _def.NormalSprite.bounds.size.y);
            else
                refSize = Constants.CELL_SIZE * 0.9f;

            float specSize = Mathf.Max(special.bounds.size.x, special.bounds.size.y);
            if (specSize <= 0.0001f) return;

            // Bump up a bit: the bomb PNGs have transparent padding, so the
            // visible candy looks smaller than the gems at a 1:1 match. This
            // factor makes them read as a comfortable MEDIUM size on all phones.
            const float SPECIAL_SCALE_BOOST = 1.45f;

            float scale = (refSize / specSize) * SPECIAL_SCALE_BOOST;
            _baseScale = new Vector3(scale, scale, 1f);
            transform.localScale = _baseScale;
        }

        // ── Selection ────────────────────────────────────────

        public void SetHighlight(bool on)
        {
            if (_idleCoroutine != null) { StopCoroutine(_idleCoroutine); _idleCoroutine = null; }

            if (on)
            {
                // Pulsing glow when selected
                _idleCoroutine = StartCoroutine(SelectPulse());
                transform.localScale = _baseScale * 1.12f;
                HapticFeedback.Light();
            }
            else
            {
                // Return to idle
                transform.localScale = _baseScale;
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
                // Spring overshoot — scale relative to the gem's base scale
                // so special pieces keep their (matched) size.
                float s = t < 0.7f
                    ? Mathf.Lerp(0f, 1.15f, t / 0.7f)
                    : Mathf.Lerp(1.15f, 1f, (t - 0.7f) / 0.3f);
                transform.localScale = _baseScale * s;
                yield return null;
            }
            transform.localScale = _baseScale;
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
                transform.localScale = _baseScale * Mathf.Lerp(1.08f, 1.16f, t);
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
                    // Candy Crush style color bomb — dark sphere with rainbow sprinkles
                    _sr.sprite = GetColorBombSprite();
                    _sr.color = Color.white;
                    transform.localScale = Vector3.one * 1.1f;  // slightly bigger
                    StartCoroutine(ColorBombSpin());
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

        // Color bomb spin + pulse — makes it clearly special
        private IEnumerator ColorBombSpin()
        {
            while (true)
            {
                // Slow rotation
                transform.Rotate(0f, 0f, 40f * Time.deltaTime);
                // Gentle pulse
                float s = 1.1f + Mathf.Sin(Time.time * 3f) * 0.08f;
                transform.localScale = Vector3.one * s;
                // Glow cycles rainbow
                if (_glowSr != null)
                {
                    Color rb = Color.HSVToRGB((Time.time * 0.3f) % 1f, 0.9f, 1f);
                    _glowSr.color = rb.WithAlpha(0.5f + Mathf.Sin(Time.time*4f)*0.2f);
                }
                yield return null;
            }
        }

        // ── Color Bomb Sprite (Candy Crush style) ────────────
        private static Sprite _colorBombSprite;
        private static Sprite GetColorBombSprite()
        {
            if (_colorBombSprite != null) return _colorBombSprite;

            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x/(float)(size-1)*2f-1f;
                float v = y/(float)(size-1)*2f-1f;
                float r = Mathf.Sqrt(u*u + v*v);

                if (r > 1f) { tex.SetPixel(x,y,Color.clear); continue; }

                // Dark sphere base (near black with shading)
                float light = Mathf.Clamp01(1f - (u*0.4f - v*0.5f + r*0.4f));
                Color c = new Color(0.08f, 0.08f, 0.12f) * (0.5f + light);

                // Rainbow sprinkles scattered on surface
                float angle = Mathf.Atan2(v, u);
                float hash = Fract(Mathf.Sin(x*12.9f + y*78.2f) * 43758.5f);
                if (hash > 0.88f && r < 0.85f)
                {
                    // Colored sprinkle dot
                    Color sprinkle = Color.HSVToRGB(Fract(hash * 6f), 0.9f, 1f);
                    c = sprinkle;
                }

                // Rainbow ring around the equator
                float ringDist = Mathf.Abs(r - 0.6f);
                if (ringDist < 0.12f)
                {
                    float hue = (angle / (Mathf.PI * 2f) + 1f) % 1f;
                    Color ring = Color.HSVToRGB(hue, 0.85f, 1f);
                    float ringA = 1f - ringDist / 0.12f;
                    c = Color.Lerp(c, ring, ringA * 0.7f);
                }

                // Bright specular highlight top-left
                float hl = Mathf.Max(0f, 1f - Mathf.Sqrt((u+0.35f)*(u+0.35f)+(v-0.4f)*(v-0.4f))*3f);
                c = Color.Lerp(c, Color.white, hl*hl*0.8f);

                // Soft edge
                float alpha = r < 0.92f ? 1f : Mathf.Clamp01((1f-r)/0.08f);
                c.r=Mathf.Clamp01(c.r); c.g=Mathf.Clamp01(c.g); c.b=Mathf.Clamp01(c.b);
                c.a = alpha;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            _colorBombSprite = Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), size);
            return _colorBombSprite;
        }

        private static float Fract(float x) => x - Mathf.Floor(x);

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
