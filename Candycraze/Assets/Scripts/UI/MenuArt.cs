// ============================================================
// MenuArt.cs
// Procedural, 100% ORIGINAL art generator for the home screen.
// Everything is drawn from code into Texture2D at runtime — no
// external image files, no copyrighted assets.
//
// Provides: gradients, rounded rectangles, candy-striped borders,
// circles, rainbow arcs, clouds, stars, gumdrops, lollipops and
// UI icons (play / camera / gift / gear).
//
// Generated sprites are cached so repeated calls are cheap.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace CandyCraze
{
    public static class MenuArt
    {
        // Sprite cache keyed by a descriptive string.
        static readonly Dictionary<string, Sprite> _cache = new Dictionary<string, Sprite>();

        // ── Small internal drawing buffer ────────────────────
        class Buf
        {
            public Color[] px;
            public int w, h;
            public Buf(int w, int h)
            {
                this.w = w; this.h = h;
                px = new Color[w * h];
                for (int i = 0; i < px.Length; i++) px[i] = new Color(0, 0, 0, 0);
            }
            public Sprite ToSprite(float ppu = 100f)
            {
                var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
                tex.wrapMode = TextureWrapMode.Clamp;
                tex.filterMode = FilterMode.Bilinear;
                tex.SetPixels(px);
                tex.Apply();
                return Sprite.Create(tex, new Rect(0, 0, w, h),
                    new Vector2(0.5f, 0.5f), ppu, 0, SpriteMeshType.FullRect);
            }
        }

        // Alpha-blend a colour over an existing pixel.
        static void Blend(Buf b, int x, int y, Color c, float coverage)
        {
            if (coverage <= 0f || x < 0 || y < 0 || x >= b.w || y >= b.h) return;
            int i = y * b.w + x;
            Color dst = b.px[i];
            float a = c.a * coverage;
            float outA = a + dst.a * (1f - a);
            if (outA <= 0f) { b.px[i] = new Color(0, 0, 0, 0); return; }
            float r = (c.r * a + dst.r * dst.a * (1f - a)) / outA;
            float g = (c.g * a + dst.g * dst.a * (1f - a)) / outA;
            float bl = (c.b * a + dst.b * dst.a * (1f - a)) / outA;
            b.px[i] = new Color(r, g, bl, outA);
        }

        // Fill a layer using a coverage function (0..1) evaluated with
        // supersampling for smooth edges.
        static void Layer(Buf b, System.Func<float, float, float> coverage, Color col, int ss = 3)
        {
            float inv = 1f / ss;
            float half = inv * 0.5f;
            for (int y = 0; y < b.h; y++)
            for (int x = 0; x < b.w; x++)
            {
                float acc = 0f;
                for (int sy = 0; sy < ss; sy++)
                for (int sx = 0; sx < ss; sx++)
                {
                    float px = x + sx * inv + half;
                    float py = y + sy * inv + half;
                    acc += Mathf.Clamp01(coverage(px, py));
                }
                acc /= (ss * ss);
                if (acc > 0f) Blend(b, x, y, col, acc);
            }
        }

        // ── Signed distance for a rounded box (centered) ─────
        // Returns <0 inside, >0 outside. p relative to center.
        static float RoundedBoxSDF(float px, float py, float halfW, float halfH, float r)
        {
            float qx = Mathf.Abs(px) - (halfW - r);
            float qy = Mathf.Abs(py) - (halfH - r);
            float outside = Mathf.Sqrt(Mathf.Max(qx, 0f) * Mathf.Max(qx, 0f) +
                                       Mathf.Max(qy, 0f) * Mathf.Max(qy, 0f));
            float inside = Mathf.Min(Mathf.Max(qx, qy), 0f);
            return outside + inside - r;
        }

        // ════════════════════════════════════════════════════
        // PUBLIC FACTORY METHODS
        // ════════════════════════════════════════════════════

        /// <summary>Flat 4x4 white sprite tinted via Image.color.</summary>
        public static Sprite Solid()
        {
            const string k = "solid";
            if (_cache.TryGetValue(k, out var s)) return s;
            var b = new Buf(4, 4);
            for (int i = 0; i < b.px.Length; i++) b.px[i] = Color.white;
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        /// <summary>Vertical gradient (small, meant to be stretched).</summary>
        public static Sprite VGradient(Color top, Color bottom, int h = 256)
        {
            string k = $"vgrad_{top}_{bottom}_{h}";
            if (_cache.TryGetValue(k, out var s)) return s;
            var b = new Buf(4, h);
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                Color c = Color.Lerp(bottom, top, t);
                for (int x = 0; x < 4; x++) b.px[y * 4 + x] = c;
            }
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        /// <summary>Filled circle with soft edge.</summary>
        public static Sprite Circle(Color col, int size = 128)
        {
            string k = $"circle_{col}_{size}";
            if (_cache.TryGetValue(k, out var s)) return s;
            var b = new Buf(size, size);
            float r = size * 0.5f - 1f;
            float cx = size * 0.5f, cy = size * 0.5f;
            Layer(b, (x, y) =>
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                return Mathf.Clamp01(r - d + 0.5f);
            }, col, 2);
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        /// <summary>Circle with a lighter glossy highlight — gem / candy look.</summary>
        public static Sprite Gem(Color col, int size = 128)
        {
            string k = $"gem_{col}_{size}";
            if (_cache.TryGetValue(k, out var s)) return s;
            var b = new Buf(size, size);
            float r = size * 0.5f - 1f;
            float cx = size * 0.5f, cy = size * 0.5f;
            Color dark = col * 0.75f; dark.a = col.a;
            // base with subtle vertical shade
            Layer(b, (x, y) =>
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                return Mathf.Clamp01(r - d + 0.5f);
            }, col, 2);
            // bottom shade
            Layer(b, (x, y) =>
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                float inside = Mathf.Clamp01(r - d + 0.5f);
                float low = Mathf.Clamp01((cy - y) / (size * 0.5f));
                return inside * low * 0.5f;
            }, dark, 2);
            // glossy highlight top-left
            float hx = size * 0.36f, hy = size * 0.66f, hr = size * 0.20f;
            Layer(b, (x, y) =>
            {
                float d = Mathf.Sqrt((x - hx) * (x - hx) + (y - hy) * (y - hy));
                return Mathf.Clamp01(hr - d + 0.5f) * 0.55f;
            }, new Color(1, 1, 1, 1), 2);
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        /// <summary>Rounded rectangle, exact pixel size, with vertical shading + optional gloss.</summary>
        public static Sprite RoundedRect(int w, int h, int radius, Color fill, bool gloss = true)
        {
            string k = $"rr_{w}_{h}_{radius}_{fill}_{gloss}";
            if (_cache.TryGetValue(k, out var s)) return s;
            w = Mathf.Max(4, w); h = Mathf.Max(4, h);
            radius = Mathf.Clamp(radius, 1, Mathf.Min(w, h) / 2);
            var b = new Buf(w, h);
            float hw = w * 0.5f, hh = h * 0.5f;
            Color top = Color.Lerp(fill, Color.white, 0.12f); top.a = fill.a;
            Color bot = fill * 0.82f; bot.a = fill.a;
            // base body with top→bottom shade
            for (int y = 0; y < h; y++)
            {
                float t = y / (float)(h - 1);
                Color c = Color.Lerp(bot, top, t);
                for (int x = 0; x < w; x++)
                {
                    float d = RoundedBoxSDF(x + 0.5f - hw, y + 0.5f - hh, hw, hh, radius);
                    float cov = Mathf.Clamp01(0.5f - d);
                    if (cov > 0f) Blend(b, x, y, c, cov);
                }
            }
            if (gloss)
            {
                // top highlight band
                float bandH = h * 0.42f;
                Layer(b, (x, y) =>
                {
                    float d = RoundedBoxSDF(x - hw, y - hh, hw, hh, radius);
                    float inside = Mathf.Clamp01(0.5f - d);
                    float band = Mathf.Clamp01((y - (h - bandH)) / bandH);
                    return inside * band * 0.22f;
                }, Color.white, 2);
            }
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        /// <summary>Transparent-centre rounded ring painted with diagonal candy stripes.</summary>
        public static Sprite CandyBorder(int w, int h, int radius, float thickness,
            Color stripeA, Color stripeB, float stripeW = 14f)
        {
            string k = $"cb_{w}_{h}_{radius}_{thickness}_{stripeA}_{stripeB}_{stripeW}";
            if (_cache.TryGetValue(k, out var s)) return s;
            w = Mathf.Max(4, w); h = Mathf.Max(4, h);
            radius = Mathf.Clamp(radius, 1, Mathf.Min(w, h) / 2);
            var b = new Buf(w, h);
            float hw = w * 0.5f, hh = h * 0.5f;
            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float d = RoundedBoxSDF(x + 0.5f - hw, y + 0.5f - hh, hw, hh, radius);
                // ring band: distance in [-thickness, 0]
                float outer = Mathf.Clamp01(0.5f - d);
                float inner = Mathf.Clamp01(0.5f - (-(d + thickness)));
                float ring = Mathf.Clamp01(outer - (1f - inner));
                if (ring <= 0f) continue;
                float diag = (x + y) / stripeW;
                bool a = Mathf.Repeat(diag, 2f) < 1f;
                Blend(b, x, y, a ? stripeA : stripeB, ring);
            }
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        /// <summary>Soft fluffy cloud (a few merged blobs).</summary>
        public static Sprite Cloud(int w = 220, int h = 120)
        {
            string k = $"cloud_{w}_{h}";
            if (_cache.TryGetValue(k, out var s)) return s;
            var b = new Buf(w, h);
            // blob centres (x,y,r) in pixel space
            float[,] blobs =
            {
                { w * 0.28f, h * 0.42f, h * 0.34f },
                { w * 0.50f, h * 0.55f, h * 0.44f },
                { w * 0.70f, h * 0.44f, h * 0.36f },
                { w * 0.40f, h * 0.40f, h * 0.30f },
                { w * 0.60f, h * 0.40f, h * 0.30f },
            };
            Layer(b, (x, y) =>
            {
                float best = 0f;
                for (int i = 0; i < blobs.GetLength(0); i++)
                {
                    float dx = x - blobs[i, 0], dy = y - blobs[i, 1], r = blobs[i, 2];
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    best = Mathf.Max(best, Mathf.Clamp01(r - d + 0.5f));
                }
                return best;
            }, Color.white, 2);
            // faint bottom shadow tint for depth
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        /// <summary>Rainbow arc — concentric colour bands, transparent elsewhere.
        /// Centre is at the bottom-centre of the texture.</summary>
        public static Sprite Rainbow(int size = 512)
        {
            string k = $"rainbow_{size}";
            if (_cache.TryGetValue(k, out var s)) return s;
            var b = new Buf(size, size);
            Color[] bands =
            {
                MHex("#FF5D5D"), MHex("#FF9F45"), MHex("#FFE14D"),
                MHex("#5FD35F"), MHex("#4FA8F5"), MHex("#A06BE8"),
            };
            float cx = size * 0.5f, cy = 0f;
            float rOuter = size * 0.95f;
            float bandW = size * 0.055f;
            for (int i = 0; i < bands.Length; i++)
            {
                float ri = rOuter - bandW * (i + 1);
                float ro = rOuter - bandW * i;
                Color c = bands[i];
                Layer(b, (x, y) =>
                {
                    float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                    float band = Mathf.Clamp01(0.5f - Mathf.Abs(d - (ri + ro) * 0.5f) + (ro - ri) * 0.5f - 0.5f);
                    return band;
                }, c, 2);
            }
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        /// <summary>Five-point star.</summary>
        public static Sprite Star(Color col, int size = 96)
        {
            string k = $"star_{col}_{size}";
            if (_cache.TryGetValue(k, out var s)) return s;
            var b = new Buf(size, size);
            Vector2[] pts = StarPoints(size * 0.5f, size * 0.5f, size * 0.46f, size * 0.20f, 5, -Mathf.PI / 2f);
            Layer(b, (x, y) => PointInPoly(pts, x, y) ? 1f : 0f, col, 3);
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        /// <summary>Lollipop — striped candy disc on a stick.</summary>
        public static Sprite Lollipop(Color a, Color bcol, int size = 160)
        {
            string k = $"lolli_{a}_{bcol}_{size}";
            if (_cache.TryGetValue(k, out var s)) return s;
            var b = new Buf(size, size);
            float cx = size * 0.5f, cy = size * 0.58f, r = size * 0.36f;
            // stick
            Layer(b, (x, y) =>
            {
                float halfW = size * 0.045f;
                bool inX = Mathf.Abs(x - cx) < halfW;
                bool inY = y < cy && y > size * 0.04f;
                return (inX && inY) ? 1f : 0f;
            }, MHex("#FFF3E0"), 2);
            // candy disc base
            Layer(b, (x, y) =>
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                return Mathf.Clamp01(r - d + 0.5f);
            }, a, 2);
            // spiral-ish stripes (angular wedges)
            Layer(b, (x, y) =>
            {
                float d = Mathf.Sqrt((x - cx) * (x - cx) + (y - cy) * (y - cy));
                if (d > r) return 0f;
                float ang = Mathf.Atan2(y - cy, x - cx);
                float spiral = ang + d * 0.06f;
                bool on = Mathf.Repeat(spiral / (Mathf.PI / 3.5f), 2f) < 1f;
                return on ? Mathf.Clamp01(r - d + 0.5f) : 0f;
            }, bcol, 2);
            // gloss
            Layer(b, (x, y) =>
            {
                float hx = cx - r * 0.35f, hy = cy + r * 0.35f, hr = r * 0.35f;
                float d = Mathf.Sqrt((x - hx) * (x - hx) + (y - hy) * (y - hy));
                return Mathf.Clamp01(hr - d + 0.5f) * 0.5f;
            }, Color.white, 2);
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        // ── ICONS ────────────────────────────────────────────

        public static Sprite IconPlay(Color col, int size = 96)
        {
            string k = $"icoplay_{col}_{size}";
            if (_cache.TryGetValue(k, out var s)) return s;
            var b = new Buf(size, size);
            Vector2[] tri =
            {
                new Vector2(size * 0.30f, size * 0.22f),
                new Vector2(size * 0.30f, size * 0.78f),
                new Vector2(size * 0.78f, size * 0.50f),
            };
            Layer(b, (x, y) => PointInPoly(tri, x, y) ? 1f : 0f, col, 3);
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        public static Sprite IconCamera(Color col, int size = 96)
        {
            string k = $"icocam_{col}_{size}";
            if (_cache.TryGetValue(k, out var s)) return s;
            var b = new Buf(size, size);
            float hw = size * 0.5f, hh = size * 0.5f;
            // body
            Layer(b, (x, y) =>
            {
                float d = RoundedBoxSDF(x - hw, y - hh + size * 0.03f, size * 0.40f, size * 0.26f, size * 0.07f);
                return Mathf.Clamp01(0.5f - d);
            }, col, 2);
            // top viewfinder bump
            Layer(b, (x, y) =>
            {
                float d = RoundedBoxSDF(x - size * 0.62f, y - size * 0.74f, size * 0.12f, size * 0.08f, size * 0.03f);
                return Mathf.Clamp01(0.5f - d);
            }, col, 2);
            // lens outer (cut out by drawing background-hole colour = clear via erase)
            float lx = size * 0.5f, ly = size * 0.47f, lr = size * 0.20f;
            EraseCircle(b, lx, ly, lr);
            Layer(b, (x, y) =>
            {
                float d = Mathf.Sqrt((x - lx) * (x - lx) + (y - ly) * (y - ly));
                return Mathf.Clamp01(lr - d + 0.5f);
            }, col, 2);
            EraseCircle(b, lx, ly, lr * 0.55f);
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        public static Sprite IconGift(int size = 128)
        {
            string k = $"icogift_{size}";
            if (_cache.TryGetValue(k, out var s)) return s;
            var b = new Buf(size, size);
            Color box = MHex("#FFD54A");
            Color boxDark = MHex("#F5A623");
            Color ribbon = MHex("#E8433B");
            // box body
            Layer(b, (x, y) =>
            {
                float d = RoundedBoxSDF(x - size * 0.5f, y - size * 0.34f, size * 0.34f, size * 0.24f, size * 0.04f);
                return Mathf.Clamp01(0.5f - d);
            }, box, 2);
            // lid
            Layer(b, (x, y) =>
            {
                float d = RoundedBoxSDF(x - size * 0.5f, y - size * 0.63f, size * 0.40f, size * 0.09f, size * 0.03f);
                return Mathf.Clamp01(0.5f - d);
            }, boxDark, 2);
            // vertical ribbon
            Layer(b, (x, y) => Mathf.Abs(x - size * 0.5f) < size * 0.06f && y < size * 0.72f && y > size * 0.10f ? 1f : 0f, ribbon, 3);
            // bow
            Vector2[] bowL = { new Vector2(size * 0.5f, size * 0.72f), new Vector2(size * 0.30f, size * 0.90f), new Vector2(size * 0.30f, size * 0.70f) };
            Vector2[] bowR = { new Vector2(size * 0.5f, size * 0.72f), new Vector2(size * 0.70f, size * 0.90f), new Vector2(size * 0.70f, size * 0.70f) };
            Layer(b, (x, y) => PointInPoly(bowL, x, y) ? 1f : 0f, ribbon, 3);
            Layer(b, (x, y) => PointInPoly(bowR, x, y) ? 1f : 0f, ribbon, 3);
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        public static Sprite IconGear(Color col, int size = 112)
        {
            string k = $"icogear_{col}_{size}";
            if (_cache.TryGetValue(k, out var s)) return s;
            var b = new Buf(size, size);
            float cx = size * 0.5f, cy = size * 0.5f;
            float rOut = size * 0.42f, rIn = size * 0.30f, teeth = 8f;
            Layer(b, (x, y) =>
            {
                float dx = x - cx, dy = y - cy;
                float d = Mathf.Sqrt(dx * dx + dy * dy);
                float ang = Mathf.Atan2(dy, dx);
                float wave = Mathf.Cos(ang * teeth);
                float rr = Mathf.Lerp(rIn, rOut, Mathf.SmoothStep(0f, 1f, (wave + 1f) * 0.5f));
                return Mathf.Clamp01(rr - d + 0.5f);
            }, col, 3);
            EraseCircle(b, cx, cy, size * 0.14f);
            s = b.ToSprite();
            _cache[k] = s;
            return s;
        }

        // ════════════════════════════════════════════════════
        // HELPERS
        // ════════════════════════════════════════════════════
        static void EraseCircle(Buf b, float cx, float cy, float r)
        {
            for (int y = 0; y < b.h; y++)
            for (int x = 0; x < b.w; x++)
            {
                float d = Mathf.Sqrt((x + 0.5f - cx) * (x + 0.5f - cx) + (y + 0.5f - cy) * (y + 0.5f - cy));
                float cov = Mathf.Clamp01(r - d + 0.5f);
                if (cov > 0f)
                {
                    int i = y * b.w + x;
                    Color c = b.px[i];
                    c.a *= (1f - cov);
                    b.px[i] = c;
                }
            }
        }

        static Vector2[] StarPoints(float cx, float cy, float rOut, float rIn, int points, float startAng)
        {
            var list = new Vector2[points * 2];
            float step = Mathf.PI / points;
            for (int i = 0; i < points * 2; i++)
            {
                float r = (i % 2 == 0) ? rOut : rIn;
                float a = startAng + i * step;
                list[i] = new Vector2(cx + Mathf.Cos(a) * r, cy + Mathf.Sin(a) * r);
            }
            return list;
        }

        static bool PointInPoly(Vector2[] p, float x, float y)
        {
            bool inside = false;
            int n = p.Length;
            for (int i = 0, j = n - 1; i < n; j = i++)
            {
                if (((p[i].y > y) != (p[j].y > y)) &&
                    (x < (p[j].x - p[i].x) * (y - p[i].y) / (p[j].y - p[i].y) + p[i].x))
                    inside = !inside;
            }
            return inside;
        }

        static Color MHex(string h)
        {
            ColorUtility.TryParseHtmlString(h, out Color c);
            return c;
        }
    }
}
