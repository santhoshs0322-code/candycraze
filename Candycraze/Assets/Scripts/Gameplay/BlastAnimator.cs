// ============================================================
// BlastAnimator.cs
// Plays rich blast animations for every special piece type.
//
// LineBlast  → horizontal/vertical laser sweep
// AreaBomb   → expanding shockwave ring + debris
// ColorCrystal → rainbow spiral burst across board
//
// All effects are 100% procedural — no external assets needed.
// ============================================================

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace CandyCraze
{
    public class BlastAnimator : MonoBehaviour
    {
        public static BlastAnimator Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ════════════════════════════════════════════════════
        // PUBLIC API
        // ════════════════════════════════════════════════════

        /// <summary>Play the correct blast for a special gem type.</summary>
        public void PlayBlast(GemSpecialType type, Vector3 worldPos, Color gemColor,
                              List<GemView> affectedGems = null)
        {
            switch (type)
            {
                case GemSpecialType.LineBlast:
                    StartCoroutine(LineBlastAnim(worldPos, gemColor, affectedGems));
                    break;
                case GemSpecialType.AreaBomb:
                    StartCoroutine(AreaBombAnim(worldPos, gemColor, affectedGems));
                    break;
                case GemSpecialType.ColorCrystal:
                    StartCoroutine(ColorCrystalAnim(worldPos, gemColor, affectedGems));
                    break;
            }
        }

        /// <summary>Play a simple match destroy burst (non-special).</summary>
        public void PlayMatchDestroy(Vector3 worldPos, Color color, int comboLevel = 0)
        {
            StartCoroutine(MatchBurstAnim(worldPos, color, comboLevel));
        }

        // ════════════════════════════════════════════════════
        // LINE BLAST
        // ════════════════════════════════════════════════════

        IEnumerator LineBlastAnim(Vector3 origin, Color color,
                                   List<GemView> affected)
        {
            // 1. Flash the origin gem white
            yield return StartCoroutine(FlashAt(origin, color, 0.12f));

            // 2. Spawn horizontal laser beam
            var hBeam = SpawnBeam(origin, true, color);
            var vBeam = SpawnBeam(origin, false, color);

            // 3. Screen flash
            StartCoroutine(ScreenFlash(color, 0.18f));
            ScreenShake.Instance?.Shake(0.3f, 0.15f);
            HapticFeedback.Heavy();
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.SpecialPiece);

            // 4. Sweep beams outward
            yield return StartCoroutine(SweepBeams(hBeam, vBeam, origin, 0.35f));

            // 5. Burst particles along the swept path
            if (affected != null)
                foreach (var g in affected)
                    ParticleManager.Instance?.PlayMatchBurst(g.transform.position, color);

            // 6. Fade beams
            yield return StartCoroutine(FadeAndDestroy(hBeam, 0.15f));
            yield return StartCoroutine(FadeAndDestroy(vBeam, 0.15f));
        }

        // ════════════════════════════════════════════════════
        // AREA BOMB
        // ════════════════════════════════════════════════════

        IEnumerator AreaBombAnim(Vector3 origin, Color color,
                                  List<GemView> affected)
        {
            // 1. Charge-up: gem vibrates and grows
            yield return StartCoroutine(ChargeUp(origin, color, 0.3f));

            // 2. BOOM flash
            StartCoroutine(ScreenFlash(color, 0.25f));
            ScreenShake.Instance?.ShakeHeavy();
            HapticFeedback.Heavy();
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.SpecialPiece);

            // 3. Expanding shockwave rings (3 rings)
            for (int i = 0; i < 3; i++)
            {
                float delay = i * 0.07f;
                StartCoroutine(ShockwaveRing(origin, color, 1.5f + i * 0.8f, delay));
            }

            // 4. Debris particles outward
            SpawnDebris(origin, color, 20);

            // 5. Burst at each affected gem
            yield return new WaitForSeconds(0.1f);
            if (affected != null)
            {
                foreach (var g in affected)
                {
                    ParticleManager.Instance?.PlayComboBurst(g.transform.position, color);
                    yield return new WaitForSeconds(0.02f);
                }
            }
        }

        // ════════════════════════════════════════════════════
        // COLOR CRYSTAL
        // ════════════════════════════════════════════════════

        IEnumerator ColorCrystalAnim(Vector3 origin, Color color,
                                      List<GemView> affected)
        {
            // 1. Rainbow spiral from origin
            StartCoroutine(RainbowSpiral(origin, 0.5f));
            ScreenShake.Instance?.Shake(0.4f, 0.1f);
            HapticFeedback.Heavy();
            AudioManager.Instance?.PlaySFX(AudioManager.SFX.SpecialPiece);

            // 2. Lightning bolts shoot to each affected gem
            yield return new WaitForSeconds(0.1f);
            if (affected != null)
            {
                foreach (var g in affected)
                {
                    StartCoroutine(LightningBolt(origin, g.transform.position, color));
                    yield return new WaitForSeconds(0.03f);
                }
            }

            yield return new WaitForSeconds(0.2f);

            // 3. Big screen flash at end
            StartCoroutine(ScreenFlash(Color.white, 0.3f));

            // 4. Burst at every affected gem
            if (affected != null)
                foreach (var g in affected)
                    ParticleManager.Instance?.PlaySpecialBurst(g.transform.position,
                        Color.HSVToRGB(Random.value, 0.8f, 1f));
        }

        // ════════════════════════════════════════════════════
        // MATCH BURST
        // ════════════════════════════════════════════════════

        IEnumerator MatchBurstAnim(Vector3 pos, Color color, int combo)
        {
            // Scale pop on all gems in match
            float intensity = 1f + combo * 0.3f;
            SpawnBurstCircle(pos, color, 6 + combo * 2, intensity);

            if (combo >= 2)
            {
                StartCoroutine(ScreenFlash(color, 0.1f));
                ScreenShake.Instance?.ShakeLight();
            }
            yield return null;
        }

        // ════════════════════════════════════════════════════
        // VISUAL PRIMITIVES
        // ════════════════════════════════════════════════════

        // ── Laser Beam ───────────────────────────────────────

        GameObject SpawnBeam(Vector3 pos, bool horizontal, Color color)
        {
            var go   = new GameObject(horizontal ? "HBeam" : "VBeam");
            go.transform.position = pos;

            var sr   = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite();
            sr.sortingOrder = 20;

            // Thin line
            float len = horizontal
                ? Camera.main.orthographicSize * Camera.main.aspect * 2.2f
                : Camera.main.orthographicSize * 2.2f;

            go.transform.localScale = horizontal
                ? new Vector3(0f, 0.15f, 1f)   // start collapsed
                : new Vector3(0.15f, 0f, 1f);

            Color c = color; c.a = 0.9f;
            sr.color = c;

            return go;
        }

        IEnumerator SweepBeams(GameObject hBeam, GameObject vBeam,
                                Vector3 origin, float duration)
        {
            float boardW = 8f * Constants.CELL_SIZE;
            float boardH = 8f * Constants.CELL_SIZE;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float eased = 1f - Mathf.Pow(1f-t, 2f);

                if (hBeam != null)
                    hBeam.transform.localScale = new Vector3(boardW * eased, 0.18f, 1f);
                if (vBeam != null)
                    vBeam.transform.localScale = new Vector3(0.18f, boardH * eased, 1f);
                yield return null;
            }
        }

        // ── Shockwave Ring ───────────────────────────────────

        IEnumerator ShockwaveRing(Vector3 centre, Color color,
                                   float maxRadius, float startDelay)
        {
            if (startDelay > 0) yield return new WaitForSeconds(startDelay);

            var go  = new GameObject("Shockwave");
            go.transform.position = centre;
            var sr  = go.AddComponent<SpriteRenderer>();
            sr.sprite = CircleSprite();
            sr.sortingOrder = 18;

            float duration = 0.45f, elapsed = 0f;
            Color c = color;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                float radius = Mathf.Lerp(0f, maxRadius, t);
                float alpha  = Mathf.Lerp(0.8f, 0f, t);

                go.transform.localScale = Vector3.one * radius * 2f;
                c.a = alpha;
                sr.color = c;
                yield return null;
            }
            Destroy(go);
        }

        // ── Charge-up animation ──────────────────────────────

        IEnumerator ChargeUp(Vector3 pos, Color color, float duration)
        {
            // Spawn a growing glow orb
            var go = new GameObject("ChargeOrb");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CircleSprite();
            sr.sortingOrder = 19;
            Color c = color; c.a = 0.7f;
            sr.color = c;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t     = elapsed / duration;
                float scale = Mathf.Lerp(0.1f, 0.8f, t);
                float shake = Mathf.Sin(elapsed * 40f) * 0.05f * t;
                go.transform.localScale   = Vector3.one * scale;
                go.transform.localPosition = new Vector3(shake, shake, 0f);
                c.a = 0.4f + t * 0.4f;
                sr.color = c;
                yield return null;
            }
            Destroy(go);
        }

        // ── Debris particles ─────────────────────────────────

        void SpawnDebris(Vector3 origin, Color color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                StartCoroutine(DebrisParticle(origin, color,
                    Random.insideUnitCircle.normalized * Random.Range(2f, 5f)));
            }
        }

        IEnumerator DebrisParticle(Vector3 start, Color color, Vector2 velocity)
        {
            var go = new GameObject("Debris");
            go.transform.position = start;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = DotSprite();   // round particle (not a square)
            sr.sortingOrder = 19;
            Color c = color; c.a = 1f;
            sr.color = c;
            go.transform.localScale = Vector3.one * Random.Range(0.06f, 0.14f);

            float lifetime = Random.Range(0.3f, 0.6f);
            float elapsed  = 0f;
            Vector3 pos    = start;
            Vector3 vel    = new Vector3(velocity.x, velocity.y, 0f);

            while (elapsed < lifetime)
            {
                elapsed += Time.deltaTime;
                float t  = elapsed / lifetime;
                vel.y   -= Time.deltaTime * 8f;  // gravity
                pos     += vel * Time.deltaTime;
                go.transform.position   = pos;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.12f, 0.02f, t);
                c.a = 1f - t;
                sr.color = c;
                yield return null;
            }
            Destroy(go);
        }

        // ── Lightning bolt ───────────────────────────────────

        IEnumerator LightningBolt(Vector3 from, Vector3 to, Color color)
        {
            var go   = new GameObject("Lightning");
            var lr   = go.AddComponent<LineRenderer>();
            lr.material          = new Material(Shader.Find("Sprites/Default"));
            lr.startColor        = Color.white;
            lr.endColor          = color;
            lr.startWidth        = 0.08f;
            lr.endWidth          = 0.02f;
            lr.sortingOrder      = 22;
            lr.useWorldSpace     = true;

            int    segments = 8;
            float  duration = 0.15f;
            float  elapsed  = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t  = elapsed / duration;

                // Jagged lightning path
                var points = new Vector3[segments + 1];
                for (int i = 0; i <= segments; i++)
                {
                    float pct     = i / (float)segments;
                    Vector3 base_ = Vector3.Lerp(from, to, pct);
                    float jitter  = (1f - t) * 0.25f;
                    if (i > 0 && i < segments)
                        base_ += new Vector3(
                            Random.Range(-jitter, jitter),
                            Random.Range(-jitter, jitter), 0f);
                    points[i] = base_;
                }

                lr.positionCount = segments + 1;
                lr.SetPositions(points);

                Color c = Color.Lerp(Color.white, color, t);
                c.a = 1f - t;
                lr.startColor = c;
                lr.endColor   = c;
                yield return null;
            }
            Destroy(go);
        }

        // ── Rainbow spiral ───────────────────────────────────

        IEnumerator RainbowSpiral(Vector3 centre, float duration)
        {
            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;

                // Emit 3 particles per frame in a spiral
                for (int i = 0; i < 3; i++)
                {
                    float angle = elapsed * 720f + i * 120f;
                    float rad   = t * 4f;
                    Vector3 pos = centre + new Vector3(
                        Mathf.Cos(angle * Mathf.Deg2Rad) * rad,
                        Mathf.Sin(angle * Mathf.Deg2Rad) * rad, 0f);
                    Color c = Color.HSVToRGB((elapsed * 2f + i * 0.33f) % 1f, 1f, 1f);
                    StartCoroutine(TinyBurst(pos, c));
                }
                yield return null;
            }
        }

        IEnumerator TinyBurst(Vector3 pos, Color color)
        {
            var go = new GameObject("Tiny");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CircleSprite();
            sr.sortingOrder = 21;
            Color c = color; c.a = 0.9f;
            sr.color = c;
            go.transform.localScale = Vector3.one * 0.15f;

            float dur = 0.2f, elapsed = 0f;
            while (elapsed < dur)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / dur;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.15f, 0.4f, t);
                c.a = 1f - t;
                sr.color = c;
                yield return null;
            }
            Destroy(go);
        }

        // ── Screen Flash ─────────────────────────────────────

        IEnumerator ScreenFlash(Color color, float duration)
        {
            var go  = new GameObject("ScreenFlash");
            var cam = Camera.main;
            if (cam == null) yield break;

            go.transform.SetParent(cam.transform, false);
            go.transform.localPosition = new Vector3(0f, 0f, 1f);

            var sr  = go.AddComponent<SpriteRenderer>();
            sr.sprite = WhiteSprite();
            sr.sortingOrder = 100;

            // Cover whole screen
            float h = cam.orthographicSize * 2f;
            float w = h * cam.aspect;
            go.transform.localScale = new Vector3(w, h, 1f);

            Color c = color; c.a = 0.5f;
            sr.color = c;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                c.a = Mathf.Lerp(0.5f, 0f, elapsed / duration);
                sr.color = c;
                yield return null;
            }
            Destroy(go);
        }

        // ── Flash at position ────────────────────────────────

        IEnumerator FlashAt(Vector3 pos, Color color, float duration)
        {
            var go = new GameObject("FlashAt");
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CircleSprite();
            sr.sortingOrder = 22;
            Color c = Color.white; c.a = 1f;
            sr.color = c;
            go.transform.localScale = Vector3.one * 0.5f;

            float elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / duration;
                go.transform.localScale = Vector3.one * Mathf.Lerp(0.5f, 1.8f, t);
                c.a = 1f - t;
                sr.color = c;
                yield return null;
            }
            Destroy(go);
        }

        // ── Burst circle ─────────────────────────────────────

        void SpawnBurstCircle(Vector3 pos, Color color, int count, float speed)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = i / (float)count * 360f;
                Vector2 dir = new Vector2(
                    Mathf.Cos(angle * Mathf.Deg2Rad),
                    Mathf.Sin(angle * Mathf.Deg2Rad));
                StartCoroutine(DebrisParticle(pos, color, dir * speed));
            }
        }

        // ── Fade and destroy ─────────────────────────────────

        IEnumerator FadeAndDestroy(GameObject go, float duration)
        {
            if (go == null) yield break;
            var sr = go.GetComponent<SpriteRenderer>();
            if (sr == null) { Destroy(go); yield break; }

            float elapsed = 0f;
            Color start   = sr.color;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                if (sr == null) yield break;
                Color c = start;
                c.a = Mathf.Lerp(start.a, 0f, elapsed / duration);
                sr.color = c;
                yield return null;
            }
            if (go != null) Destroy(go);
        }

        // ════════════════════════════════════════════════════
        // SPRITE GENERATORS
        // ════════════════════════════════════════════════════

        static Sprite _whiteSprite;
        static Sprite _circleSprite;
        static Sprite _dotSprite;

        static Sprite WhiteSprite()
        {
            if (_whiteSprite != null) return _whiteSprite;
            var tex = new Texture2D(4, 4);
            for (int i = 0; i < 16; i++) tex.SetPixel(i%4, i/4, Color.white);
            tex.Apply();
            _whiteSprite = Sprite.Create(tex, new Rect(0,0,4,4), new Vector2(0.5f,0.5f), 4f);
            return _whiteSprite;
        }

        // Soft FILLED circle — used for round debris/burst particles so the
        // blast looks rounded (not square).
        static Sprite DotSprite()
        {
            if (_dotSprite != null) return _dotSprite;
            int size = 64;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x/(float)(size-1)*2f-1f;
                float v = y/(float)(size-1)*2f-1f;
                float r = Mathf.Sqrt(u*u+v*v);
                // solid to ~0.7, soft feather to the edge
                float a = r <= 0.7f ? 1f : Mathf.Clamp01((1f - r) / 0.3f);
                tex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            _dotSprite = Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), size);
            return _dotSprite;
        }

        static Sprite CircleSprite()
        {
            if (_circleSprite != null) return _circleSprite;
            int size = 64;
            var tex  = new Texture2D(size, size);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x/(float)(size-1)*2f-1f;
                float v = y/(float)(size-1)*2f-1f;
                float r = Mathf.Sqrt(u*u+v*v);
                float a = Mathf.Clamp01((1f-r)/0.15f);
                // Ring: bright edge, transparent centre
                float ring = Mathf.Abs(r - 0.75f);
                float ra   = Mathf.Clamp01((0.12f - ring)/0.12f);
                tex.SetPixel(x, y, new Color(1,1,1, Mathf.Max(a*0.3f, ra)));
            }
            tex.Apply();
            _circleSprite = Sprite.Create(tex, new Rect(0,0,size,size),
                                          new Vector2(0.5f,0.5f), size);
            return _circleSprite;
        }
    }
}
