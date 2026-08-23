// ============================================================
// AppIconSetup.cs  (EDITOR ONLY)
// CandyCraze → Create App Icon
// Generates a procedural app icon texture.
// ============================================================
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;

namespace CandyCraze.Editor
{
    public static class AppIconSetup
    {
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

            string path = $"{folder}/AppIcon.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer != null)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }

            // Apply as Android icon
            var icons = PlayerSettings.GetIconsForTargetGroup(BuildTargetGroup.Android);
            var sprite = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (icons.Length > 0 && sprite != null)
            {
                for (int i = 0; i < icons.Length; i++)
                    icons[i] = sprite;
                PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Android, icons);
            }

            AssetDatabase.SaveAssets();

            EditorUtility.DisplayDialog("App Icon Created",
                $"App icon saved to: {path}\n\n" +
                "Applied to Android Player Settings.\n" +
                "For best results, replace with a custom 512x512 PNG.",
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
