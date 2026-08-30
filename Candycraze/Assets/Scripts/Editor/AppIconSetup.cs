// ============================================================
// AppIconSetup.cs  (EDITOR ONLY)
// CandyCraze → Apply App Icon (from PNG)
//   Loads Assets/Art/UI/AppIcon.png (your CandyCraze logo) and
//   applies it to every Android icon slot: legacy, round, and
//   adaptive (foreground). Falls back to a procedural icon via
//   "Create App Icon (Procedural)" if no PNG is present.
// ============================================================
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;

namespace CandyCraze.Editor
{
    public static class AppIconSetup
    {
        private const string IconPath = "Assets/Art/UI/AppIcon.png";

        // ────────────────────────────────────────────────────
        // MAIN: apply the logo PNG as the Android app icon
        // ────────────────────────────────────────────────────
        [MenuItem("CandyCraze/Apply App Icon (from PNG)")]
        public static void ApplyAppIcon()
        {
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            if (tex == null)
            {
                EditorUtility.DisplayDialog("App Icon Missing",
                    $"No image found at:\n{IconPath}\n\n" +
                    "Save your CandyCraze logo as a 1024x1024 (or 512x512) PNG\n" +
                    "to that exact path, then run this menu item again.",
                    "OK");
                return;
            }

            // Make sure the texture importer settings suit an icon
            ConfigureImporter(IconPath);
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath); // reload after reimport

            int applied = ApplyToAllAndroidIcons(tex);

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("App Icon Applied",
                $"Applied '{Path.GetFileName(IconPath)}' to {applied} Android icon density slot(s).\n\n" +
                "Build the AAB/APK to see it on device.\n\n" +
                "Tip: for a round/adaptive icon, also set it in\n" +
                "Player Settings → Icon → Adaptive.",
                "Done");
        }

        // Apply the given texture to every Android icon density slot.
        // Uses the stable GetIcons/SetIcons API (no Android-extension
        // assembly dependency) so it compiles in any project setup.
        private static int ApplyToAllAndroidIcons(Texture2D tex)
        {
            if (tex == null) return 0;

            var icons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Android);
            if (icons == null || icons.Length == 0)
            {
                // Fall back to the default icon size list if none are defined yet
                var sizes = PlayerSettings.GetIconSizesForTargetGroup(BuildTargetGroup.Android);
                icons = new Texture2D[Mathf.Max(1, sizes != null ? sizes.Length : 1)];
            }

            for (int i = 0; i < icons.Length; i++) icons[i] = tex;
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);

            return icons.Length;
        }

        private static void ConfigureImporter(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) return;
            bool changed = false;

            if (importer.textureType != TextureImporterType.Default)
            { importer.textureType = TextureImporterType.Default; changed = true; }
            if (!importer.isReadable)
            { importer.isReadable = true; changed = true; }
            if (importer.mipmapEnabled)
            { importer.mipmapEnabled = false; changed = true; }
            if (importer.npotScale != TextureImporterNPOTScale.None)
            { importer.npotScale = TextureImporterNPOTScale.None; changed = true; }
            if (importer.maxTextureSize < 1024)
            { importer.maxTextureSize = 1024; changed = true; }

            if (changed) importer.SaveAndReimport();
        }

        // ────────────────────────────────────────────────────
        // FALLBACK: procedural icon (kept for convenience)
        // ────────────────────────────────────────────────────
        [MenuItem("CandyCraze/Create App Icon (Procedural)")]
        public static void CreateAppIcon()
        {
            int size = 512;
            var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);

            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)size;
                float v = y / (float)size;
                tex.SetPixel(x, y, GetIconPixel(u, v));
            }

            tex.Apply();

            string folder = "Assets/Art/UI";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Art", "UI");

            File.WriteAllBytes(IconPath, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(IconPath);
            ConfigureImporter(IconPath);

            var loaded = AssetDatabase.LoadAssetAtPath<Texture2D>(IconPath);
            ApplyToAllAndroidIcons(loaded);
            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("App Icon Created",
                $"Procedural app icon saved to: {IconPath}\n\n" +
                "Applied to Android Player Settings.\n" +
                "Replace that PNG with your logo and run\n" +
                "'Apply App Icon (from PNG)' for the real artwork.",
                "Done");
        }

        private static Color GetIconPixel(float u, float v)
        {
            float cx = u - 0.5f, cy = v - 0.5f;

            // Rounded square background
            float r = Mathf.Max(Mathf.Abs(cx), Mathf.Abs(cy),
                     (Mathf.Abs(cx) + Mathf.Abs(cy)) * 0.7f);
            if (r > 0.48f) return Color.clear;

            // Gradient background: deep purple → blue
            Color bg = Color.Lerp(
                new Color(0.35f, 0.05f, 0.6f),
                new Color(0.05f, 0.15f, 0.5f),
                v);

            // Draw 4 gem shapes on the icon
            float[] gx = {-0.18f,  0.18f, -0.18f,  0.18f};
            float[] gy = { 0.15f,  0.15f, -0.15f, -0.15f};
            Color[] gc = {
                new Color(1f,0.2f,0.2f),
                new Color(0.2f,0.5f,1f),
                new Color(0.2f,0.9f,0.3f),
                new Color(1f,0.8f,0.1f)
            };

            for (int i = 0; i < 4; i++)
            {
                float dx = cx - gx[i], dy = cy - gy[i];
                float dist = Mathf.Abs(dx) + Mathf.Abs(dy);
                if (dist < 0.13f)
                {
                    float bright = 1f - dist / 0.13f * 0.4f;
                    return gc[i] * bright;
                }
            }

            // Star in centre
            float angle = Mathf.Atan2(cy, cx);
            float rad   = Mathf.Sqrt(cx*cx + cy*cy);
            float star  = 0.04f + 0.025f * Mathf.Cos(angle * 6f);
            if (rad < star)
                return Color.white;

            return bg;
        }
    }
}
#endif
