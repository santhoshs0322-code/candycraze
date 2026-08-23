// ============================================================
// GemVisualSetup.cs  (EDITOR ONLY)
// Creates realistic gem textures with facets, depth, shine,
// rim lighting, and inner glow effects.
// ============================================================
#if UNITY_EDITOR

using UnityEditor;
using UnityEngine;
using System.IO;

namespace CandyCraze.Editor
{
    public static class GemVisualSetup
    {
        // ── Gem base colours (rich, saturated) ───────────────
        static readonly Color[] GemBase = {
            new Color(0.92f, 0.08f, 0.10f),  // Ruby      — deep red
            new Color(0.08f, 0.30f, 0.95f),  // Sapphire  — royal blue
            new Color(0.05f, 0.78f, 0.22f),  // Emerald   — vivid green
            new Color(0.58f, 0.08f, 0.92f),  // Amethyst  — violet
            new Color(1.00f, 0.72f, 0.02f),  // Topaz     — warm gold
            new Color(0.75f, 0.92f, 1.00f),  // Diamond   — icy white-blue
        };

        static readonly Color[] GemDark = {
            new Color(0.40f, 0.00f, 0.02f),
            new Color(0.02f, 0.08f, 0.45f),
            new Color(0.01f, 0.30f, 0.08f),
            new Color(0.20f, 0.00f, 0.40f),
            new Color(0.45f, 0.28f, 0.00f),
            new Color(0.20f, 0.45f, 0.60f),
        };

        static readonly Color[] GemShine = {
            new Color(1.00f, 0.60f, 0.60f),
            new Color(0.60f, 0.80f, 1.00f),
            new Color(0.60f, 1.00f, 0.70f),
            new Color(0.90f, 0.70f, 1.00f),
            new Color(1.00f, 0.95f, 0.60f),
            new Color(1.00f, 1.00f, 1.00f),
        };

        static readonly string[] GemNames = {
            "Ruby","Sapphire","Emerald","Amethyst","Topaz","Diamond"
        };

        // ════════════════════════════════════════════════════
        [MenuItem("CandyCraze/Create Gem Visuals (Realistic)")]
        public static void CreateGemVisuals()
        {
            string folder = "Assets/Art/Gems";
            if (!AssetDatabase.IsValidFolder(folder))
                AssetDatabase.CreateFolder("Assets/Art","Gems");

            for (int i = 0; i < GemNames.Length; i++)
            {
                CreateRealisticGem(i, folder);
                UpdateGemDefinition(i);
            }

            CreateColoredGemPrefabs();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.DisplayDialog("Realistic Gems Created",
                "6 high-quality gem textures generated with:\n\n" +
                "• Faceted 3D depth shading\n" +
                "• Specular highlight & rim light\n" +
                "• Inner glow\n" +
                "• Anti-aliased edges\n\n" +
                "Run 'Build All Scenes' then Play to see them.",
                "Done");
        }

        // ════════════════════════════════════════════════════
        // REALISTIC GEM GENERATOR
        // ════════════════════════════════════════════════════

        static void CreateRealisticGem(int index, string folder)
        {
            int size    = 256;          // Higher res = crisper
            var tex     = new Texture2D(size, size, TextureFormat.RGBA32, false);
            Color baseC = GemBase[index];
            Color darkC = GemDark[index];
            Color shinC = GemShine[index];

            for (int py = 0; py < size; py++)
            for (int px = 0; px < size; px++)
            {
                // Normalised coords -1..1
                float u = (px / (float)(size-1)) * 2f - 1f;
                float v = (py / (float)(size-1)) * 2f - 1f;

                Color pixel = DrawGem(index, u, v, baseC, darkC, shinC);
                tex.SetPixel(px, py, pixel);
            }

            tex.Apply();

            string path = $"{folder}/Gem_{GemNames[index]}.png";
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);

