// ============================================================
// ScreenSafeArea.cs
// Applies device safe area to a RectTransform so UI doesn't
// overlap notches or camera cutouts on Android devices.
// Attach to the root Canvas RectTransform.
// ============================================================

using UnityEngine;

namespace CandyCraze
{
    [RequireComponent(typeof(RectTransform))]
    public class ScreenSafeArea : MonoBehaviour
    {
        private RectTransform _rt;
        private Rect          _lastSafeArea;
        private Vector2       _lastScreenSize;

        private void Awake()
        {
            _rt = GetComponent<RectTransform>();
            Apply();
        }

        private void Update()
        {
            // Re-apply if screen size or safe area changes (rotation etc.)
            if (Screen.safeArea != _lastSafeArea ||
                new Vector2(Screen.width, Screen.height) != _lastScreenSize)
                Apply();
        }

        private void Apply()
        {
            Rect safeArea   = Screen.safeArea;
            _lastSafeArea   = safeArea;
            _lastScreenSize = new Vector2(Screen.width, Screen.height);

            Vector2 anchorMin = safeArea.position;
            Vector2 anchorMax = safeArea.position + safeArea.size;

            anchorMin.x /= Screen.width;
            anchorMin.y /= Screen.height;
            anchorMax.x /= Screen.width;
            anchorMax.y /= Screen.height;

            _rt.anchorMin = anchorMin;
            _rt.anchorMax = anchorMax;
        }
    }
}
