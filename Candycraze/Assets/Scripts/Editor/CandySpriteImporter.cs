#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CandyCraze.Editor
{
    public class CandySpriteImporter : AssetPostprocessor
    {
        private void OnPreprocessTexture()
        {
            if (!assetPath.Replace('\\', '/').StartsWith("Assets/Resources/CandySprites/")) return;
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = 512;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.compressionQuality = 80;
            importer.GetSourceTextureWidthAndHeight(out int width, out int height);
            // Generated icons have transparent padding. Keep their visible body
            // close to 0.9 board cells without changing logical board coordinates.
            importer.spritePixelsPerUnit = Mathf.Max(width, height) * 1.05f;
            importer.spritePivot = new Vector2(.5f,.5f);
        }
    }
}
#endif
