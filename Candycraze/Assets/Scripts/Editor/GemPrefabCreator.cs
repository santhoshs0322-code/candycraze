// ============================================================
// GemPrefabCreator.cs  (EDITOR ONLY)
// CandyCraze → Create Default Gem Prefab
//
// Creates a simple coloured-square gem prefab that works
// immediately without any art assets.  Replace sprites later.
// ============================================================

#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;

namespace CandyCraze.Editor
{
    public static class GemPrefabCreator
    {
        [MenuItem("CandyCraze/Create Default Gem Prefab")]
        public static void CreateDefaultGemPrefab()
        {
            // Root GameObject
            GameObject root = new GameObject("DefaultGem");

            // SpriteRenderer — will use a white square by default
            SpriteRenderer sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = GetDefaultSprite();
            // Use Default sorting layer — works without any custom layer setup
            sr.sortingOrder = 1;

            // Collider for touch detection
            BoxCollider2D col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);  // Slightly smaller than cell

            // GemView script
            root.AddComponent<GemView>();

            // Highlight child (sibling SpriteRenderer with bright overlay)
            GameObject highlight = new GameObject("Highlight");
            highlight.transform.SetParent(root.transform);
            highlight.transform.localPosition = Vector3.zero;
            SpriteRenderer hlSr = highlight.AddComponent<SpriteRenderer>();
            hlSr.sprite = GetDefaultSprite();
            hlSr.color  = new Color(1f, 1f, 1f, 0.5f);
            hlSr.sortingOrder = 2;

            // Save as prefab
            string path = "Assets/Prefabs/Gems/DefaultGem.prefab";
            bool success;
            PrefabUtility.SaveAsPrefabAsset(root, path, out success);

            Object.DestroyImmediate(root);

            if (success)
            {
                Debug.Log($"[GemPrefabCreator] Saved prefab: {path}");
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Prefab Created",
                    $"DefaultGem prefab saved to:\n{path}\n\n" +
                    "Assign this prefab to each GemDefinition's 'Gem Prefab' field.",
                    "OK");
            }
            else
            {
                Debug.LogError("[GemPrefabCreator] Failed to save prefab.");
            }
        }

        private static Sprite GetDefaultSprite()
        {
            // Create a plain white 64x64 texture as placeholder sprite
            Texture2D tex = new Texture2D(64, 64);
            Color[] pixels = new Color[64 * 64];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels(pixels);
            tex.Apply();

            return Sprite.Create(
                tex,
                new Rect(0, 0, 64, 64),
                new Vector2(0.5f, 0.5f),
                64f);
        }
    }
}

#endif
