// ============================================================
// FixGemPrefab.cs (EDITOR ONLY)
// CandyCraze → Fix Gem Prefab & Assign
// Rebuilds the DefaultGem prefab cleanly and assigns it +
// sprites to all 6 GemDefinitions.
// ============================================================
#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace CandyCraze.Editor
{
    public static class FixGemPrefab
    {
        static readonly string[] Names = { "Ruby","Sapphire","Emerald","Amethyst","Topaz","Diamond" };
        static readonly Color[] Colors = {
            new Color(0.95f,0.15f,0.15f), new Color(0.15f,0.40f,0.95f),
            new Color(0.10f,0.80f,0.25f), new Color(0.60f,0.15f,0.90f),
            new Color(1.00f,0.75f,0.05f), new Color(0.80f,0.90f,1.00f),
        };

        [MenuItem("CandyCraze/Fix Gem Prefab and Assign")]
        public static void Fix()
        {
            // 1. Create a clean DefaultGem prefab
            var root = new GameObject("DefaultGem");
            var sr = root.AddComponent<SpriteRenderer>();
            sr.sprite = MakeCircleSprite();
            sr.color = Color.white;
            sr.sortingOrder = 5;

            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.9f, 0.9f);

            root.AddComponent<GemView>();

            string prefabPath = "Assets/Prefabs/Gems/DefaultGem.prefab";
            EnsureFolder("Assets/Prefabs");
            EnsureFolder("Assets/Prefabs/Gems");

            bool ok;
            var prefab = PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out ok);
            Object.DestroyImmediate(root);

            if (!ok) { Debug.LogError("[FixGemPrefab] Failed to save prefab!"); return; }
            Debug.Log("[FixGemPrefab] Clean DefaultGem prefab created.");

            // 2. Assign prefab + sprite + color to each GemDefinition
            for (int i = 0; i < Names.Length; i++)
            {
                string defPath = $"Assets/ScriptableObjects/Gems/GemDefinition_{Names[i]}.asset";
                var def = AssetDatabase.LoadAssetAtPath<GemDefinition>(defPath);
                if (def == null)
                {
                    Debug.LogWarning($"[FixGemPrefab] Missing: {defPath}");
                    continue;
                }

                def.GemTypeID = i;
                def.GemName = Names[i];
                def.GemColor = Colors[i];
                def.GemPrefab = prefab;

                // Assign sprite
                string spritePath = $"Assets/Art/Gems/Gem_{Names[i]}.png";
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
                if (sprite != null) def.NormalSprite = sprite;

                EditorUtility.SetDirty(def);
            }

            // 3. Ensure GameConfig has the gem definitions
            var config = AssetDatabase.LoadAssetAtPath<GameConfig>("Assets/Resources/GameConfig.asset");
            if (config != null)
            {
                var defs = new GemDefinition[6];
                for (int i = 0; i < 6; i++)
                    defs[i] = AssetDatabase.LoadAssetAtPath<GemDefinition>(
                        $"Assets/ScriptableObjects/Gems/GemDefinition_{Names[i]}.asset");
                config.GemDefinitions = defs;
                EditorUtility.SetDirty(config);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Gem Prefab Fixed!",
                "DefaultGem prefab rebuilt and assigned to all 6 gems.\n\n" +
                "Now run 'Build All Scenes' then Play the Game scene.",
                "Done");
        }

        // Circle sprite so gems are always visible even without art
        static Sprite MakeCircleSprite()
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x/(float)(size-1)*2f-1f;
                float v = y/(float)(size-1)*2f-1f;
                float r = Mathf.Sqrt(u*u + v*v);
                float a = r < 0.85f ? 1f : Mathf.Clamp01((1f-r)/0.15f);
                // Bright with highlight
                float bright = 1f - r*0.3f;
                float hl = Mathf.Max(0, 1f - Mathf.Sqrt((u+0.3f)*(u+0.3f)+(v-0.3f)*(v-0.3f))*2f);
                Color c = Color.white * bright;
                c = Color.Lerp(c, Color.white, hl*0.5f);
                c.a = a;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();

            // Save as asset so it persists
            string path = "Assets/Art/Gems/GemCircle.png";
            System.IO.File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType = TextureImporterType.Sprite;
                imp.spritePixelsPerUnit = 128;
                imp.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
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
