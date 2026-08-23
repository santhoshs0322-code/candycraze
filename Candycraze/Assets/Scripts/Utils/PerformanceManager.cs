// ============================================================
// PerformanceManager.cs
// Sets target frame rate, manages screen sleep, and runs
// lightweight FPS monitoring.
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    public class PerformanceManager : MonoBehaviour
    {
        public static PerformanceManager Instance { get; private set; }

        [Header("Settings")]
        [SerializeField] private int  _targetFPS         = 60;
        [SerializeField] private bool _disableScreenSleep = true;
        [SerializeField] private bool _showFPSInDebug    = false;

        // FPS tracking
        private float _fpsTimer;
        private int   _frameCount;
        private float _currentFPS;

        // ────────────────────────────────────────────────────
        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            Apply();
        }

        private void Apply()
        {
            Application.targetFrameRate = _targetFPS;
            QualitySettings.vSyncCount  = 0;

            if (_disableScreenSleep)
                Screen.sleepTimeout = SleepTimeout.NeverSleep;
        }

        private void Update()
        {
            if (!_showFPSInDebug) return;

            _frameCount++;
            _fpsTimer += Time.unscaledDeltaTime;
            if (_fpsTimer >= 1f)
            {
                _currentFPS = _frameCount / _fpsTimer;
                _frameCount = 0;
                _fpsTimer   = 0f;
                Debug.Log($"[Performance] FPS: {_currentFPS:F0}");
            }
        }

        public float CurrentFPS => _currentFPS;
    }
}
