// ============================================================
// ObjectPool.cs
// Generic object pool to avoid runtime Instantiate/Destroy
// calls during gameplay.  Used primarily for gem GameObjects
// and particle effects.
// ============================================================

using System.Collections.Generic;
using UnityEngine;

namespace CandyCraze
{
    public class ObjectPool : MonoBehaviour
    {
        // ── Singleton ────────────────────────────────────────
        public static ObjectPool Instance { get; private set; }

        // ── Internal pool storage ────────────────────────────
        private Dictionary<string, Queue<GameObject>> _pools
            = new Dictionary<string, Queue<GameObject>>();

        private Dictionary<string, GameObject> _prefabMap
            = new Dictionary<string, GameObject>();

        // ── Parent for pooled objects ────────────────────────
        private Transform _poolRoot;

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            _poolRoot = new GameObject("PoolRoot").transform;
            _poolRoot.SetParent(transform);
        }

        // ── Public API ───────────────────────────────────────

        /// <summary>
        /// Pre-warms a pool by instantiating <paramref name="count"/>
        /// copies of <paramref name="prefab"/> and disabling them.
        /// </summary>
        public void WarmPool(GameObject prefab, int count)
        {
            string key = prefab.name;
            if (!_pools.ContainsKey(key))
            {
                _pools[key] = new Queue<GameObject>();
                _prefabMap[key] = prefab;
            }

            for (int i = 0; i < count; i++)
            {
                GameObject obj = CreateNew(prefab);
                ReturnToPool(obj);
            }
        }

        /// <summary>
        /// Spawns an object from the pool (or creates one if empty).
        /// </summary>
        public GameObject Spawn(GameObject prefab, Vector3 position, Quaternion rotation)
        {
            string key = prefab.name;

            if (!_pools.ContainsKey(key))
            {
                _pools[key] = new Queue<GameObject>();
                _prefabMap[key] = prefab;
            }

            GameObject obj;
            if (_pools[key].Count > 0)
            {
                obj = _pools[key].Dequeue();
            }
            else
            {
                obj = CreateNew(prefab);
            }

            obj.transform.SetPositionAndRotation(position, rotation);
            obj.SetActive(true);
            return obj;
        }

        /// <summary>
        /// Returns an object to its pool.  Call this instead of Destroy.
        /// </summary>
        public void ReturnToPool(GameObject obj)
        {
            obj.SetActive(false);
            obj.transform.SetParent(_poolRoot);

            string key = obj.name.Replace("(Clone)", "").Trim();
            if (!_pools.ContainsKey(key))
            {
                _pools[key] = new Queue<GameObject>();
            }
            _pools[key].Enqueue(obj);
        }

        // ── Private ──────────────────────────────────────────
        private GameObject CreateNew(GameObject prefab)
        {
            GameObject obj = Instantiate(prefab, _poolRoot);
            // Strip "(Clone)" so the key lookup works
            obj.name = prefab.name;
            obj.SetActive(false);
            return obj;
        }
    }
}
