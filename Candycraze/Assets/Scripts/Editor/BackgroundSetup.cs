// ============================================================
// BackgroundSetup.cs (EDITOR ONLY)
// CandyCraze → Generate Candy Backgrounds
// Creates candy-kingdom backgrounds saved to Resources/UI
// so all scenes can load them at runtime.
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace CandyCraze.Editor
{
    public static class BackgroundSetup
    {
        [MenuItem("CandyCraze/Generate Candy Backgrounds")]
        public static void Generate()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");
            string f = "Assets/Resources/UI";

            // Light, bright candy backgrounds for ALL pages.
            Save(LightCandyBackground(0), $"{f}/BG_Menu.png");
            Save(LightCandyBackground(1), $"{f}/BG_Map.png");
            Save(LightCandyBackground(2), $"{f}/BG_Game.png");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Backgrounds Ready!",
                "Candy backgrounds saved to Resources/UI/\n\n" +
                "BG_Menu, BG_Map, BG_Game\n\n" +
                "Run 'Build All Scenes' to apply.",
                "Done");
        }

        static int W = 540, H = 960;

        // ── Background: deep candy-night blue with cartoon cloud shadows ─
        // Much darker than sky-blue so colorful gems POP, with soft dark
        // cloud-shadow shapes for cartoon depth (like Candy Crush's deep
        // blue/purple game board background).
        static Texture2D LightCandyBackground(int variant)
        {
            // Deep candy blue — NOT bright, comfortable for long play.
            Color top    = new Color(0.08f, 0.12f, 0.30f); // deep navy
            Color bottom = new Color(0.15f, 0.22f, 0.45f); // slightly lighter navy

            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            float seedX = variant * 3.7f;
            float seedY = variant * 2.1f;

            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x / (float)W;
                float v = y / (float)H;

                Color c = Color.Lerp(bottom, top, v);

                // Cartoon cloud shadows — large soft darker patches give depth
                float n1 = Mathf.PerlinNoise(u * 2.5f + seedX, v * 1.8f + seedY);
                float n2 = Mathf.PerlinNoise(u * 4f + seedX + 5f, v * 3f + seedY + 3f);
                float cloud = n1 * 0.55f + n2 * 0.45f;

                // Light patches — subtle soft glow areas
                if (cloud > 0.56f)
                {
                    float t = Mathf.Clamp01((cloud - 0.56f) / 0.3f);
                    Color glow = new Color(0.18f, 0.28f, 0.55f);
                    c = Color.Lerp(c, glow, t * 0.35f);
                }
                // Dark shadow patches — cartoon depth
                else if (cloud < 0.35f)
                {
                    float t = Mathf.Clamp01((0.35f - cloud) / 0.25f);
                    Color shadow = new Color(0.04f, 0.06f, 0.16f);
                    c = Color.Lerp(c, shadow, t * 0.4f);
                }

                // Very faint stars/sparkle (1 in 200 pixels, subtle)
                float sh = Fract(Mathf.Sin(x * 0.17f + y * 0.43f + variant) * 631f);
                if (sh > 0.995f && v > 0.5f)
                    c = Color.Lerp(c, new Color(0.6f, 0.7f, 1f), 0.3f);

                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            return tex;
        }

        // ── Candy kingdom background ─────────────────────────
        static Texture2D CandyBackground(bool menu)
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            for (int y = 0; y < H; y++)
            for (int x = 0; x < W; x++)
            {
                float u = x/(float)W, v = y/(float)H;
                Color c;

                if (v > 0.45f)
                {
                    // Sky — blue gradient
                    float t = (v - 0.45f) / 0.55f;
                    c = Color.Lerp(new Color(0.45f,0.78f,1f), new Color(0.30f,0.68f,1f), t);
                    // Clouds
                    float cloud = Cloud(u*3f + (menu?0:5f), v*2f);
                    if (cloud > 0.62f) c = Color.Lerp(c, Color.white, (cloud-0.62f)*2.5f);
                    // Sun glow top-right
                    float sd = Mathf.Sqrt((u-0.82f)*(u-0.82f)+(v-0.9f)*(v-0.9f));
                    if (sd < 0.25f) c = Color.Lerp(c, new Color(1f,0.97f,0.75f), (0.25f-sd)/0.25f*0.5f);
                }
                else
                {
                    // Candy ground — green with sweet path
                    float t = v / 0.45f;
                    c = Color.Lerp(new Color(0.22f,0.58f,0.15f), new Color(0.38f,0.75f,0.22f), t);
                    // Winding path
                    float pathX = 0.5f + Mathf.Sin(v*10f)*0.12f;
                    float pd = Mathf.Abs(u - pathX);
                    if (pd < 0.12f)
                        c = Color.Lerp(c, new Color(0.90f,0.72f,0.42f), (0.12f-pd)/0.12f*0.85f);
                    // Candy dots
                    float ch = Fract(Mathf.Sin(x*0.31f+y*0.71f)*437f);
                    if (ch > 0.975f)
                        c = Color.Lerp(c, Color.HSVToRGB(ch*9f%1f,0.85f,0.95f), 0.8f);
                }

                // Rainbow arc upper-right
                float rx=u-0.72f, ry=v-0.55f;
                float rr=Mathf.Sqrt(rx*rx+ry*ry);
                if (rr>0.26f && rr<0.34f && ry>0)
                    c = Color.Lerp(c, Color.HSVToRGB((rr-0.26f)/0.08f,0.7f,1f), 0.45f);

                // Candy castle silhouette (left, menu only)
                if (menu && u<0.28f && v>0.35f && v<0.72f)
                {
                    if (Castle(u,v)) c = Color.Lerp(c, new Color(0.92f,0.62f,0.82f), 0.55f);
                }

                // Sparkle stars in sky
                float sh=Fract(Mathf.Sin(x*0.17f+y*0.43f)*631f);
                if (sh>0.994f && v>0.6f) c=Color.Lerp(c,new Color(1f,1f,0.8f),0.9f);

                c.a=1; tex.SetPixel(x,y,c);
            }
            tex.Apply();
            return tex;
        }

        // ── Game background — dark so gems pop ───────────────
        static Texture2D GameBackground()
        {
            var tex = new Texture2D(W, H, TextureFormat.RGBA32, false);
            for (int y=0;y<H;y++) for (int x=0;x<W;x++)
            {
                float u=x/(float)W, v=y/(float)H;
                Color c = Color.Lerp(new Color(0.10f,0.05f,0.22f), new Color(0.16f,0.08f,0.32f), v);
                // Soft radial vignette glow center
                float dx=u-0.5f, dy=v-0.55f;
                float d=Mathf.Sqrt(dx*dx+dy*dy);
                c = Color.Lerp(c, new Color(0.22f,0.12f,0.40f), Mathf.Max(0,0.4f-d));
                // Faint candy dots
                float ch=Fract(Mathf.Sin(x*0.23f+y*0.51f)*331f);
                if (ch>0.992f) c=Color.Lerp(c, Color.HSVToRGB(ch*7f%1f,0.6f,0.7f), 0.3f);
                c.a=1; tex.SetPixel(x,y,c);
            }
            tex.Apply();
            return tex;
        }

        static float Cloud(float x, float y)
            => Mathf.PerlinNoise(x,y)*0.5f + Mathf.PerlinNoise(x*2+3,y*2+3)*0.3f + Mathf.PerlinNoise(x*4+7,y*4+7)*0.2f;

        static bool Castle(float u, float v)
        {
            float ru=(u)/0.28f, rv=(v-0.35f)/0.37f;
            if (ru>0.3f && ru<0.7f && rv>0f && rv<0.85f) return true;
            if (ru>0.1f && ru<0.35f && rv>0.2f && rv<0.7f) return true;
            if (rv>0.85f && Mathf.Abs(ru-0.5f)<(1f-rv)*1.2f) return true;
            return false;
        }

        static float Fract(float x) => x - Mathf.Floor(x);

        static void Save(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.maxTextureSize = 1024;
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
