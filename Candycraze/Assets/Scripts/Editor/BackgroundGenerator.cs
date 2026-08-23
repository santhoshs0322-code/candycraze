// ============================================================
// BackgroundGenerator.cs (EDITOR ONLY)
// CandyCraze → Generate Background Images
// Creates premium gradient backgrounds with gem patterns.
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace CandyCraze.Editor
{
    public static class BackgroundGenerator
    {
        [MenuItem("CandyCraze/Generate Background Images")]
        public static void GenerateBackgrounds()
        {
            string folder = "Assets/Art/Backgrounds";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Art","Backgrounds");

            GenerateMainMenuBG(folder);
            GenerateGameBG(folder);
            GenerateLevelMapBG(folder);
            GenerateSplashBG(folder);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Backgrounds Generated",
                "4 premium background images created!\n\n" +
                "• MainMenuBG.png\n• GameBG.png\n• LevelMapBG.png\n• SplashBG.png\n\n" +
                "Assign them in your scene canvases.",
                "Done");
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
                imp.spritePixelsPerUnit = 100;
                imp.maxTextureSize = 1024;
                imp.textureCompression = TextureImporterCompression.Compressed;
                imp.SaveAndReimport();
            }
        }

        // ── Main Menu Background ─────────────────────────────
        // Deep purple → midnight blue gradient with floating gems
        static void GenerateMainMenuBG(string folder)
        {
            int w=540, h=960;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y=0; y<h; y++)
            for (int x=0; x<w; x++)
            {
                float u = x/(float)w, v = y/(float)h;
                // Gradient: dark purple bottom → midnight blue top
                Color c = Color.Lerp(
                    new Color(0.04f,0.01f,0.10f),
                    new Color(0.06f,0.04f,0.20f), v);

                // Diagonal light sweep
                float sweep = Mathf.Sin((u + v) * 3.14f) * 0.04f;
                c += new Color(sweep, sweep*0.5f, sweep*2f, 0f);

                // Star field
                float hash = Fract(Mathf.Sin(u*127.1f + v*311.7f) * 43758.5f);
                if (hash > 0.985f) {
                    float star = (hash-0.985f)/0.015f;
                    c = Color.Lerp(c, Color.white, star * 0.8f);
                }

                // Floating gem glow spots
                Color gemGlow = GemGlow(u, v, w, h);
                c = Color.Lerp(c, gemGlow, gemGlow.a * 0.3f);
                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            Save(tex, $"{folder}/MainMenuBG.png");
        }

        // ── Game Background ──────────────────────────────────
        // Dark board-friendly background
        static void GenerateGameBG(string folder)
        {
            int w=540, h=960;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y=0; y<h; y++)
            for (int x=0; x<w; x++)
            {
                float u = x/(float)w, v = y/(float)h;
                // Very dark, almost black - gems should pop
                Color c = Color.Lerp(
                    new Color(0.03f,0.01f,0.08f),
                    new Color(0.05f,0.03f,0.12f), v);

                // Subtle grid pattern
                float gx = Mathf.Abs(Fract(u * 8f) - 0.5f);
                float gy = Mathf.Abs(Fract(v * 8f) - 0.5f);
                float grid = Mathf.Max(0f, 1f - Mathf.Min(gx,gy) * 20f);
                c += new Color(0f, 0f, grid * 0.03f, 0f);

                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            Save(tex, $"{folder}/GameBG.png");
        }

        // ── Level Map Background ─────────────────────────────
        static void GenerateLevelMapBG(string folder)
        {
            int w=540, h=960;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y=0; y<h; y++)
            for (int x=0; x<w; x++)
            {
                float u = x/(float)w, v = y/(float)h;
                // Deep blue → purple
                Color c = Color.Lerp(
                    new Color(0.03f,0.01f,0.12f),
                    new Color(0.08f,0.03f,0.22f), v);

                // Nebula effect
                float n1 = Mathf.PerlinNoise(u*3f, v*3f);
                float n2 = Mathf.PerlinNoise(u*6f+0.5f, v*6f+0.5f);
                Color nebula = new Color(n1*0.05f, n2*0.02f, n1*0.08f, 0f);
                c += nebula;

                // Path dots
                float path = Mathf.Abs(Mathf.Sin(u*Mathf.PI*2f + v*4f)) * 0.5f;
                if (path > 0.48f && path < 0.52f)
                    c += new Color(0.02f, 0.02f, 0.05f, 0f);

                // Stars
                float hash = Fract(Mathf.Sin(x*0.127f + y*0.311f) * 437.58f);
                if (hash > 0.982f) c = Color.Lerp(c, Color.white, 0.6f);

                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            Save(tex, $"{folder}/LevelMapBG.png");
        }

        // ── Splash Background ─────────────────────────────────
        static void GenerateSplashBG(string folder)
        {
            int w=540, h=960;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y=0; y<h; y++)
            for (int x=0; x<w; x++)
            {
                float u = x/(float)w, v = y/(float)h;
                // Radial glow from center
                float dx = u - 0.5f, dy = v - 0.5f;
                float r = Mathf.Sqrt(dx*dx + dy*dy);

                Color c = Color.Lerp(
                    new Color(0.15f,0.05f,0.35f),  // bright centre
                    new Color(0.02f,0.01f,0.08f),  // dark edges
                    r * 1.5f);

                // Gold shimmer ring
                float ring = Mathf.Abs(r - 0.35f);
                if (ring < 0.02f)
                {
                    float t = 1f - ring/0.02f;
                    c = Color.Lerp(c, new Color(1f,0.8f,0.1f), t * 0.15f);
                }

                // Stars
                float hash = Fract(Mathf.Sin(x*0.173f + y*0.421f) * 631.5f);
                if (hash > 0.988f) c = Color.Lerp(c, Color.white, 0.9f);

                // Rainbow gem decorations
                float ga = Mathf.Atan2(dy, dx);
                float gr = r;
                if (gr > 0.15f && gr < 0.22f)
                {
                    Color gem = Color.HSVToRGB((ga/(Mathf.PI*2f)+1f)%1f, 0.9f, 1f);
                    float pt = Mathf.Abs(Mathf.Sin(ga * 6f));
                    c = Color.Lerp(c, gem, pt * 0.12f);
                }

                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            Save(tex, $"{folder}/SplashBG.png");
        }

        static Color GemGlow(float u, float v, int w, int h)
        {
            // Place 6 gem glow spots
            Vector2[] spots = {
                new Vector2(0.15f,0.8f), new Vector2(0.85f,0.75f),
                new Vector2(0.2f, 0.3f), new Vector2(0.8f, 0.35f),
                new Vector2(0.5f, 0.9f), new Vector2(0.5f, 0.15f)
            };
            Color[] cols = {
                new Color(1f,0.2f,0.2f), new Color(0.2f,0.4f,1f),
                new Color(0.2f,1f,0.3f), new Color(0.8f,0.2f,1f),
                new Color(1f,0.8f,0.1f), new Color(0.8f,0.9f,1f)
            };

            Color result = Color.clear;
            for (int i=0; i<spots.Length; i++)
            {
                float dx = u - spots[i].x, dy = v - spots[i].y;
                float d = Mathf.Sqrt(dx*dx + dy*dy*2.25f); // squish vertically
                float glow = Mathf.Max(0f, 1f - d/0.15f);
                glow = glow * glow;
                result = Color.Lerp(result, cols[i], glow * 0.6f);
                result.a = Mathf.Max(result.a, glow * 0.4f);
            }
            return result;
        }

        static float Fract(float x) => x - Mathf.Floor(x);
    }
}
#endif
