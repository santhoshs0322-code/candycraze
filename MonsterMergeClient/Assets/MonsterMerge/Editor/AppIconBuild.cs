using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace MonsterMerge.Editor
{
    // Unity's generated mipmap PNGs have occasionally been solid black even when
    // the source icon is valid. Write Android resources from the source PNGs.
    public sealed class AppIconBuild : IPreprocessBuildWithReport, IPostGenerateGradleAndroidProject
    {
        private const string IconPath = "Assets/MonsterMerge/Art/AppIcon.png";
        private const string BackgroundPath = "Assets/MonsterMerge/Art/AppIconBackground.png";
        private const string ForegroundPath = "Assets/MonsterMerge/Art/AppIconForeground.png";

        public int callbackOrder => 0;

        [MenuItem("Monster Merge/Apply Dragon App Icon")]
        public static void Apply()
        {
            var icon = LoadIcon(IconPath);
            var background = LoadIcon(BackgroundPath);
            var foreground = LoadIcon(ForegroundPath);
            PlayerSettings.SetIconsForTargetGroup(BuildTargetGroup.Unknown, new[] { icon });
            foreach (var kind in new[] { AndroidPlatformIconKind.Legacy, AndroidPlatformIconKind.Round, AndroidPlatformIconKind.Adaptive })
            {
                var slots = PlayerSettings.GetPlatformIcons(BuildTargetGroup.Android, kind);
                foreach (var slot in slots)
                {
                    if (kind == AndroidPlatformIconKind.Adaptive)
                        slot.SetTextures(new[] { background, foreground });
                    else
                        slot.SetTexture(icon);
                }
                PlayerSettings.SetPlatformIcons(BuildTargetGroup.Android, kind, slots);
            }
            AssetDatabase.SaveAssets();
        }

        private static Texture2D LoadIcon(string path)
        {
            if (!File.Exists(path)) throw new BuildFailedException("Monster Merge app icon is missing: " + path);
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null) throw new BuildFailedException("Cannot import app icon: " + path);
            if (importer.textureType != TextureImporterType.Default ||
                importer.textureCompression != TextureImporterCompression.Uncompressed ||
                !importer.isReadable || !importer.sRGBTexture || importer.maxTextureSize < 2048)
            {
                importer.textureType = TextureImporterType.Default;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.isReadable = true;
                importer.sRGBTexture = true;
                importer.maxTextureSize = 2048;
                importer.SaveAndReimport();
            }
            var icon = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (icon == null) throw new BuildFailedException("Cannot load app icon: " + path);
            return icon;
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            if (report.summary.platform == BuildTarget.Android) Apply();
        }

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            var resources = Path.GetFullPath(Path.Combine(path, "..", "launcher", "src", "main", "res"));
            if (!Directory.Exists(resources))
                throw new BuildFailedException("Android launcher resources not found: " + resources);
            var icon = ReadSource(IconPath);
            var background = ReadSource(BackgroundPath);
            var foreground = ReadSource(ForegroundPath);
            var count = 0;
            try
            {
                foreach (var directory in Directory.GetDirectories(resources, "mipmap-*"))
                {
                    count += Replace(directory, "app_icon.png", icon, 1f);
                    count += Replace(directory, "app_icon_round.png", icon, 1f);
                    count += Replace(directory, "ic_launcher_background.png", background, 1f);
                    count += Replace(directory, "ic_launcher_foreground.png", foreground, 0.66f);
                }
            }
            finally
            {
                Object.DestroyImmediate(icon);
                Object.DestroyImmediate(background);
                Object.DestroyImmediate(foreground);
            }
            if (count == 0) throw new BuildFailedException("No Android launcher icon PNGs were generated.");
            Debug.Log("[Monster Merge] Replaced " + count + " Android launcher icon PNGs with dragon artwork.");
        }

        private static Texture2D ReadSource(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
                throw new BuildFailedException("Cannot decode app icon: " + path);
            return texture;
        }

        private static int Replace(string directory, string filename, Texture2D source, float scale)
        {
            var destination = Path.Combine(directory, filename);
            if (!File.Exists(destination)) return 0;
            var existing = ReadSource(destination);
            var size = existing.width;
            Object.DestroyImmediate(existing);
            var output = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var margin = (1f - scale) * 0.5f;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    var u = ((x + 0.5f) / size - margin) / scale;
                    var v = ((y + 0.5f) / size - margin) / scale;
                    output.SetPixel(x, y, u < 0f || u > 1f || v < 0f || v > 1f
                        ? Color.clear : source.GetPixelBilinear(u, v));
                }
            }
            output.Apply(false, false);
            File.WriteAllBytes(destination, output.EncodeToPNG());
            Object.DestroyImmediate(output);
            return 1;
        }
    }
}
