// ============================================================
// CandyUIGenerator.cs (EDITOR ONLY)
// CandyCraze → Generate Candy UI Images
// Creates candy-style background, buttons, and UI elements
// procedurally — bright colors, gradients, cartoon look.
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace CandyCraze.Editor
{
    public static class CandyUIGenerator
    {
        [MenuItem("CandyCraze/Generate Candy UI Images")]
        public static void Generate()
        {
            string folder = "Assets/Art/UI";
            EnsureFolder(folder);

            GenerateMainMenuBG(folder);
            GeneratePlayButton(folder);
            GenerateBlueButton(folder);
            GeneratePurpleButton(folder);
            GenerateOrangeButton(folder);
            GenerateCardBG(folder);
            GenerateTitleBanner(folder);

            // Copy MainMenuBG to Resources so RuntimeUIBuilder can load it
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");
            AssetDatabase.CopyAsset($"{folder}/MainMenuBG.png", "Assets/Resources/UI/MainMenuBG.png");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Candy UI Generated!",
                "7 UI images created in Assets/Art/UI/\n" +
                "MainMenuBG also copied to Resources/UI/\n\n" +
                "Run Build All Scenes to apply.",
                "Done!");
        }

        // ════════════════════════════════════════════════════
        // MAIN MENU BACKGROUND — Candy Kingdom
        // ════════════════════════════════════════════════════
        static void GenerateMainMenuBG(string folder)
        {
            int w = 540, h = 960;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x / (float)w, v = y / (float)h;
                Color c;

                // Sky gradient: light blue top → deeper blue middle
                if (v > 0.45f)
                {
                    float t = (v - 0.45f) / 0.55f;
                    c = Color.Lerp(
                        new Color(0.30f, 0.75f, 1.00f),  // mid sky
                        new Color(0.40f, 0.82f, 1.00f),  // top sky
                        t);

                    // Clouds
                    float cloud = CloudNoise(u * 3f, v * 2f);
                    if (cloud > 0.6f)
                        c = Color.Lerp(c, Color.white, (cloud - 0.6f) * 2f);

                    // Sun glow top-right
                    float sunDist = Mathf.Sqrt((u-0.85f)*(u-0.85f) + (v-0.9f)*(v-0.9f));
                    if (sunDist < 0.2f)
                        c = Color.Lerp(c, new Color(1f,0.95f,0.7f), (0.2f-sunDist)/0.2f * 0.4f);
                }
                // Ground: candy grass with path
                else
                {
                    float t = v / 0.45f;
                    // Green grass base
                    c = Color.Lerp(
                        new Color(0.20f, 0.55f, 0.10f),  // darker green bottom
                        new Color(0.35f, 0.75f, 0.20f),  // lighter green
                        t);

                    // Sandy path in centre
                    float pathDist = Mathf.Abs(u - 0.5f);
                    float pathWidth = 0.15f + Mathf.Sin(v * 8f) * 0.03f;
                    if (pathDist < pathWidth)
                    {
                        float pt = 1f - pathDist / pathWidth;
                        Color pathCol = new Color(0.85f, 0.70f, 0.40f);
                        c = Color.Lerp(c, pathCol, pt * 0.8f);
                    }

                    // Candy dots on ground
                    float candyHash = Fract(Mathf.Sin(x * 0.31f + y * 0.71f) * 437f);
                    if (candyHash > 0.97f)
                    {
                        Color candy = Color.HSVToRGB(candyHash * 7f % 1f, 0.8f, 0.9f);
                        c = Color.Lerp(c, candy, 0.7f);
                    }
                }

                // Rainbow arc (upper right)
                float rx = u - 0.75f, ry = v - 0.55f;
                float rr = Mathf.Sqrt(rx*rx + ry*ry);
                if (rr > 0.25f && rr < 0.35f && ry > 0)
                {
                    float hue = (rr - 0.25f) / 0.1f;
                    Color rainbow = Color.HSVToRGB(hue, 0.7f, 1f);
                    c = Color.Lerp(c, rainbow, 0.5f);
                }

                // Candy castle silhouettes (left)
                if (u < 0.3f && v > 0.35f && v < 0.75f)
                {
                    float castleShape = CastleShape(u, v);
                    if (castleShape > 0)
                    {
                        Color castle = new Color(0.90f, 0.60f, 0.80f);
                        c = Color.Lerp(c, castle, castleShape * 0.6f);
                    }
                }

                // Lollipop (right side)
                float lx = u - 0.85f, ly = v - 0.4f;
                float lr = Mathf.Sqrt(lx*lx + ly*ly);
                if (lr < 0.08f)
                {
                    float angle = Mathf.Atan2(ly, lx);
                    Color lolly = Mathf.Sin(angle * 4f) > 0
                        ? new Color(1f, 0.3f, 0.5f)
                        : Color.white;
                    c = Color.Lerp(c, lolly, 0.8f);
                }
                // Lollipop stick
                if (Mathf.Abs(u - 0.85f) < 0.012f && v < 0.32f && v > 0.1f)
                    c = Color.Lerp(c, new Color(0.8f, 0.6f, 0.3f), 0.9f);

                // Stars / sparkles
                float starHash = Fract(Mathf.Sin(x*0.173f+y*0.421f)*631.5f);
                if (starHash > 0.995f && v > 0.6f)
                    c = Color.Lerp(c, new Color(1f,1f,0.7f), 0.9f);

                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            SaveSprite(tex, $"{folder}/MainMenuBG.png", 100);
        }

        // ════════════════════════════════════════════════════
        // PLAY BUTTON — Glossy Green
        // ════════════════════════════════════════════════════
        static void GeneratePlayButton(string folder)
        {
            int w = 512, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color top    = new Color(0.45f, 0.90f, 0.25f);
            Color bottom = new Color(0.20f, 0.65f, 0.10f);
            Color shine  = new Color(0.85f, 1.0f, 0.75f);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x/(float)w, v = y/(float)h;
                float roundedness = RoundedRect(u, v, 0.4f);
                if (roundedness <= 0) { tex.SetPixel(x,y,Color.clear); continue; }

                // Vertical gradient
                Color c = Color.Lerp(bottom, top, v);

                // Top shine band
                if (v > 0.6f)
                {
                    float shineT = (v - 0.6f) / 0.4f;
                    c = Color.Lerp(c, shine, shineT * 0.5f);
                }

                // Edge darkening
                c = Color.Lerp(c, bottom * 0.7f, (1f - roundedness) * 0.4f);

                // Candy stripe border
                float edge = 1f - roundedness;
                if (edge > 0.85f && edge < 1f)
                {
                    float stripe = Mathf.Sin((u + v) * 50f) > 0 ? 1f : 0f;
                    Color stripeCol = stripe > 0.5f ? Color.white : new Color(1f,0.3f,0.3f);
                    c = Color.Lerp(c, stripeCol, 0.6f);
                }

                c.a = Mathf.Clamp01(roundedness * 10f);
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            SaveSprite(tex, $"{folder}/PlayButton.png", 128);
        }

        // ════════════════════════════════════════════════════
        // BLUE BUTTON — Shots/Secondary
        // ════════════════════════════════════════════════════
        static void GenerateBlueButton(string folder)
        {
            GenerateGlossyButton(folder, "BlueButton",
                new Color(0.15f, 0.55f, 0.95f),
                new Color(0.08f, 0.35f, 0.75f),
                new Color(0.60f, 0.85f, 1.0f));
        }

        // ════════════════════════════════════════════════════
        // PURPLE BUTTON — Daily Gift
        // ════════════════════════════════════════════════════
        static void GeneratePurpleButton(string folder)
        {
            int w = 200, h = 200;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color top    = new Color(0.65f, 0.25f, 0.90f);
            Color bottom = new Color(0.40f, 0.10f, 0.65f);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x/(float)w, v = y/(float)h;
                float rnd = RoundedRect(u, v, 0.2f);
                if (rnd <= 0) { tex.SetPixel(x,y,Color.clear); continue; }
                Color c = Color.Lerp(bottom, top, v);
                if (v > 0.55f) c = Color.Lerp(c, top * 1.3f, (v-0.55f)/0.45f * 0.4f);
                c = Color.Lerp(c, bottom*0.6f, (1f-rnd)*0.3f);
                c.a = Mathf.Clamp01(rnd * 10f);
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            SaveSprite(tex, $"{folder}/PurpleButton.png", 100);
        }

        // ════════════════════════════════════════════════════
        // ORANGE BUTTON — Settings
        // ════════════════════════════════════════════════════
        static void GenerateOrangeButton(string folder)
        {
            int w = 200, h = 200;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            Color top    = new Color(1.0f, 0.75f, 0.15f);
            Color bottom = new Color(0.85f, 0.50f, 0.05f);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x/(float)w, v = y/(float)h;
                float rnd = RoundedRect(u, v, 0.2f);
                if (rnd <= 0) { tex.SetPixel(x,y,Color.clear); continue; }
                Color c = Color.Lerp(bottom, top, v);
                if (v > 0.55f) c = Color.Lerp(c, top * 1.2f, (v-0.55f)/0.45f * 0.4f);
                c = Color.Lerp(c, bottom*0.6f, (1f-rnd)*0.3f);
                c.a = Mathf.Clamp01(rnd * 10f);
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            SaveSprite(tex, $"{folder}/OrangeButton.png", 100);
        }

        // ════════════════════════════════════════════════════
        // CARD BACKGROUND — Level card
        // ════════════════════════════════════════════════════
        static void GenerateCardBG(string folder)
        {
            int w = 256, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x/(float)w, v = y/(float)h;
                float rnd = RoundedRect(u, v, 0.15f);
                if (rnd <= 0) { tex.SetPixel(x,y,Color.clear); continue; }

                Color c = Color.Lerp(
                    new Color(0.20f, 0.10f, 0.40f),
                    new Color(0.30f, 0.15f, 0.55f), v);

                // Gold border
                if (rnd < 0.12f)
                    c = Color.Lerp(c, new Color(1f,0.8f,0.1f), 0.7f);

                c.a = Mathf.Clamp01(rnd * 12f);
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            SaveSprite(tex, $"{folder}/CardBG.png", 128);
        }

        // ════════════════════════════════════════════════════
        // TITLE BANNER — Pink ribbon
        // ════════════════════════════════════════════════════
        static void GenerateTitleBanner(string folder)
        {
            int w = 512, h = 96;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x/(float)w, v = y/(float)h;

                // Banner shape (wider in middle, tapers at ends)
                float bannerH = 0.5f + 0.3f * Mathf.Sin(u * Mathf.PI);
                float dy = Mathf.Abs(v - 0.5f) / bannerH;
                if (dy > 0.5f) { tex.SetPixel(x,y,Color.clear); continue; }

                // Pink gradient
                Color c = Color.Lerp(
                    new Color(0.90f, 0.20f, 0.50f),
                    new Color(1.00f, 0.45f, 0.65f), v);

                // Shine
                if (v > 0.6f) c = Color.Lerp(c, Color.white, (v-0.6f)*0.3f);

                // Edge stars
                float starH = Fract(Mathf.Sin(x*0.1f)*99f);
                if (starH > 0.98f)
                    c = Color.Lerp(c, new Color(1f,1f,0.5f), 0.6f);

                c.a = 1f;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            SaveSprite(tex, $"{folder}/TitleBanner.png", 128);
        }

        // ════════════════════════════════════════════════════
        // GLOSSY BUTTON HELPER
        // ════════════════════════════════════════════════════
        static void GenerateGlossyButton(string folder, string name,
            Color top, Color bottom, Color shine)
        {
            int w = 512, h = 128;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);

            for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
            {
                float u = x/(float)w, v = y/(float)h;
                float rnd = RoundedRect(u, v, 0.4f);
                if (rnd <= 0) { tex.SetPixel(x,y,Color.clear); continue; }

                Color c = Color.Lerp(bottom, top, v);
                if (v > 0.6f) c = Color.Lerp(c, shine, (v-0.6f)/0.4f * 0.45f);
                c = Color.Lerp(c, bottom * 0.6f, (1f-rnd) * 0.35f);
                c.a = Mathf.Clamp01(rnd * 10f);
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            SaveSprite(tex, $"{folder}/{name}.png", 128);
        }

        // ════════════════════════════════════════════════════
        // UTILITY
        // ════════════════════════════════════════════════════
        static float RoundedRect(float u, float v, float radius)
        {
            float dx = Mathf.Max(0, Mathf.Abs(u-0.5f)*2f - (1f-radius));
            float dy = Mathf.Max(0, Mathf.Abs(v-0.5f)*2f - (1f-radius));
            float d  = Mathf.Sqrt(dx*dx + dy*dy) / radius;
            return 1f - Mathf.Clamp01(d);
        }

        static float CloudNoise(float x, float y)
        {
            return Mathf.PerlinNoise(x, y) * 0.5f +
                   Mathf.PerlinNoise(x*2f+5f, y*2f+5f) * 0.3f +
                   Mathf.PerlinNoise(x*4f+10f, y*4f+10f) * 0.2f;
        }

        static float CastleShape(float u, float v)
        {
            // Simple castle silhouette
            float relV = (v - 0.35f) / 0.4f;
            float relU = u / 0.3f;

            // Main tower
            if (relU > 0.3f && relU < 0.7f && relV > 0.0f) return 0.8f;
            // Side tower
            if (relU > 0.1f && relU < 0.35f && relV > 0.3f) return 0.6f;
            // Cone roofs
            if (relV > 0.8f)
            {
                float roofDist = Mathf.Abs(relU - 0.5f);
                if (roofDist < (1f - relV) * 0.8f) return 0.9f;
            }
            return 0f;
        }

        static float Fract(float x) => x - Mathf.Floor(x);

        static void SaveSprite(Texture2D tex, string path, int ppu)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spritePixelsPerUnit = ppu;
                imp.filterMode = FilterMode.Bilinear;
                imp.textureCompression = TextureImporterCompression.Compressed;
                imp.SaveAndReimport();
            }
        }

        static void EnsureFolder(string path)
        {
            if (!AssetDatabase.IsValidFolder(path))
            {
                string p = System.IO.Path.GetDirectoryName(path).Replace('\\','/');
                string f = System.IO.Path.GetFileName(path);
                AssetDatabase.CreateFolder(p, f);
            }
        }
    }
}
#endif
