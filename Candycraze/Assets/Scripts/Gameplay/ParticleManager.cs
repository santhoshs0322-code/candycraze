// ============================================================
// ParticleManager.cs
// Spawns burst particles at world positions for matches,
// combos, and special piece explosions.
// Uses procedural particle systems — no external assets needed.
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    public class ParticleManager : MonoBehaviour
    {
        public static ParticleManager Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        // ── Public API ───────────────────────────────────────

        public void PlayMatchBurst(Vector3 worldPos, Color color)
            => SpawnBurst(worldPos, color, 8, 2.5f, 0.4f);

        public void PlayComboBurst(Vector3 worldPos, Color color)
            => SpawnBurst(worldPos, color, 16, 4f, 0.6f);

        public void PlaySpecialBurst(Vector3 worldPos, Color color)
            => SpawnBurst(worldPos, color, 24, 5f, 0.8f);

        public void PlayWinEffect(Vector3 screenCentre)
        {
            for (int i = 0; i < 6; i++)
            {
                Color c = Color.HSVToRGB(i / 6f, 0.9f, 1f);
                Vector3 pos = screenCentre + new Vector3(
                    Random.Range(-3f, 3f), Random.Range(-1f, 2f), 0f);
                SpawnBurst(pos, c, 12, 3f, 1f);
            }
        }

        // ── Private ──────────────────────────────────────────

        private void SpawnBurst(Vector3 pos, Color color,
            int count, float speed, float lifetime)
        {
            var go = new GameObject("Burst");
            go.transform.position = pos;

            var ps   = go.AddComponent<ParticleSystem>();
            var main = ps.main;
            main.startColor    = new ParticleSystem.MinMaxGradient(color, Color.white);
            main.startSize     = new ParticleSystem.MinMaxCurve(0.08f, 0.18f);
            main.startSpeed    = new ParticleSystem.MinMaxCurve(speed * 0.6f, speed);
            main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime * 0.7f, lifetime);
            main.maxParticles  = count;
            main.loop          = false;
            main.playOnAwake   = false;
            main.gravityModifier = 0.3f;

            var emission = ps.emission;
            emission.enabled = false;
            // Allocate one burst slot BEFORE calling SetBurst(0, ...),
            // otherwise Unity warns "Burst index too high [0, max 0]".
            emission.burstCount = 1;

            var burst = new ParticleSystem.Burst(0f, (short)count);
            emission.SetBurst(0, burst);
            emission.enabled = true;

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Circle;
            shape.radius    = 0.1f;

            var renderer = ps.GetComponent<ParticleSystemRenderer>();
            var mat = new Material(Shader.Find("Sprites/Default"));
            // Give the particle a soft ROUND texture so bursts aren't square.
            mat.mainTexture = GetRoundParticleTexture();
            mat.color = color;
            renderer.material = mat;
            renderer.sortingOrder = 10;

            ps.Play();
            Destroy(go, lifetime + 0.5f);
        }

        // Soft round particle texture (cached) so match bursts render as
        // circles instead of the default square quad.
        private static Texture2D _roundTex;
        private static Texture2D GetRoundParticleTexture()
        {
            if (_roundTex != null) return _roundTex;
            int size = 32;
            _roundTex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float u = x / (float)(size - 1) * 2f - 1f;
                float v = y / (float)(size - 1) * 2f - 1f;
                float r = Mathf.Sqrt(u * u + v * v);
                float a = r <= 0.6f ? 1f : Mathf.Clamp01((1f - r) / 0.4f);
                _roundTex.SetPixel(x, y, new Color(1, 1, 1, a));
            }
            _roundTex.Apply();
            _roundTex.filterMode = FilterMode.Bilinear;
            return _roundTex;
        }
    }
}
