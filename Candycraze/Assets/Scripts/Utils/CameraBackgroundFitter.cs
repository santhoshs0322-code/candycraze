// ============================================================
// CameraBackgroundFitter.cs
// Scales a background SpriteRenderer so it always fully covers
// the orthographic camera's view — on ANY phone aspect ratio
// (16:9, 18:9, 19.5:9, 20:9, tablets, etc.).
// Attach to the background GameObject (a child of the camera).
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    [RequireComponent(typeof(SpriteRenderer))]
    public class CameraBackgroundFitter : MonoBehaviour
    {
        [Tooltip("Extra margin so edges are never exposed (1.05 = 5% overscan).")]
        [SerializeField] private float _overscan = 1.06f;

        private SpriteRenderer _sr;
        private Camera         _cam;
        private Vector2        _lastScreen;
        private float          _lastOrtho = -1f;

        private void Awake()
        {
            _sr  = GetComponent<SpriteRenderer>();
            _cam = Camera.main;
            if (_cam == null) _cam = GetComponentInParent<Camera>();
        }

        private void Start()  => Fit();

        private void LateUpdate()
        {
            // Re-fit only when something actually changed (cheap check)
            if (_cam == null) { _cam = Camera.main; if (_cam == null) return; }

            var screen = new Vector2(Screen.width, Screen.height);
            if (screen != _lastScreen ||
                !Mathf.Approximately(_cam.orthographicSize, _lastOrtho))
                Fit();
        }

        private void Fit()
        {
            if (_cam == null || _sr == null || _sr.sprite == null) return;
            if (!_cam.orthographic) return;

            _lastScreen = new Vector2(Screen.width, Screen.height);
            _lastOrtho  = _cam.orthographicSize;

            float aspect = (float)Screen.width / Mathf.Max(1, Screen.height);

            // Visible world extents of the orthographic camera
            float worldH = _cam.orthographicSize * 2f;
            float worldW = worldH * aspect;

            // Sprite's native world size at scale 1
            Vector2 sp = _sr.sprite.bounds.size;
            if (sp.x < 0.0001f || sp.y < 0.0001f) return;

            // Cover: scale up to the larger required ratio, plus overscan
            float scale = Mathf.Max(worldW / sp.x, worldH / sp.y) * _overscan;
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }
}
