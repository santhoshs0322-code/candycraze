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

            GameObject go;
            if (ObjectPool.Instance != null)
            {
                go = ObjectPool.Instance.Spawn(prefab, worldPos, Quaternion.identity);
            }
            else
            {
                go = Instantiate(prefab, worldPos, Quaternion.identity);
            }

            go.transform.SetParent(parent, worldPositionStays: true);
            return go;
        }

        /// <summary>
        /// Picks a random GemDefinition for a given cell based on the
        /// level's spawn weights.  Respects forced gem types in TileData.
        /// </summary>
        public GemDefinition GetRandomGemDefinition(LevelData level, int row, int col)
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