            AssetDatabase.ImportAsset(path);
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp != null)
            {
                imp.textureType         = TextureImporterType.Sprite;
                imp.spritePixelsPerUnit = 128;
                imp.filterMode          = FilterMode.Bilinear;
                imp.textureCompression  = TextureImporterCompression.Compressed;
                imp.crunchedCompression = true;
                imp.SaveAndReimport();
            }
        }

        // ── Core pixel shader ────────────────────────────────

        static Color DrawGem(int idx, float u, float v,
            Color baseC, Color darkC, Color shinC)
        {
            // ── Shape mask ───────────────────────────────────
            float mask  = GemMask(idx, u, v);
            float alpha = SoftEdge(mask, 0.90f, 0.96f);
            if (alpha <= 0.001f) return Color.clear;

            // ── Facet normals (simulate 3D) ──────────────────
            // Top facet: bright centre plateau
            float topFacet   = Mathf.Pow(Mathf.Max(0f, 1f - (u*u + v*v)*1.6f), 0.6f);

            // Outer bevel: darker ring
            float outerBevel = 1f - topFacet;

            // Side facet lines (sharp radial cuts)
            float angle      = Mathf.Atan2(v, u);           // -π .. π
            int   facetCount = GemFacetCount(idx);
            float facetLine  = Mathf.Abs(Mathf.Sin(angle * facetCount * 0.5f));
            float facetDark  = Mathf.Pow(facetLine, 8f);    // sharp dark lines

            // ── Diffuse lighting (light from top-left) ───────
            // Fake normal from position
            float nDotL      = Mathf.Clamp01((-u * 0.5f + v * 0.3f + 0.8f));
            float diffuse    = Mathf.Lerp(0.35f, 1.15f, nDotL);

            // ── Specular (sharp highlight) ────────────────────
            // Two highlights: main + secondary
            float spec1      = SpecHot( u, v, -0.30f,  0.35f, 2.2f);
            float spec2      = SpecHot( u, v,  0.20f, -0.25f, 1.4f);
            float specTotal  = spec1 * 0.9f + spec2 * 0.3f;

            // ── Rim light (bright edge) ───────────────────────
            float rim        = Mathf.Pow(Mathf.Clamp01(1f - topFacet * 1.3f), 3f) * 0.55f;

            // ── Inner glow (saturated centre) ────────────────
            float innerGlow  = Mathf.Pow(topFacet, 2.5f) * 0.4f;

            // ── Compose colour ───────────────────────────────
            // Base diffuse blend: dark → base → shine
            float t          = diffuse * topFacet;
            Color gemColor   = Color.Lerp(darkC, baseC, t);
            gemColor         = Color.Lerp(gemColor, shinC, innerGlow);

            // Darken at facet edges
            gemColor         = Color.Lerp(gemColor, darkC, facetDark * outerBevel * 0.5f);

            // Outer bevel slightly darker
            gemColor         = Color.Lerp(gemColor, darkC * 1.2f, outerBevel * 0.25f);

            // Rim light (near-white)
            gemColor         = Color.Lerp(gemColor, Color.white, rim);

            // Specular highlights (pure white)
            gemColor         = Color.Lerp(gemColor, Color.white, specTotal);

            // Gamma correction for vibrancy
            gemColor.r = Mathf.Pow(Mathf.Clamp01(gemColor.r), 0.85f);
            gemColor.g = Mathf.Pow(Mathf.Clamp01(gemColor.g), 0.85f);
            gemColor.b = Mathf.Pow(Mathf.Clamp01(gemColor.b), 0.85f);
            gemColor.a = alpha;

            // Diamond sparkle: extra white facets
            if (idx == 5)
            {
                float sparkle = Mathf.Abs(Mathf.Sin(angle * 4f)) *
                                Mathf.Pow(topFacet, 1.5f) * 0.35f;
                gemColor = Color.Lerp(gemColor, Color.white, sparkle);
            }

            return gemColor;
        }

        // ── Shape mask per gem type ──────────────────────────

        static float GemMask(int idx, float u, float v)
        {
            switch (idx)
            {
                case 0: // Ruby — classic gem cut (pointed top & bottom)
                {
                    float top    = 1f - Mathf.Abs(u) - Mathf.Max(0f, v) * 0.8f;
                    float bot    = 1f - Mathf.Abs(u) + Mathf.Min(0f, v) * 0.8f;
                    return Mathf.Min(top, bot) * 1.4f;
                }
                case 1: // Sapphire — round brilliant
                {
                    float r = Mathf.Sqrt(u*u + v*v);
                    return 1f - r / 0.88f;
                }
                case 2: // Emerald — rectangular step cut
                {
                    float bx = 1f - Mathf.Abs(u) / 0.75f;
                    float by = 1f - Mathf.Abs(v) / 0.88f;
                    float corner = 1f - Mathf.Pow(Mathf.Abs(u)/0.75f, 5f)
                                     - Mathf.Pow(Mathf.Abs(v)/0.88f, 5f);
                    return Mathf.Min(bx, by) * 1.2f + corner * 0.3f;
                }
                case 3: // Amethyst — hexagonal
                {
                    float hex = Mathf.Max(
                        Mathf.Abs(u),
                        (Mathf.Abs(u) + Mathf.Abs(v) * 1.732f) * 0.5f);
                    return (0.88f - hex) / 0.88f * 1.5f;
                }
                case 4: // Topaz — cushion cut (puffy square)
                {
                    float sq  = Mathf.Pow(Mathf.Abs(u), 4f) +
                                Mathf.Pow(Mathf.Abs(v), 4f);
                    return (0.55f - sq) / 0.55f * 2.0f;
                }
                case 5: // Diamond — marquise/kite
                {
                    float d = Mathf.Abs(u) / 0.65f + Mathf.Abs(v) / 0.88f;
                    return (1f - d) * 1.4f;
                }
                default:
                    return 1f - Mathf.Sqrt(u*u + v*v) / 0.85f;
            }
        }

        static int GemFacetCount(int idx) =>
            idx switch { 0=>8, 1=>16, 2=>4, 3=>6, 4=>8, 5=>8, _=>8 };

        // ── Smooth edge anti-alias ───────────────────────────
        static float SoftEdge(float mask, float innerR, float outerR)
        {
            float raw = Mathf.InverseLerp(0f, 1f, mask);
            if (raw >= outerR) return 1f;
            if (raw <= innerR) return 0f;
            float t = (raw - innerR) / (outerR - innerR);
            return t * t * (3f - 2f * t); // smoothstep
        }

        // ── Specular hot-spot ────────────────────────────────
        static float SpecHot(float u, float v, float cx, float cy, float power)
        {
            float dx = u - cx, dy = v - cy;
            float d  = Mathf.Sqrt(dx*dx + dy*dy);
            return Mathf.Pow(Mathf.Max(0f, 1f - d * power), 6f);
        }

        // ── Update GemDefinition sprites ────────────────────

        static void UpdateGemDefinition(int index)
        {
            string defPath = $"Assets/ScriptableObjects/Gems/GemDefinition_{GemNames[index]}.asset";
            var def = AssetDatabase.LoadAssetAtPath<GemDefinition>(defPath);
            if (def == null) return;

            string spritePath = $"Assets/Art/Gems/Gem_{GemNames[index]}.png";
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spritePath);
            if (sprite != null)
            {
                def.NormalSprite = sprite;
                def.GemColor     = GemBase[index];
                EditorUtility.SetDirty(def);
            }
        }

        // ── Update DefaultGem prefab ─────────────────────────

        static void CreateColoredGemPrefabs()
        {
            string prefabPath = "Assets/Prefabs/Gems/DefaultGem.prefab";
            var root = new GameObject("DefaultGem");

            var sr = root.AddComponent<SpriteRenderer>();
            sr.sortingOrder = 1;

            var col = root.AddComponent<BoxCollider2D>();
            col.size = new Vector2(0.88f, 0.88f);

            root.AddComponent<GemView>();

            // Highlight child — white overlay flash
            var hlGO = new GameObject("Highlight");
            hlGO.transform.SetParent(root.transform, false);
            var hlSr = hlGO.AddComponent<SpriteRenderer>();
            hlSr.color        = new Color(1f, 1f, 1f, 0f); // invisible by default
            hlSr.sortingOrder = 2;

            // Glow child — coloured outer glow
            var glowGO = new GameObject("Glow");
            glowGO.transform.SetParent(root.transform, false);
            glowGO.transform.localScale = Vector3.one * 1.3f;
            var glowSr = glowGO.AddComponent<SpriteRenderer>();
            glowSr.color        = new Color(1f, 1f, 1f, 0f); // invisible until selected
            glowSr.sortingOrder = 0;

            bool ok;
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath, out ok);
            Object.DestroyImmediate(root);

            if (ok) Debug.Log("[GemVisualSetup] DefaultGem prefab rebuilt.");
        }
    }
}
#endif
