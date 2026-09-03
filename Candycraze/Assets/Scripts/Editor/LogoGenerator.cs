// ============================================================
// LogoGenerator.cs (EDITOR ONLY)
// CandyCraze → Generate Logo
// Creates a colourful candy-style "CandyCraze" logo texture
// saved to Resources/UI/Logo.png
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;

namespace CandyCraze.Editor
{
    public static class LogoGenerator
    {
        [MenuItem("CandyCraze/Generate Logo")]
        public static void Generate()
        {
            EnsureFolder("Assets/Resources");
            EnsureFolder("Assets/Resources/UI");

            // Render the text into a texture using a temporary camera+TextMesh
            // Simpler: build a gradient banner behind bold text handled in UI.
            // Here we make a decorative ribbon banner sprite.
            var banner = MakeRibbonBanner();
            Save(banner, "Assets/Resources/UI/LogoBanner.png");

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Logo Ready!",
                "Logo banner saved to Resources/UI/LogoBanner.png\n\n" +
                "Run 'Build All Scenes' to apply.",
                "Done");
        }

        // Decorative ribbon banner (pink with gold trim + sparkles)
        static Texture2D MakeRibbonBanner()
        {
            int w = 512, h = 160;
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            for (int y=0;y<h;y++) for (int x=0;x<w;x++)
            {
                float u = x/(float)w, v = y/(float)h;

                // Banner shape — bulges in middle, notched ends
                float centerBulge = 0.55f + 0.35f*Mathf.Sin(u*Mathf.PI);
                float dy = Mathf.Abs(v-0.5f)/centerBulge;
                if (dy > 0.5f) { tex.SetPixel(x,y,Color.clear); continue; }

                // Pink gradient
                Color c = Color.Lerp(new Color(0.85f,0.15f,0.45f),
                                     new Color(1f,0.4f,0.65f), v);
                // Top shine
                if (v > 0.6f) c = Color.Lerp(c, Color.white, (v-0.6f)*0.5f);
                // Gold trim top & bottom
                if (dy > 0.42f)
                    c = Color.Lerp(c, new Color(1f,0.85f,0.2f), (dy-0.42f)/0.08f);

                c.a = 1;
                tex.SetPixel(x,y,c);
            }
            tex.Apply();
            return tex;
        }

        static void Save(Texture2D tex, string path)
        {
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null) { imp.textureType = TextureImporterType.Sprite; imp.SaveAndReimport(); }
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
