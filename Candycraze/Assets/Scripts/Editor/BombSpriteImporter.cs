// ============================================================
// BombSpriteImporter.cs  (EDITOR ONLY)
// Auto-configures the special-piece PNGs (ColorBomb, Stripe_H,
// Stripe_V) as Sprites the moment they're added to
// Resources/Gems/. No manual import settings needed — just drop
// the transparent PNGs in and they work.
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CandyCraze.Editor
{
    public class BombSpriteImporter : AssetPostprocessor
    {
        // File names (without extension) we want configured as sprites.
        static readonly string[] Targets =
        {
            "ColorBomb", "Stripe_H", "Stripe_V", "LineBomb"
        };

        void OnPreprocessTexture()
        {
            // Only touch PNGs inside Resources/Gems/
            if (!assetPath.Replace('\\', '/').Contains("Resources/Gems/")) return;

            string fileName = System.IO.Path.GetFileNameWithoutExtension(assetPath);
            bool isTarget = System.Array.Exists(Targets, t => t == fileName);
            if (!isTarget) return;

            var imp = (TextureImporter)assetImporter;
            imp.textureType        = TextureImporterType.Sprite;
            imp.spriteImportMode   = SpriteImportMode.Single;
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled      = false;
            imp.filterMode         = FilterMode.Bilinear;
            imp.spritePixelsPerUnit = 256f;   // fits ~1 board cell
            imp.spriteBorder       = Vector4.zero;

            Debug.Log($"[BombSpriteImporter] Configured {fileName} as a Sprite.");
        }
    }
}
#endif
