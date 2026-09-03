// ============================================================
// ButtonSpriteGenerator.cs (EDITOR ONLY)
// CandyCraze → Generate Button Sprites
// Creates rounded glossy candy-style button sprites and a
// rounded panel sprite. Saved to Resources/UI so runtime
// code can load them with Resources.Load.
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace CandyCraze.Editor
{
    public static class ButtonSpriteGenerator
    {
        [MenuItem("CandyCraze/Generate Button Sprites")]
        public static void Generate()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");
            string f = "Assets/Resources/UI";

            // Rounded glossy buttons in various colors
            Save(RoundedButton(new Color(0.20f,0.75f,0.25f)), $"{f}/BtnGreen.png");
            Save(RoundedButton(new Color(0.20f,0.50f,0.95f)), $"{f}/BtnBlue.png");
            Save(RoundedButton(new Color(0.90f,0.25f,0.25f)), $"{f}/BtnRed.png");
            Save(RoundedButton(new Color(0.95f,0.65f,0.10f)), $"{f}/BtnGold.png");
            Save(RoundedButton(new Color(0.55f,0.25f,0.85f)), $"{f}/BtnPurple.png");
            Save(RoundedButton(new Color(0.30f,0.20f,0.45f)), $"{f}/BtnDark.png");

            // Rounded panel (for cards/panels)
            Save(RoundedPanel(new Color(0.15f,0.10f,0.30f)), $"{f}/Panel.png");
            Save(RoundedPanel(new Color(0.10f,0.06f,0.22f)), $"{f}/PanelDark.png");

            // Circle (for gems/icons backgrounds)
            Save(Circle(new Color(1f,1f,1f)), $"{f}/Circle.png");

            // Star shape (for victory screen + level cards)
            SaveStar(Star(new Color(1f,1f,1f)), $"{f}/Star.png");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Button Sprites Created!",
                "Rounded candy-style buttons + panels + star saved to Resources/UI/\n\n" +
                "Run 'Build All Scenes' to apply the new look.",
                "Done");
        }

        static int W = 512, H = 200;  // High res, taller for pill shape

        // ── Pill-shaped glossy candy button ──────────────────
        static Texture2D RoundedButton(Color baseCol)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            // FULL pill — radius = half the height (fully rounded ends)
            float radius = H * 0.5f;

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float dist = PillDist(x, y, W, H);
                if (dist > 1f) { tex.SetPixel(x,y,Color.clear); continue; }

                float vy = y / (float)H;

                // Deep 3D gradient
                Color c = Color.Lerp(baseCol * 0.55f, baseCol * 1.15f, vy);

                // Strong glossy top half (candy shine)
                if (vy > 0.5f)
                {
                    float shine = (vy - 0.5f) / 0.5f;
                    shine = shine * shine;
                    c = Color.Lerp(c, Color.white, shine * 0.55f);
                }

                // Dark bottom rim for 3D pop
                if (vy < 0.28f)
                {
                    float sh = (0.28f - vy) / 0.28f;
                    c = Color.Lerp(c, baseCol * 0.4f, sh * 0.55f);
                }

                // Thick glossy white rim around whole pill
                if (dist > 0.80f)
                {
                    float edge = (dist - 0.80f) / 0.20f;
                    Color rim = Color.Lerp(baseCol * 1.4f, Color.white, 0.6f);
                    c = Color.Lerp(c, rim, edge * 0.7f);
                }

                // Top specular streak (glass reflection)
                if (vy > 0.72f && vy < 0.92f)
                {
                    float streak = 1f - Mathf.Abs(vy - 0.82f) / 0.1f;
                    c = Color.Lerp(c, Color.white, streak * 0.3f);
                }

                c.r=Mathf.Clamp01(c.r); c.g=Mathf.Clamp01(c.g); c.b=Mathf.Clamp01(c.b);
                c.a = dist > 0.96f ? Mathf.Clamp01((1f - dist) / 0.04f) : 1f;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        // Pill distance: fully rounded left/right ends
        static float PillDist(int x, int y, int w, int h)
        {
            float r = h * 0.5f;
            float cy = h * 0.5f;
            float leftC = r, rightC = w - r;

            float dy = y - cy;
            if (x < leftC)
            {
                float dx = x - leftC;
                return Mathf.Sqrt(dx*dx + dy*dy) / r;
            }
            else if (x > rightC)
            {
                float dx = x - rightC;
                return Mathf.Sqrt(dx*dx + dy*dy) / r;
            }
            else
            {
                return Mathf.Abs(dy) / r;
            }
        }

        // ── Rounded panel ────────────────────────────────────
        static Texture2D RoundedPanel(Color baseCol)
        {
            int pw = 128, ph = 128;
            var tex = new Texture2D(pw, ph, TextureFormat.RGBA32, false);
            float radius = 28f;

            for (int y = 0; y < ph; y++)
            for (int x = 0; x < pw; x++)
            {
                float dist = RoundRectDist(x, y, pw, ph, radius);
                if (dist > 1f) { tex.SetPixel(x,y,Color.clear); continue; }
                Color c = baseCol;
                // Subtle top gradient
                float vy = y/(float)ph;
                c = Color.Lerp(baseCol*0.9f, baseCol*1.1f, vy);
                c.a = dist > 0.95f ? (1f-dist)/0.05f : 0.97f;
                tex.SetPixel(x,y,c);
            }
            tex.Apply();
            return tex;
        }

        // ── Circle ───────────────────────────────────────────
        static Texture2D Circle(Color col)
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y=0;y<size;y++) for (int x=0;x<size;x++)
            {
                float u=x/(float)(size-1)*2-1, v=y/(float)(size-1)*2-1;
                float r=Mathf.Sqrt(u*u+v*v);
                float a = r<0.9f?1f:Mathf.Clamp01((1f-r)/0.1f);
                var c=col; c.a=a;
                tex.SetPixel(x,y,c);
            }
            tex.Apply();
            return tex;
        }

        // ── Star (5-point glossy candy star) ─────────────────
        static Texture2D Star(Color col)
        {
            int size = 256;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float cx = size * 0.5f, cy = size * 0.5f;
            float outer = size * 0.46f;
            float inner = outer * 0.44f;   // deeper points for a crisp star
            int points = 5;

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float dx = x - cx;
                float dy = y - cy;
                float r  = Mathf.Sqrt(dx*dx + dy*dy);
                // angle, rotate so a point faces up
                float ang = Mathf.Atan2(dy, dx) + Mathf.PI * 0.5f;
                if (ang < 0) ang += Mathf.PI * 2f;

                // radius of the star edge at this angle
                float seg = Mathf.PI * 2f / points;
                float a = ang % seg;
                float t = a / seg;                 // 0..1 across a segment
                // triangle wave peaking at the outer point
                float tri = 1f - Mathf.Abs(t - 0.5f) * 2f;
                float edge = Mathf.Lerp(inner, outer, tri);

                if (r > edge)
                {
                    // soft anti-aliased outside
                    float aa = Mathf.Clamp01((edge - r) / 2.5f + 1f);
                    if (aa <= 0f) { tex.SetPixel(x, y, Color.clear); continue; }
                    var ec = col; ec.a = aa; tex.SetPixel(x, y, ec); continue;
                }

                // glossy vertical gradient + bright center
                float vy = y / (float)size;
                Color c = Color.Lerp(col * 0.75f, col, vy);
                float centerGlow = Mathf.Clamp01(1f - r / outer);
                c = Color.Lerp(c, Color.white, centerGlow * 0.35f);
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        // Save a plain (non-9-sliced) sprite
        static void SaveStar(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spriteBorder = Vector4.zero;
                imp.filterMode = FilterMode.Bilinear;
                imp.SaveAndReimport();
            }
        }

        // Distance function for rounded rectangle (0=center, 1=edge)
        static float RoundRectDist(int x, int y, int w, int h, float radius)
        {
            float cx = w * 0.5f, cy = h * 0.5f;
            float dx = Mathf.Abs(x - cx) - (cx - radius);
            float dy = Mathf.Abs(y - cy) - (cy - radius);
            dx = Mathf.Max(dx, 0);
            dy = Mathf.Max(dy, 0);
            float cornerDist = Mathf.Sqrt(dx*dx + dy*dy);
            return cornerDist / radius;
        }

        static void Save(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                // 9-slice: left/right borders = full radius (100) to keep round ends
                imp.spriteBorder = new Vector4(100, 90, 100, 90);
                imp.filterMode = FilterMode.Bilinear;
                imp.SaveAndReimport();
            }
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string p = Path.GetDirectoryName(path).Replace('\\','/');
                string fn = Path.GetFileName(path);
                AssetDatabase.CreateFolder(p, fn);
            }
        }
    }
}
#endif
