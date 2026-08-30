// ============================================================
// TileManager.cs
// Responsible for creating GemView GameObjects and selecting
// which gem type to spawn at a given position.
// Uses the ObjectPool for efficient gem reuse.
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    public class TileManager : MonoBehaviour
    {
        [Header("Fallback prefab (used only if GemDefinition has no prefab)")]
        [SerializeField] private GameObject _defaultGemPrefab;

        private GameConfig _config;

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            _config = Resources.Load<GameConfig>("GameConfig");

            if (_config == null)
                Debug.LogWarning("[TileManager] GameConfig not found in Resources/. " +
                                 "Place GameConfig.asset in Assets/Resources/.");
        }

        // ── Public API ───────────────────────────────────────

        /// <summary>
        /// Instantiates (or recycles from pool) a gem GameObject.
        /// </summary>
        public GameObject CreateGemObject(GemDefinition def, Vector3 worldPos, Transform parent)
        {
            if (def == null)
            {
                Debug.LogError("[TileManager] CreateGemObject called with null GemDefinition.");
                return null;
            }

            GameObject prefab = def.GemPrefab != null ? def.GemPrefab : _defaultGemPrefab;

            if (prefab == null)
            {
                Debug.LogError("[TileManager] No gem prefab available.  " +
                               "Assign GemPrefab on the GemDefinition or assign a DefaultGemPrefab.");
                return null;
            }

            // Direct instantiate (ObjectPool bypassed for reliability)
            GameObject go = Instantiate(prefab, worldPos, Quaternion.identity);
            go.SetActive(true);

            go.transform.SetParent(parent, worldPositionStays: true);
            return go;
        }

        /// <summary>
        /// Picks a random GemDefinition for a given cell based on the
        /// level's spawn weights.  Respects forced gem types in TileData.
        /// </summary>
        // Cached gem definitions — created at runtime if none found
        private GemDefinition[] _gems;

        private GemDefinition[] GetGemDefs()
        {
            if (_gems != null && _gems.Length > 0) return _gems;

            // 1. Try config
            if (_config != null && _config.GemDefinitions != null)
            {
                var valid = new System.Collections.Generic.List<GemDefinition>();
                foreach (var g in _config.GemDefinitions) if (g != null) valid.Add(g);
                if (valid.Count > 0) { _gems = valid.ToArray(); return _gems; }
            }

            // 2. Try Resources
            var loaded = Resources.LoadAll<GemDefinition>("Gems");
            if (loaded != null && loaded.Length > 0) { _gems = loaded; return _gems; }

            // 3. ULTIMATE FALLBACK — create 6 gem defs in code
            Debug.LogWarning("[TileManager] Creating 6 gem definitions in code (fallback).");
            _gems = CreateCodeGems();
            return _gems;
        }

        private GemDefinition[] CreateCodeGems()
        {
            Color[] colors = {
                new Color(0.95f,0.15f,0.20f), // Ruby
                new Color(0.15f,0.45f,0.95f), // Sapphire
                new Color(0.10f,0.80f,0.30f), // Emerald
                new Color(0.65f,0.20f,0.95f), // Amethyst
                new Color(1.00f,0.72f,0.05f), // Topaz
                new Color(0.55f,0.90f,1.00f), // Diamond
            };
            string[] names = { "Ruby","Sapphire","Emerald","Amethyst","Topaz","Diamond" };

            var gems = new GemDefinition[6];
            for (int i=0;i<6;i++)
            {
                var g = ScriptableObject.CreateInstance<GemDefinition>();
                g.GemTypeID = i;
                g.GemName = names[i];
                g.GemColor = colors[i];
                g.NormalSprite = MakeGlossyGemSprite(colors[i]);  // realistic per-color sprite
                g.GemPrefab = _defaultGemPrefab;
                gems[i] = g;
            }
            return gems;
        }

        // Creates a realistic glossy 3D-looking gem sprite
        private static Sprite MakeGlossyGemSprite(Color baseColor)
        {
            int size = 128;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x/(float)(size-1)*2f-1f;
                float v = y/(float)(size-1)*2f-1f;
                float r = Mathf.Sqrt(u*u + v*v);

                if (r > 1f) { tex.SetPixel(x,y,Color.clear); continue; }

                // Sphere shading: bright top-left, dark bottom-right
                float light = Mathf.Clamp01(1f - (u*0.4f - v*0.5f + r*0.3f));
                Color c = baseColor * (0.45f + light * 0.75f);

                // Specular highlight (top-left glossy spot)
                float hlDist = Mathf.Sqrt((u+0.35f)*(u+0.35f) + (v-0.4f)*(v-0.4f));
                float hl = Mathf.Max(0f, 1f - hlDist * 2.8f);
                c = Color.Lerp(c, Color.white, hl * hl * 0.85f);

                // Rim light (bright edge)
                float rim = Mathf.SmoothStep(0.75f, 1f, r);
                c = Color.Lerp(c, baseColor * 1.4f, rim * 0.4f);

                // Bottom reflection glow
                float bottomGlow = Mathf.Max(0f, -v - 0.3f) * 0.3f;
                c = Color.Lerp(c, Color.white, bottomGlow * 0.2f);

                // Soft anti-aliased edge
                float alpha = r < 0.92f ? 1f : Mathf.Clamp01((1f - r) / 0.08f);

                c.r = Mathf.Clamp01(c.r); c.g = Mathf.Clamp01(c.g); c.b = Mathf.Clamp01(c.b);
                c.a = alpha;
                tex.SetPixel(x, y, c);
            }
            tex.Apply();
            tex.filterMode = FilterMode.Bilinear;
            return Sprite.Create(tex, new Rect(0,0,size,size), new Vector2(0.5f,0.5f), size);
        }

        public GemDefinition GetRandomGemDefinition(LevelData level, int row, int col)
        {
            var gems = GetGemDefs();
            if (gems == null || gems.Length == 0) return null;
            for (int a=0; a<10; a++)
            {
                var pick = gems[Random.Range(0, gems.Length)];
                if (pick != null) return pick;
            }
            foreach (var g in gems) if (g != null) return g;
            return null;
        }

        private GemDefinition GetRandomGemDefinition_OLD(LevelData level, int row, int col)
        {
            if (_config == null || _config.GemDefinitions == null || _config.GemDefinitions.Length == 0)
            {
                Debug.LogError("[TileManager] GameConfig has no GemDefinitions.");
                return null;
            }

            // Check for forced type
            TileData tile = level.GetTileData(row, col);
            if (tile.ForcedGemTypeID >= 0)
            {
                GemDefinition forced = _config.GetGemDefinition(tile.ForcedGemTypeID);
                if (forced != null) return forced;
            }

            // Weighted random selection
            float totalWeight = 0f;
            for (int i = 0; i < _config.GemDefinitions.Length; i++)
            {
                if (_config.GemDefinitions[i] == null) continue;
                totalWeight += level.GetSpawnWeight(_config.GemDefinitions[i].GemTypeID);
            }

            if (totalWeight <= 0f)
            {
                // Uniform fallback
                int idx = Random.Range(0, _config.GemDefinitions.Length);
                return _config.GemDefinitions[idx];
            }

            float roll = Random.Range(0f, totalWeight);
            float cumulative = 0f;

            foreach (var def in _config.GemDefinitions)
            {
                if (def == null) continue;
                cumulative += level.GetSpawnWeight(def.GemTypeID);
                if (roll <= cumulative)
                    return def;
            }

            // Fallback: return first valid definition
            return _config.GemDefinitions[0];
        }
    }
}
